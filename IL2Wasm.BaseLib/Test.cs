
namespace IL2Wasm.BaseLib;

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
        if (Value == 13)
        {
            return 32;
        }

        return Value;
    }
}
