using System.IO;
using System.Linq;

namespace IL2Wasm.CLI;

internal interface IWatWriter
{
    void BeginModule();
    void EndModule();

    void DeclareMemory(int initialPages = 1);
    void DeclareGlobal(string name, string type, bool isMutable, string? initialValue = null);
    void DeclareImport(string funcName, string moduleName, List<string> paramTypes, string? returnType);

    void BeginFunction(string name, string? returnType = null, List<string>? parameters = null);
    void EndFunction();
    void DeclareLocals(int count);
    void DeclareLocal(string name, string type);
    void WriteInstruction(string line);
    void ExportFunction(string name);
}

/// <summary>
/// Writes WebAssembly Text Format (WAT) to a stream.
/// </summary>
internal class TextWatWriter : IWatWriter
{
    private readonly StreamWriter _writer;

    /// <summary> Creates a new instance of <see cref="TextWatWriter"/> </summary>
    /// <param name="stream">Stream Writer</param>
    public TextWatWriter(Stream stream) => _writer = new StreamWriter(stream);

    public void BeginModule()
    {
        _writer.WriteLine("(module");
    }

    public void EndModule()
    {
        _writer.WriteLine(")");
    }

    public void DeclareMemory(int initialPages = 1)
    {
        _writer.WriteLine($"  (memory $mem {initialPages})");
        _writer.WriteLine("  (export \"memory\" (memory $mem))");
    }

    public void DeclareGlobal(string name, string type, bool isMutable, string? initialValue = null)
    {
        var mut = isMutable ? $"(mut {type})" : type;
        var init = initialValue != null ? $"(i32.const {initialValue})" : "(i32.const 0)";
        _writer.WriteLine($"  (global ${name} {mut} {init})");
    }

    public void DeclareImport(string funcName, string moduleName, List<string> paramTypes, string? returnType)
    {
        var paramStr = paramTypes.Count > 0
            ? string.Join(" ", paramTypes.Select((t, i) => $"(param ${i} {t})"))
            : string.Empty;
        var resultStr = returnType != null ? $"(result {returnType})" : string.Empty;

        _writer.WriteLine($"  (import \"{moduleName}\" \"{funcName}\" (func ${funcName} {paramStr} {resultStr}))");
    }

    public void BeginFunction(string name, string? returnType = null, List<string>? parameters = null)
    {
        var paramStr = parameters is { Count: > 0 }
            ? string.Join(" ", parameters.Select((t, i) => $"(param ${i} {t})"))
            : string.Empty;

        if (returnType != null)
            _writer.WriteLine($"  (func ${name} {paramStr} (result {returnType})");
        else
            _writer.WriteLine($"  (func ${name} {paramStr}");
    }

    public void EndFunction()
    {
        _writer.WriteLine("  )");
    }

    public void DeclareLocals(int count)
    {
        if (count > 0)
        {
            var locals = string.Join(" ", Enumerable.Repeat("i32", count));
            _writer.WriteLine($"    (local {locals})");
        }
    }

    public void DeclareLocal(string name, string type)
    {
        _writer.WriteLine($"    (local ${name} {type})");
    }

    public void WriteInstruction(string line)
    {
        _writer.WriteLine($"    {line}");
    }

    public void ExportFunction(string name)
    {
        _writer.WriteLine($"  (export \"{name}\" (func ${name}))");
    }

    public void Flush()
    {
        _writer.Flush();
    }
}
