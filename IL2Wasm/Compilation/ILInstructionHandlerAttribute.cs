namespace IL2Wasm.Compilation;

/// <summary>
/// Defines a class as an IL handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ILInstructionHandlerAttribute : Attribute { }
