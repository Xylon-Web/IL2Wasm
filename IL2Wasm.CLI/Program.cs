
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IL2Wasm.Compilation;
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

        using var stream = new MemoryStream();
        var writer = new TextWatWriter(stream);

        // Discover and instantiate all instruction handlers marked with [ILInstructionHandler]
        var handlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IInstructionHandler).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && t.GetCustomAttribute<ILInstructionHandlerAttribute>() != null)
            .Select(t => (IInstructionHandler)Activator.CreateInstance(t)!)
            .ToList();

        // Add default fallback handler
        handlers.Add(new DefaultInstructionHandler());

        // Create compiler using visitor pattern
        var compiler = new CompilerVisitor(writer, handlers);

        // Generate WAT by visiting the assembly
        compiler.VisitAssembly(assembly);

        // Output the generated WAT
        var wasmBytes = stream.ToArray();
        Console.WriteLine($"WAT:\n{Encoding.UTF8.GetString(wasmBytes)}");

#else
        // ------------------------
        // Release mode: Compile from provided assembly path
        // ------------------------
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: IL2Wasm <assembly-path>");
            return;
        }

        var assemblyPath = args[0];
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

        // Compile to WASM
        var wasmBytes = Compiler.Compile(assembly);

        // Write output to file
        File.WriteAllBytes("output.wasm", wasmBytes);
        Console.WriteLine("WASM compilation complete: output.wasm");
#endif
    }
}
