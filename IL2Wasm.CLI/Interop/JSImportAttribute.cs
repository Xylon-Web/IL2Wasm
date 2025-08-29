
namespace IL2Wasm.CLI.Interop;

/// <summary>
/// Marks a externed method as a JavaScript import.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class JSImportAttribute : Attribute
{
    /// <summary>
    /// Name of the JS module (e.g., "console").
    /// </summary>
    public string Module { get; }

    /// <summary>
    /// Name of the JS function (e.g., "log").
    /// </summary>
    public string Name { get; }

    public JSImportAttribute(string module, string name)
    {
        Module = module;
        Name = name;
    }
}
