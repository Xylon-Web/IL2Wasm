
namespace IL2Wasm.Interop;

/// <summary>
/// Indicates that the decorated method should have its symbol name emitted exactly as declared, without any compiler-generated name mangling or decoration.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NoMangleAttribute : Attribute { }
