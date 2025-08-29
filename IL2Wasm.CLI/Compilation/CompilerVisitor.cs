using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IL2Wasm.CLI.Compilation;

internal class CompilerVisitor : ICompilerVisitor
{
    private readonly IWatWriter _writer;
    private readonly List<IInstructionHandler> _handlers;

    public CompilerVisitor(IWatWriter writer, IEnumerable<IInstructionHandler> handlers)
    {
        _writer = writer;
        _handlers = new List<IInstructionHandler>(handlers);
    }

    public void VisitAssembly(AssemblyDefinition assembly)
    {
        _writer.BeginModule();
        foreach (var module in assembly.Modules)
            VisitModule(module);
        _writer.EndModule();
        if (_writer is TextWatWriter tw) tw.Flush();
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
                string? watType = Conversion.GetWatType(field.FieldType) ?? "i32";
                string initialValue = watType == "f32" || watType == "f64" ? "0.0" : "0"; // Default to 0
                _writer.DeclareGlobal($"{type.Name}_{field.Name}", watType, isMutable: !field.IsInitOnly, initialValue);
            }
        }

        foreach (var method in type.Methods)
            VisitMethod(method);

        foreach (var nested in type.NestedTypes)
            VisitType(nested);
    }

    public void VisitMethod(MethodDefinition method)
    {
        if (!method.HasBody)
            return;

        string? returnType = Conversion.GetWatType(method.ReturnType);

        // Convert parameters to WASM types
        var paramTypes = new List<string>();
        foreach (var param in method.Parameters)
        {
            string? watType = Conversion.GetWatType(param.ParameterType) ?? "i32";
            paramTypes.Add(watType);
        }


        // Track whether we need a 'this' pointer
        bool hasThis = method.IsConstructor && !method.IsStatic;
        bool usesNewObj = method.Body.Instructions.Any(instr => instr.OpCode.Code == Code.Newobj);
        bool instanceMethod = !method.IsStatic && !method.IsConstructor;

        string name;
        if (method.IsConstructor && !method.IsStatic)
        {
            name = $"{method.DeclaringType.Name}_ctor"; // Use _ctor instead of .ctor
            paramTypes.Insert(0, "i32"); // this pointer
        }
        else if (!method.IsStatic) // instance method
        {
            name = $"{method.DeclaringType.Name}_{method.Name}";
            paramTypes.Insert(0, "i32"); // this pointer
        }
        else
        {
            name = method.Name; // static method
        }

        _writer.BeginFunction(name, returnType, paramTypes);

        // Declare $this local
        if (hasThis || usesNewObj || instanceMethod)
            _writer.DeclareLocal("this", "i32");

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
