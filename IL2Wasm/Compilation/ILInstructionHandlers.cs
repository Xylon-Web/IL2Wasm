using System;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IL2Wasm.Compilation;

// ------------------------
// Base instruction handler
// ------------------------
public abstract class BaseInstructionHandler
{
    public virtual Dictionary<string, string>? LocalVariables { get; set; }

    public abstract bool CanHandle(Instruction instr);
    public abstract string Handle(Instruction instr);
}

public class DefaultInstructionHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => true;

    public override string Handle(Instruction instr) =>
        instr.Operand != null
            ? $";; Unhandled operand: {instr.OpCode.Code} ({instr.Operand})"
            : $";; Unhandled opcode: {instr.OpCode.Code}";
}

// ------------------------
// Constant loading
// ------------------------
[ILInstructionHandler]
internal class LdcI4Handler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) =>
        instr.OpCode.Code >= Code.Ldc_I4_0 && instr.OpCode.Code <= Code.Ldc_I4_8
        || instr.OpCode.Code == Code.Ldc_I4 || instr.OpCode.Code == Code.Ldc_I4_S;

    public override string Handle(Instruction instr)
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
internal class StlocHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) =>
        instr.OpCode.Code == Code.Stloc_0 || instr.OpCode.Code == Code.Stloc_1 ||
        instr.OpCode.Code == Code.Stloc_2 || instr.OpCode.Code == Code.Stloc_3 ||
        instr.OpCode.Code == Code.Stloc_S;

    public override string Handle(Instruction instr)
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
internal class LdlocHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) =>
        instr.OpCode.Code == Code.Ldloc_0 || instr.OpCode.Code == Code.Ldloc_1 ||
        instr.OpCode.Code == Code.Ldloc_2 || instr.OpCode.Code == Code.Ldloc_3 ||
        instr.OpCode.Code == Code.Ldloc_S;

    public override string Handle(Instruction instr)
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
internal class LdargHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) =>
        instr.OpCode.Code is Code.Ldarg_0 or Code.Ldarg_1 or Code.Ldarg_2 or Code.Ldarg_3 or Code.Ldarg_S;

    public override string Handle(Instruction instr)
    {
        int index = instr.Operand is ParameterDefinition p ? p.Index : instr.OpCode.Code switch
        {
            Code.Ldarg_0 => 0,
            Code.Ldarg_1 => 1,
            Code.Ldarg_2 => 2,
            Code.Ldarg_3 => 3,
            _ => -1
        };
        return $"local.get {index}";
    }
}

// ------------------------
// Control flow
// ------------------------
[ILInstructionHandler]
internal class RetHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ret;
    public override string Handle(Instruction instr) => "return";
}

// ------------------------
// Arithmetic / Stack
// ------------------------
[ILInstructionHandler]
internal class ArithmeticHandler : BaseInstructionHandler
{
    private static readonly Dictionary<Code, string> Map = new()
    {
        // Arithmetic
        { Code.Add, "i32.add" },
        { Code.Sub, "i32.sub" },
        { Code.Mul, "i32.mul" },
        { Code.Div, "i32.div_s" },
        { Code.Div_Un, "i32.div_u" },
        { Code.Rem, "i32.rem_s" },
        { Code.Rem_Un, "i32.rem_u" },

        // Bitwise
        { Code.And, "i32.and" },
        { Code.Or, "i32.or" },
        { Code.Xor, "i32.xor" },
        { Code.Shl, "i32.shl" },
        { Code.Shr, "i32.shr_s" },
        { Code.Shr_Un, "i32.shr_u" },

        // Comparison
        { Code.Cgt, "i32.gt_s" },
        { Code.Clt, "i32.lt_s" },
        { Code.Ceq, "i32.eq" },

        // Negation
        { Code.Neg, "i32.const 0\ni32.sub" }
    };

    public override bool CanHandle(Instruction instr) => Map.ContainsKey(instr.OpCode.Code);

    public override string Handle(Instruction instr) => Map[instr.OpCode.Code];
}

[ILInstructionHandler]
internal class NopHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Nop;
    public override string Handle(Instruction instr) => "nop";
}

[ILInstructionHandler]
internal class PopHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Pop;
    public override string Handle(Instruction instr) => "drop";
}


// ------------------------
// Field access
// ------------------------
[ILInstructionHandler]
internal class LdsfldHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ldsfld;

    public override string Handle(Instruction instr) =>
        instr.Operand is FieldReference fieldRef
            ? $"global.get ${Conversion.GetWasmFieldName(fieldRef)}"
            : ";; Invalid Ldsfld operand";
}

[ILInstructionHandler]
internal class StsfldHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Stsfld;

    public override string Handle(Instruction instr) =>
        instr.Operand is FieldReference fieldRef
            ? $"global.set ${Conversion.GetWasmFieldName(fieldRef)}"
            : ";; Invalid Stsfld operand";
}

[ILInstructionHandler]
internal class LdfldHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ldfld;

    public override string Handle(Instruction instr)
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

        string wasmType = Conversion.GetWasmType(field.FieldType) ?? "i32";
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
internal class StfldHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Stfld;

    public override string Handle(Instruction instr)
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

        string storeInstr = (Conversion.GetWasmType(field.FieldType) ?? "i32") + ".store";

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
internal class NewObjHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Newobj;

    public override string Handle(Instruction instr)
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
internal class CallvirtHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Callvirt;

    public override string Handle(Instruction instr)
    {
        if (instr.Operand is not MethodReference methodRef) return ";; Invalid callvirt operand";

        string wasmName = Conversion.GetWasmMethodName(methodRef);
        string comment = !methodRef.HasThis || (methodRef.HasThis && !methodRef.Resolve().IsStatic)
            ? $";; callvirt {methodRef.DeclaringType.Name}.{methodRef.Name} (stack: ..., this, args...)"
            : $";; call {methodRef.DeclaringType.Name}.{methodRef.Name} (static method)";

        return $@"
{comment}
call ${wasmName}
";
    }
}

[ILInstructionHandler]
internal class CallHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Call;
    public override string Handle(Instruction instr)
    {
        if (instr.Operand is not MethodReference methodRef)
            return ";; Invalid call operand";

        string typeName = methodRef.DeclaringType.Name;
        string methodName = methodRef.Name;

        // Handle JS imports
        var resolvedMethod = methodRef.Resolve();
        if (resolvedMethod != null)
        {
            var jsImportAttr = resolvedMethod.CustomAttributes
               .FirstOrDefault(a => a.AttributeType.Name == "JSImportAttribute");

            if (jsImportAttr != null)
            {
                string importName = jsImportAttr.ConstructorArguments[1].Value?.ToString() ?? methodName;
                return $";; JSImport call\ncall ${importName}";
            }
        }

        // Handle DefaultInterpolatedStringHandler helpers
        if (typeName == "DefaultInterpolatedStringHandler")
        {
            return methodName switch
            {
                ".ctor" => "nop ;; interpolated string constructor",
                "AppendLiteral" => "nop ;; append literal",
                "AppendFormatted" => "nop ;; append formatted",
                "ToStringAndClear" => "nop ;; finalize interpolated string",
                _ => $";; call {typeName}.{methodName} (unhandled)"
            };
        }


        // Instance or static method calls
        string wasmName = Conversion.GetWasmMethodName(methodRef);
        string comment = !methodRef.HasThis || (methodRef.HasThis && !methodRef.Resolve().IsStatic)
            ? $";; callvirt {typeName}.{methodName} (stack: ..., this, args...)"
            : $";; call {typeName}.{methodName} (static method)";

        return $@"
{comment}
call ${wasmName}
";
    }

}


// ------------------------
// Strings
// ------------------------
[ILInstructionHandler]
internal class LdstrHandler : BaseInstructionHandler
{
    public override Dictionary<string, string>? LocalVariables => new() { { "strPtr", "i32" } };

    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Ldstr;

    public override string Handle(Instruction instr)
    {
        if (instr.Operand is not string str)
            return ";; Invalid ldstr operand";

        byte[] bytes = Encoding.UTF8.GetBytes(str);
        int length = bytes.Length;

        var sb = new StringBuilder();

        // Total size = 4 bytes for length + string bytes
        int totalSize = 4 + length;
        
        // Allocate memory
        sb.AppendLine($@"
i32.const {totalSize}       ;; total allocation size (4 + string bytes)
call $__alloc               ;; allocate memory
local.set $strPtr            ;; store pointer
");

        // Store string length as little-endian 32-bit integer
        sb.AppendLine($@"
local.get $strPtr
i32.const {length & 0xFF}
i32.store8
local.get $strPtr
i32.const 1
i32.add
i32.const {(length >> 8) & 0xFF}
i32.store8
local.get $strPtr
i32.const 2
i32.add
i32.const {(length >> 16) & 0xFF}
i32.store8
local.get $strPtr
i32.const 3
i32.add
i32.const {(length >> 24) & 0xFF}
i32.store8
");

        // Store each UTF-8 byte after the 4-byte length prefix
        for (int i = 0; i < bytes.Length; i++)
        {
            sb.AppendLine($@"
local.get $strPtr
i32.const {4 + i}
i32.add
i32.const {bytes[i]}
i32.store8
");
        }

        // Push pointer onto stack
        sb.AppendLine("local.get $strPtr");

        return sb.ToString();
    }
}
