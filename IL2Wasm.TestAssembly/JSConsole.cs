using IL2Wasm.Interop;

namespace IL2Wasm.TestAssembly;

public static class JSConsole
{
    [JSImport("console", "log")]
    public extern static int Log(string message);
}
