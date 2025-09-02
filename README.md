# IL2Wasm
An experimental compiler that converts C# Intermediate Language (IL) into WebAssembly (WASM).

![Version](https://img.shields.io/badge/Version-0.0.1-orange)
> [!IMPORTANT]
> IL2Wasm is a highly experimental, work-in-progress tool. **It is not yet production-ready.**

## Features
- Embed raw WAT directly inside a method using `IL2Wasm.BaseLib.Compilation.EmitWat("WAT String");`
- Mark methods with `[NoMangle]` to make them easily callable from JavaScript.
- Use `[JSImport]` on extern methods to link them to corresponding JavaScript imports.

## Requirements
- To compile WAT (WebAssembly Text) into WASM, `wat2wasm` needs to be installed and on your system PATH.
- `wat2wasm` is not required if you only need to inspect or view the generated WAT output.

## Contributing
Want to contribute? Check out [CONTRIBUTING](contributing.md) to get a grasp on the codebase.
