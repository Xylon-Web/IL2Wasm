
namespace IL2Wasm.TestAssembly;

/// <summary>
/// A sample class to demonstrate IL to WASM conversion.
/// </summary>
/// <remarks>A <see cref="Widget"/> is a class that hooks into the webpage lifecycle.</remarks>
public class Widget
{
    public static int Add(int a, int b) => JSImports.Add(a, b);
}
