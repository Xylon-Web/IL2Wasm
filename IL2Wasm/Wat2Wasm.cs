using Binaryen.NET;

namespace IL2Wasm;

public static class Wat2Wasm
{
    public static byte[] Compile(string inputWat)
    {
        BinaryenModule module = BinaryenModule.Parse(inputWat);
        return module.ToBinary();
    }
}
