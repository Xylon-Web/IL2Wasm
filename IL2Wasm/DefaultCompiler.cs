using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using IL2Wasm.Compilation;
using Mono.Cecil;

namespace IL2Wasm;

public static class DefaultCompiler
{
    public static byte[] CompileAssembly(AssemblyDefinition assembly)
    {
        using var stream = new MemoryStream();
        var writer = new TextWatWriter(stream);

        // Discover and instantiate all instruction handlers marked with [ILInstructionHandler]
        var handlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(BaseInstructionHandler).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && t.GetCustomAttribute<ILInstructionHandlerAttribute>() != null)
            .Select(t => (BaseInstructionHandler)Activator.CreateInstance(t)!)
            .ToList();

        // Add default fallback handler
        handlers.Add(new DefaultInstructionHandler());

        // Create compiler using visitor pattern
        var compiler = new CompilerVisitor(writer, handlers);

        // Generate WAT by visiting the assembly
        compiler.VisitAssembly(assembly);

        return stream.ToArray();
    }

    public static byte[] CompileAssembly(string assemblyPath)
    {
        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        return CompileAssembly(assembly);
    }
}
