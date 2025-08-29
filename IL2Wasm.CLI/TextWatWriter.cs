using System.IO;

namespace IL2Wasm.CLI;

internal interface IWatWriter
{
    void BeginModule();
    void EndModule();
    void BeginFunction(string name, string? returnType = null, List<string>? parameters = null);
    void EndFunction();
    void DeclareLocals(int count);
    void DeclareLocal(string name, string type);
    void DeclareImport(string funcName, string moduleName, List<string> paramTypes, string? returnType);
    void DeclareMemory(int initialPages = 1);
    void WriteInstruction(string line);
    void ExportFunction(string name);
    void DeclareGlobal(string name, string type, bool isMutable, string? initialValue = null);
}

internal class TextWatWriter : IWatWriter
{
    private readonly StreamWriter _writer;

    public TextWatWriter(Stream stream) => _writer = new StreamWriter(stream);

    public void BeginModule()
    {
        _writer.WriteLine("(module");
    }
    public void EndModule() => _writer.WriteLine(")");

    public void DeclareMemory(int initialPages = 1)
    {
        _writer.WriteLine($"  (memory $mem {initialPages})");
        _writer.WriteLine($"  (export \"memory\" (memory $mem))");
    }

    public void BeginFunction(string name, string? returnType = null, List<string>? parameters = null)
    {
        string paramStr = parameters != null && parameters.Count > 0
            ? string.Join(" ", parameters.Select((t, i) => $"(param ${i} {t})"))
            : "";

        if (returnType != null)
            _writer.WriteLine($"  (func ${name} {paramStr} (result {returnType})");
        else
            _writer.WriteLine($"  (func ${name} {paramStr}");
    }

    public void EndFunction() => _writer.WriteLine("  )");

    public void DeclareLocal(string name, string type)
    {
        _writer.WriteLine($"    (local ${name} {type})");
    }

    public void DeclareLocals(int count)
    {
        if (count > 0)
            _writer.WriteLine($"    (local {string.Join(" ", new string[count].Select(_ => "i32"))})");
    }

    public void WriteInstruction(string line) => _writer.WriteLine($"    {line}");
    public void ExportFunction(string name) => _writer.WriteLine($"  (export \"{name}\" (func ${name}))");
    public void DeclareGlobal(string name, string type, bool isMutable, string? initialValue = null)
    {
        string mut = isMutable ? "(mut " + type + ")" : type;
        string init = initialValue != null ? $"(i32.const {initialValue})" : $"(i32.const 0)";
        _writer.WriteLine($"  (global ${name} {mut} {init})");
    }

    public void DeclareImport(string funcName, string moduleName, List<string> paramTypes, string? returnType)
    {
        string paramStr = paramTypes.Count > 0
            ? string.Join(" ", paramTypes.Select((t, i) => $"(param ${i} {t})"))
            : "";
        string resultStr = returnType != null ? $"(result {returnType})" : "";
        _writer.WriteLine($"  (import \"{moduleName}\" \"{funcName}\" (func ${funcName} {paramStr} {resultStr}))");
    }


    public void Flush() => _writer.Flush();
}
