using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IL2Wasm.Compilation;

public class CompilerVisitor : ICompilerVisitor
{
    private readonly IWatWriter _writer;
    private readonly List<BaseInstructionHandler> _handlers;
    private readonly List<(string module, string name, MethodDefinition method)> _imports = new();

    private readonly List<int> _closingOffsets = new();

    private string? _currentLabel;

    public CompilerVisitor(IWatWriter writer, IEnumerable<BaseInstructionHandler> handlers)
    {
        _writer = writer;
        _handlers = new List<BaseInstructionHandler>(handlers);
    }

    public void VisitAssembly(AssemblyDefinition assembly)
    {
        // Collect imports first
        foreach (var module in assembly.Modules)
            foreach (var type in module.Types)
                foreach (var method in type.Methods)
                {
                    var jsAttr = method.CustomAttributes
                        .FirstOrDefault(a => a.AttributeType.FullName == "IL2Wasm.Interop.JSImportAttribute");
                    if (jsAttr != null)
                    {
                        string moduleName = jsAttr.ConstructorArguments[0].Value?.ToString() ?? "env";
                        string name = jsAttr.ConstructorArguments[1].Value?.ToString() ?? method.Name;
                        _imports.Add((moduleName, name, method));
                    }
                }

        _writer.BeginModule();

        // Emit all imports first
        foreach (var import in _imports)
        {
            var paramTypes = import.method.Parameters
                .Select(p => Conversion.GetWasmType(p.ParameterType) ?? "i32")
                .ToList();
            var returnType = Conversion.GetWasmType(import.method.ReturnType);
            _writer.DeclareImport(import.name, import.module, paramTypes, returnType);
        }

        _writer.DeclareMemory();

        foreach (var module in assembly.Modules)
            VisitModule(module);

        _writer.EndModule();
        if (_writer is TextWatWriter tw)
            tw.Flush();
    }

    public void VisitModule(ModuleDefinition module)
    {
        foreach (var type in module.Types)
            VisitType(type);
    }

    public void VisitType(TypeDefinition type)
    {
        // Handle static (global) fields
        foreach (var field in type.Fields)
        {
            if (field.IsStatic)
            {
                string? watType = Conversion.GetWasmType(field.FieldType) ?? "i32";
                string initialValue = watType == "f32" || watType == "f64" ? "0.0" : "0"; // Default to 0
                _writer.DeclareGlobal(Conversion.GetWasmFieldName(field), watType, isMutable: !field.IsInitOnly, initialValue);
            }
        }

        foreach (var method in type.Methods)
            VisitMethod(method);

        foreach (var nested in type.NestedTypes)
            VisitType(nested);
    }

    public void VisitMethod(MethodDefinition method)
    {
        if (!method.HasBody || method.CustomAttributes.Any(a => a.AttributeType.FullName == "IL2Wasm.Interop.JSImportAttribute"))
            return;

        string? returnType = Conversion.GetWasmType(method.ReturnType);

        // Convert parameters to WASM types
        var paramTypes = new List<string>();
        foreach (var param in method.Parameters)
        {
            string? watType = Conversion.GetWasmType(param.ParameterType) ?? "i32";
            paramTypes.Add(watType);
        }


        // Track whether we need a 'this' pointer
        bool hasThis = method.IsConstructor && !method.IsStatic;
        bool usesNewObj = method.Body.Instructions.Any(instr => instr.OpCode.Code == Code.Newobj);
        bool instanceMethod = !method.IsStatic && !method.IsConstructor;

        string name;
        if (method.IsConstructor && !method.IsStatic)
        {
            name = $"{method.DeclaringType.Name}_ctor";
            paramTypes.Insert(0, "i32"); // this pointer
        }
        else if (!method.IsStatic) // instance method
        {
            name = Conversion.GetWasmMethodName(method);
            paramTypes.Insert(0, "i32");
        }
        else
        {
            name = Conversion.GetWasmMethodName(method); // static method
        }

        _writer.BeginFunction(name, returnType, paramTypes);

        // Collect all locals from handlers
        var localsToDeclare = new Dictionary<string, string>();

        if (hasThis || usesNewObj || instanceMethod)
            localsToDeclare["this"] = "i32";

        foreach (var handler in _handlers)
            if (handler.LocalVariables != null)
                foreach (var kvp in handler.LocalVariables)
                    localsToDeclare[kvp.Key] = kvp.Value;

        // Declare locals
        foreach (var kvp in localsToDeclare)
            _writer.DeclareLocal(kvp.Key, kvp.Value);

        // Declare locals for MethodDefinition variables (numeric only)
        int localCount = method.Body.Variables.Count;
        _writer.DeclareLocals(localCount);

        // Move first parameter into $this local
        if (hasThis)
        {
            _writer.WriteInstruction("local.get $0");
            _writer.WriteInstruction("local.set $this");
        }

        foreach (var instr in method.Body.Instructions)
        {
            if (instr.OpCode.FlowControl == FlowControl.Branch || instr.OpCode.FlowControl == FlowControl.Cond_Branch)
            {
                int currentOffset = instr.Offset;

                var targetInstruction = instr.Operand as Instruction;
                if (targetInstruction == null)
                    continue;

                int targetOffset = targetInstruction.Offset;

                // Skip trivial jumps
                if (currentOffset + instr.GetSize() == targetOffset)
                    continue;

                if (targetOffset < currentOffset)
                    continue; // Ignore backward branches for now, in the future they'll be loops

                // Emit block
                var label = Guid.NewGuid().ToString("N").Substring(0, 8);
                _writer.WriteInstruction($"(block ${label}");
                _currentLabel = label;

                // Track target offsets for closing blocks
                if (!_closingOffsets.Contains(targetOffset))
                    _closingOffsets.Add(targetOffset);

                // If the previous instruction pushed to the stack, we need to duplicate it in the block
                var prevInstr = instr.Previous;
                if (prevInstr != null && (prevInstr.OpCode.Code == Code.Ldloc || prevInstr.OpCode.Code == Code.Ldloc_0 ||
                                          prevInstr.OpCode.Code == Code.Ldloc_1 || prevInstr.OpCode.Code == Code.Ldloc_2 ||
                                          prevInstr.OpCode.Code == Code.Ldloc_3 || prevInstr.OpCode.Code == Code.Ldloc_S))
                {
                    VisitInstruction(prevInstr);
                }
            }

            VisitInstruction(instr);

            // Close any open blocks
            if (_closingOffsets.Contains(instr.Offset))
            {
                _writer.WriteInstruction("drop");
                _writer.WriteInstruction(")");
                _writer.WriteInstruction("local.get 1");
                _closingOffsets.Remove(instr.Offset);
                _currentLabel = null;
            }
        }


        _writer.EndFunction();
        if (method.IsPublic && method.IsStatic)
            _writer.ExportFunction(Conversion.GetWasmMethodName(method));
    }


    public void VisitInstruction(Instruction instruction)
    {
        // Skip EmitWat's string operand as to not allocate memory for a string that doesn't actually exist
        if (instruction.OpCode.Code == Code.Ldstr &&
            instruction.Next?.OpCode.Code == Code.Call &&
            instruction.Next.Operand is MethodReference nextMethod &&
            nextMethod.FullName == "System.Void IL2Wasm.BaseLib.Compilation::EmitWat(System.String)")
        {
            return;
        }

        // Detect call to Compilation.EmitWat and add inline wat
        if (instruction.OpCode.Code == Code.Call)
        {
            if (instruction.Operand is MethodReference methodRef &&
                methodRef.FullName == "System.Void IL2Wasm.BaseLib.Compilation::EmitWat(System.String)")
            {
                // Look at previous instruction for ldstr
                var prev = instruction.Previous;
                if (prev != null && prev.OpCode.Code == Code.Ldstr)
                {
                    string watCode = prev.Operand as string ?? string.Empty;
                    _writer.WriteInstruction(watCode);
                    return;
                }
            }
        }

        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(instruction))
            {
                handler.CurrentLabel = _currentLabel;
                _writer.WriteInstruction(handler.Handle(instruction));
                return;
            }
        }
    }
}
