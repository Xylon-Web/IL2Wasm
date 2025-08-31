
namespace IL2Wasm.TestAssembly;

internal static class Test
{
    public static int Value
    {
        get
        {
            return 13;
        }

        set
        {
        }
    }

    public static int TestMethod()
    {
        Value = 42;
        return Value;
    }
}
