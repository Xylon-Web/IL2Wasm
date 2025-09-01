
namespace IL2Wasm.BaseLib;

#pragma warning disable CS0626 // Allow extern methods (compiler handles these)

/// <summary>
/// Compile-time interactions.
/// </summary>
public static class Compilation
{
    /// <summary>
    /// Emits inline WAT code.
    /// </summary>
    /// <param name="wat">WAT code</param>
    public static extern void EmitWat(string wat);
}
