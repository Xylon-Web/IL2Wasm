# Contributing
## Debugging
For easy debugging, run `IL2Wasm.CLI` in debug mode. This will compile `IL2Wasm.BaseLib` into WAT format.
You can then test or validate this output using the [wat2wasm online demo](https://webassembly.github.io/wabt/demo/wat2wasm/).

## Supporting new IL Instructions
If you're debugging IL2Wasm-compiled WAT and come across a line that looks similar to:
```
;; Unhandled operand: Call (MyNamespace.MyMethod)
```
or
```
;; Unhandled opcode: Nop
```
That means the compiled C# code contains an IL instruction that IL2Wasm does not support.
An IL instruction represents a single operation in the [Common Intermediate Language (CIL)](https://www.geeksforgeeks.org/c-sharp/cil-or-msil-microsoft-intermediate-language-or-common-intermediate-language), describing exactly what the program should do.
IL2Wasm’s primary role is to translate these C# IL instructions into equivalent instructions that WebAssembly can execute.

**So, how can we add support for a new IL Instruction?**

First, understand the instruction and it's purpose.
A helpful resource is Wikipedia's '[List of CIL instructions](https://en.wikipedia.org/wiki/List_of_CIL_instructions)', though other references are also available.

Next, open `ILInstructionHandlers.cs` and add a new Instruction Handler. A typical handler looks like this:
```CSharp
[ILInstructionHandler]
internal class PopHandler : BaseInstructionHandler
{
    public override bool CanHandle(Instruction instr) => instr.OpCode.Code == Code.Pop;
    public override string Handle(Instruction instr) => "drop";
}
```

This Instruction Handler converts C#'s `Pop` to WASM's `drop`.
`CanHandle` tells the compiler which instructions this handler can process, while `Handle` generates the corresponding WAT instructions.
