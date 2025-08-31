using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IL2Wasm.CLI.Compilation;

internal class CompilerVisitor : ICompilerVisitor
{
    private readonly IWatWriter _writer;
    private readonly List<IInstructionHandler> _handlers;
    private readonly List<(string module, string name, MethodDefinition method)> _jsImports = new();

    public CompilerVisitor(IWatWriter writer, IEnumerable<IInstructionHandler> handlers)
    {
        _writer = writer;
        _handlers = new List<IInstructionHandler>(handlers);
    }

    public void VisitAssembly(AssemblyDefinition assembly)
    {
        // Collect imports first
        foreach (var module in assembly.Modules)
            foreach (var type in module.Types)
                foreach (var method in type.Methods)
                {
                    var jsAttr = method.CustomAttributes
                        .FirstOrDefault(a => a.AttributeType.FullName == "IL2Wasm.CLI.Interop.JSImportAttribute");
                    if (jsAttr != null)
                    {
                        string moduleName = jsAttr.ConstructorArguments[0].Value?.ToString() ?? "env";
                        string name = jsAttr.ConstructorArguments[1].Value?.ToString() ?? method.Name;
                        _jsImports.Add((moduleName, name, method));
                    }
                }

        _writer.BeginModule();

        // Emit all imports first
        foreach (var import in _jsImports)
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
        if (!method.HasBody || method.CustomAttributes.Any(a => a.AttributeType.FullName == "IL2Wasm.CLI.Interop.JSImportAttribute"))
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
            name = $"{method.DeclaringType.Name}_{method.Name}";
            paramTypes.Insert(0, "i32");
        }
        else
        {
            name = method.Name; // static method
        }

        _writer.BeginFunction(name, returnType, paramTypes);

        // Declare $this local
        if (hasThis || usesNewObj || instanceMethod)
            _writer.DeclareLocal("this", "i32");

        // Detect if method uses any ldstr instructions to declare $strPtr
        bool usesLdstr = method.Body.Instructions.Any(instr => instr.OpCode.Code == Code.Ldstr);
        if (usesLdstr)
            _writer.DeclareLocal("strPtr", "i32");

        // Declare locals
        int localCount = method.Body.Variables.Count;
        _writer.DeclareLocals(localCount);

        // Move first parameter into $this local
        if (hasThis)
        {
            _writer.WriteInstruction("local.get $0");
            _writer.WriteInstruction("local.set $this");
        }

        foreach (var instr in method.Body.Instructions)
            VisitInstruction(instr);

        _writer.EndFunction();
        if (method.IsPublic && method.IsStatic)
            _writer.ExportFunction(method.Name);
    }


    public void VisitInstruction(Instruction instruction)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(instruction))
            {
                _writer.WriteInstruction(handler.Handle(instruction));
                return;
            }
        }
    }
}
