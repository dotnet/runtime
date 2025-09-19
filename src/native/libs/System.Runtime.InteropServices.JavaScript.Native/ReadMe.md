# System.Runtime.InteropServices.JavaScript.Native

This library implements interop between JS and .NET
It also implements public JS API related JS interop.

## Emscripten library
- `libSystem.Runtime.InteropServices.JavaScript.Native.ts` compiled -> `libSystem.Runtime.InteropServices.JavaScript.Native.js` linked -> `dotnet.native.js`
- `ententrypoints.c` compiled -> `libSystem.Runtime.InteropServices.JavaScript.Native.a` linked -> `dotnet.native.wasm`

## ES6 JavaScript module
- `dotnet.runtime.ts` compiled -> `dotnet.runtime.js`

TypeScript is driven by `src/native/rollup.config.js`
Emscripten compilations is part of `/src/native/libs/CMakeLists.txt`
Final static linking happens in `/src/native/corehost/browserhost/CMakeLists.txt`
