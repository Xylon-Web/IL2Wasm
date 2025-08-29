using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IL2Wasm.CLI.Compilation;

// ------------------------
// Base instruction handler
// ------------------------
internal interface IInstructionHandler
{
    bool CanHandle(Instruction instr);
    string Handle(Instruction instr);
}

internal class DefaultInstructionHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => true;

    public string Handle(Instruction instr) =>
        instr.Operand != null
            ? $";; Unhandled operand: {instr.OpCode.Code} ({instr.Operand})"
            : $";; Unhandled opcode: {instr.OpCode.Code}";
}

// ------------------------
// Constant loading
// ------------------------
[ILInstructionHandler]
internal class LdcI4Handler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) =>
        instr.OpCode.Code >= Code.Ldc_I4_0 && instr.OpCode.Code <= Code.Ldc_I4_8
        || instr.OpCode.Code == Code.Ldc_I4 || instr.OpCode.Code == Code.Ldc_I4_S;

    public string Handle(Instruction instr)
    {
        int value = instr.Operand != null
            ? Convert.ToInt32(instr.Operand)
            : (int)instr.OpCode.Code - (int)Code.Ldc_I4_0;
        return $"i32.const {value}";
    }
}

// ------------------------
// Local variable handling
// ------------------------
[ILInstructionHandler]
internal class StlocHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) =>
        instr.OpCode.Code == Code.Stloc_0 || instr.OpCode.Code == Code.Stloc_1 ||
        instr.OpCode.Code == Code.Stloc_2 || instr.OpCode.Code == Code.Stloc_3 ||
        instr.OpCode.Code == Code.Stloc_S;

    public string Handle(Instruction instr)
    {
        int index = instr.Operand is VariableDefinition v ? v.Index : instr.OpCode.Code switch
        {
            Code.Stloc_0 => 0,
            Code.Stloc_1 => 1,
            Code.Stloc_2 => 2,
            Code.Stloc_3 => 3,
            _ => -1
        };
        return $"local.set {index}";
    }
}

[ILInstructionHandler]
internal class LdlocHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) =>
        instr.OpCode.Code == Code.Ldloc_0 || instr.OpCode.Code == Code.Ldloc_1 ||
        instr.OpCode.Code == Code.Ldloc_2 || instr.OpCode.Code == Code.Ldloc_3 ||
        instr.OpCode.Code == Code.Ldloc_S;

    public string Handle(Instruction instr)
    {
        int index = instr.Operand is VariableDefinition v ? v.Index : instr.OpCode.Code switch
        {
            Code.Ldloc_0 => 0,
            Code.Ldloc_1 => 1,
            Code.Ldloc_2 => 2,
            Code.Ldloc_3 => 3,
            _ => -1
        };
        return $"local.get {index}";
    }
}

[ILInstructionHandler]
internal class Ldarg_0Handler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ldarg_0;
    public string Handle(Instruction instr) => "local.get 0";
}

// ------------------------
// Control flow
// ------------------------
[ILInstructionHandler]
internal class RetHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ret;
    public string Handle(Instruction instr) => "return";
}

// ------------------------
// Arithmetic / Stack
// ------------------------
[ILInstructionHandler]
internal class AddHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Add;
    public string Handle(Instruction instr) => "i32.add";
}

[ILInstructionHandler]
internal class NopHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Nop;
    public string Handle(Instruction instr) => "nop";
}

[ILInstructionHandler]
internal class PopHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Pop;
    public string Handle(Instruction instr) => "drop";
}

// ------------------------
// Field access
// ------------------------
[ILInstructionHandler]
internal class LdsfldHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ldsfld;

    public string Handle(Instruction instr) =>
        instr.Operand is FieldReference fieldRef
            ? $"global.get ${fieldRef.DeclaringType.Name}_{fieldRef.Name}"
            : ";; Invalid Ldsfld operand";
}

[ILInstructionHandler]
internal class StsfldHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Stsfld;

    public string Handle(Instruction instr) =>
        instr.Operand is FieldReference fieldRef
            ? $"global.set ${fieldRef.DeclaringType.Name}_{fieldRef.Name}"
            : ";; Invalid Stsfld operand";
}

[ILInstructionHandler]
internal class LdfldHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ldfld;

    public string Handle(Instruction instr)
    {
        if (instr.Operand is not FieldReference field) return ";; Invalid ldfld operand";

        var typeDef = field.DeclaringType.Resolve();
        int offset = 0;
        foreach (var f in typeDef.Fields)
        {
            if (f == field) break;
            if (!f.IsStatic)
            {
                offset += f.FieldType.MetadataType switch
                {
                    MetadataType.Int32 => 4,
                    MetadataType.Int64 => 8,
                    MetadataType.Single => 4,
                    MetadataType.Double => 8,
                    _ => 4
                };
            }
        }

        string wasmType = Conversion.GetWatType(field.FieldType) ?? "i32";
        string loadInstr = wasmType + ".load";   // "i32.load", "f32.load", etc.

        return $@"
;; load field {field.Name} from offset {offset} in {typeDef.Name}
i32.const {offset} ;; push offset
i32.add             ;; compute address
{loadInstr}         ;; load value
";
    }
}

[ILInstructionHandler]
internal class StfldHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Stfld;

    public string Handle(Instruction instr)
    {
        if (instr.Operand is not FieldReference field) return ";; Invalid stfld operand";

        var typeDef = field.DeclaringType.Resolve();
        int offset = 0;
        foreach (var f in typeDef.Fields)
        {
            if (f == field) break;
            if (!f.IsStatic)
            {
                offset += f.FieldType.MetadataType switch
                {
                    MetadataType.Int32 => 4,
                    MetadataType.Int64 => 8,
                    MetadataType.Single => 4,
                    MetadataType.Double => 8,
                    _ => 4
                };
            }
        }

        string storeInstr = (Conversion.GetWatType(field.FieldType) ?? "i32") + ".store";

        return $@"
;; store field {field.Name} at offset {offset} in {typeDef.Name}
i32.const {offset}
i32.add
{storeInstr}
";
    }
}

// ------------------------
// Object instantiation
// ------------------------
[ILInstructionHandler]
internal class NewObjHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Newobj;

    public string Handle(Instruction instr)
    {
        if (instr.Operand is not MethodReference ctor) return ";; Invalid newobj operand";

        var typeDef = ctor.DeclaringType.Resolve();
        int size = typeDef.Fields.Where(f => !f.IsStatic).Sum(f => Conversion.GetTypeSize(f.FieldType));

        return $@"
;; allocate {size} bytes for {typeDef.Name}
i32.const {size}
call $__alloc
local.set $this
local.get $this
call ${typeDef.Name}_ctor
local.get $this
";
    }
}

// ------------------------
// Method calls
// ------------------------
[ILInstructionHandler]
internal class CallvirtHandler : IInstructionHandler
{
    public bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Callvirt;

    public string Handle(Instruction instr)
    {
        if (instr.Operand is not MethodReference methodRef) return ";; Invalid callvirt operand";

        string wasmName = $"{methodRef.DeclaringType.Name}_{methodRef.Name}";
        string comment = !methodRef.HasThis || (methodRef.HasThis && !methodRef.Resolve().IsStatic)
            ? $";; callvirt {methodRef.DeclaringType.Name}.{methodRef.Name} (stack: ..., this, args...)"
            : $";; call {methodRef.DeclaringType.Name}.{methodRef.Name} (static method)";

        return $@"
{comment}
call ${wasmName}
";
    }
}
