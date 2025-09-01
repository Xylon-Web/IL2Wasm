
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
        Compilation.EmitWat(";; TEST INLINE WAT");
        Value = 42;
        return Value;
    }
}
