using System.Text;
using Mono.Cecil;

namespace IL2Wasm;

/// <summary>
/// Utility class for converting C# to WASM.
/// </summary>
internal static class Conversion
{
    /// <summary>
    /// Converts a Mono.Cecil TypeReference to a WebAssembly type string.
    /// </summary>
    /// <param name="type">TypeReference.</param>
    /// <returns>WebAssembly type.</returns>
    public static string? GetWasmType(TypeReference type)
    {
        return type.MetadataType switch
        {
            MetadataType.Int32 => "i32",
            MetadataType.Int64 => "i64",
            MetadataType.Single => "f32",
            MetadataType.Double => "f64",
            _ => null
        };
    }

    /// <summary>
    /// Returns the size of a type in bytes.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static int GetTypeSize(TypeReference type) => type.MetadataType switch
    {
        MetadataType.Int32 => 4,
        MetadataType.Int64 => 8,
        MetadataType.Single => 4,
        MetadataType.Double => 8,
        _ => 4
    };

    /// <summary>
    /// Generates a valid and unique WebAssembly symbol name for a method.
    /// Replaces illegal characters and includes parameter signature to avoid collisions.
    /// </summary>
    public static string GetWasmMethodName(MethodReference method)
    {
        var sb = new StringBuilder();

        // Start with declaring type
        if (!string.IsNullOrEmpty(method.DeclaringType.Namespace))
        {
            sb.Append(method.DeclaringType.Namespace.Replace('.', '_'));
            sb.Append('_');
        }
        sb.Append(method.DeclaringType.Name.Replace('`', '_')); // handle generics

        sb.Append('_');
        sb.Append(method.Name.Replace('.', '_'));

        // Add parameter types to make it unique if overloads exist
        if (method.HasParameters)
        {
            sb.Append('_');
            foreach (var p in method.Parameters)
            {
                sb.Append(p.ParameterType.MetadataType);
                sb.Append('_');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a valid WebAssembly symbol for a field.
    /// </summary>
    public static string GetWasmFieldName(FieldReference field)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(field.DeclaringType.Namespace))
        {
            sb.Append(field.DeclaringType.Namespace.Replace('.', '_'));
            sb.Append('_');
        }
        sb.Append(field.DeclaringType.Name.Replace('`', '_'));
        sb.Append('_');
        sb.Append(field.Name);

        return sb.ToString();
    }
}
