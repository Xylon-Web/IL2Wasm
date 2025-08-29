namespace IL2Wasm.CLI.Compilation;

/// <summary>
/// Defines a class as an IL handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class ILInstructionHandlerAttribute : Attribute { }
