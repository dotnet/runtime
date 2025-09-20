# System.Native.Browser

This library implements PAL for the VM/runtime.
It also implements part of public JS API related to memory and runtime hosting.

It is a TypeScript project that is compiled via `src\native\rollup.config.js` into **Emscripten library**.
And a native library compiled by CMake into `libSystem.Native.Browser.a` as part of `/src/native/libs/CMakeLists.txt`

## Emscripten library
- `libSystem.Native.Browser.ts` compiled -> `libSystem.Native.Browser.js` linked ->`dotnet.native.js`
- `ententrypoints.c` compiled -> `libSystem.Native.Browser.a` linked -> `dotnet.native.wasm`

TypeScript is driven by `src/native/rollup.config.js`
Emscripten compilations is part of `/src/native/libs/CMakeLists.txt`
Final static linking happens in `/src/native/corehost/browserhost/CMakeLists.txt`
