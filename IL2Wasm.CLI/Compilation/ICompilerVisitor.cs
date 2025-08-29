using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IL2Wasm.CLI.Compilation;

internal interface ICompilerVisitor
{
    void VisitAssembly(AssemblyDefinition assembly);
    void VisitModule(ModuleDefinition module);
    void VisitType(TypeDefinition type);
    void VisitMethod(MethodDefinition method);
    void VisitInstruction(Instruction instruction);
}
