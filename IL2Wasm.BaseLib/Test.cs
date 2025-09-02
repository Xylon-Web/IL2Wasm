
namespace IL2Wasm.BaseLib;

internal static class Test
{
    public static int Value;

    public static int TestMethod(int num)
    {
        Value = 7;

        if (Value == num)
            return 999;

        return 0;
    }
}
