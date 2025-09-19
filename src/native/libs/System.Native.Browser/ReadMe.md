# System.JavaScript

Is TypeScript project that is compiled via `src\native\libs\rollup.config.js` into Emscripten JS library.
And a native library compiled by CMake into `System.JavaScript.a` as part of `/src/native/libs/CMakeLists.txt`

## System.JavaScript.ts -> dotnet.native.js

This is root of **Emscripten library** that would become part of `dotnet.runtime.js`
It implements native parts of PAL for the VM/runtime.

## ententrypoints.c -> System.JavaScript.a -> dotnet.native.wasm

This is making functions from `runtime/native-exports.ts` visible in C code.

**TODOWASM**: This is preventing trimming and should be replaced by generated P/Invokes that are IL trimmed first.
