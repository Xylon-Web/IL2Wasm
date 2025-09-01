using System;
using System.IO;
using System.Reflection;
using System.Text;
using Mono.Cecil;

namespace IL2Wasm;

internal class Program
{
    static void Main(string[] args)
    {
#if DEBUG
        // ------------------------
        // Debug mode: Compile base lib
        // ------------------------
        var assembly = AssemblyDefinition.ReadAssembly("IL2WASM.BaseLib.dll");
        var watBytes = DefaultCompiler.CompileAssembly(assembly);

        Console.WriteLine($"WAT:\n{Encoding.UTF8.GetString(watBytes)}");

        // Write WAT
        string tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, watBytes);

        // Output Wasm
        Wat2Wasm.Compile(tempFile, Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name.Name}.wasm"));
        Console.WriteLine($"WASM compilation complete: {assembly.Name.Name}.wasm");

#else
        // ------------------------
        // Release mode: Compile from provided assembly path
        // ------------------------
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: IL2Wasm <assembly-path>");
            return;
        }

        var watBytes = DefaultCompiler.CompileFromPath(args[0]);

        // Write WAT
        string tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, watBytes);

        // Output Wasm
        Wat2Wasm.Compile(tempFile, Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name}.wasm"));
        Console.WriteLine($"WASM compilation complete: {assembly.Name}.wasm");
#endif
    }
}
