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

        // Output Wasm
        byte[] wasm = Wat2Wasm.Compile(Encoding.UTF8.GetString(watBytes));
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name.Name}.wasm"), wasm);
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

        var assembly = AssemblyDefinition.ReadAssembly(args[0]);
        var watBytes = DefaultCompiler.CompileAssembly(assembly);

        // Output Wasm
        byte[] wasm = Wat2Wasm.Compile(Encoding.UTF8.GetString(watBytes));
        File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), $"{assembly.Name.Name}.wasm"), wasm);
        Console.WriteLine($"WASM compilation complete: {assembly.Name.Name}.wasm");
#endif
    }
}
