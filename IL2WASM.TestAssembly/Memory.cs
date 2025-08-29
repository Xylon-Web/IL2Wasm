namespace IL2WASM.TestAssembly;

public static class Memory
{
    /// <summary>
    /// Pointer to the end of the linear pool
    /// </summary>
    public static int LinearPointer = 0;

    /// <summary>
    /// Allocates memory to the linear pool
    /// </summary>
    /// <param name="size">Size in bytes.</param>
    /// <returns>Pointer to allocated memory.</returns>
    public static int __alloc(int size)
    {
        LinearPointer += size;
        return LinearPointer;
    }
}
