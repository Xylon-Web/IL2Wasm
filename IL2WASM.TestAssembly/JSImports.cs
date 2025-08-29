using IL2Wasm.CLI.Interop;

namespace IL2Wasm.TestAssembly;

public static class JSImports
{
    [JSImport("math", "add")]
    public extern static int Add(int a, int b);
}
