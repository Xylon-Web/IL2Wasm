using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IL2Wasm;

/// <summary>
/// TODO: Replace this with a P/Invoke to the actual wat2wasm tool from WebAssembly Binary Toolkit (WABT).
/// </summary>
public static class Wat2Wasm
{
    public static void Compile(string inputFile, string outputPath)
    {
        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "wat2wasm";
        process.StartInfo.Arguments = $"\"{inputFile}\" -o \"{outputPath}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception($"wat2wasm failed:\n{stderr}\n{stdout}");
    }
}
