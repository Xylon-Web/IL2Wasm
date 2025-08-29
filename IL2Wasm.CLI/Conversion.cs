using Mono.Cecil;

namespace IL2Wasm.CLI;

/// <summary>
/// Utility class for type conversions.
/// </summary>
internal static class Conversion
{
    /// <summary>
    /// Converts a Mono.Cecil TypeReference to a WebAssembly type string.
    /// </summary>
    /// <param name="type">TypeReference.</param>
    /// <returns>WebAssembly type.</returns>
    public static string? GetWatType(TypeReference type)
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

}
