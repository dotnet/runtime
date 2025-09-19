# System.Runtime.InteropServices.JavaScript

Is TypeScript project that is compiled via `src\native\libs\rollup.config.js` into Emscripten library.
And native library compiled by CMake into `System.JavaScript.a` as part of `/src/native/libs/CMakeLists.txt`

# System.Runtime.InteropServices.JavaScript.ts -> dotnet.native.js

This is root of **Emscripten library** that would become part of `dotnet.runtime.js`
It implements native part of interop between JS and .NET
Functions exported from `runtime/native-exports.ts` will be added into `dotnet.native.js` in trimmable way.

# ententrypoints.c -> System.Runtime.InteropServices.JavaScript.a -> dotnet.native.wasm

This is making functions from `runtime/native-exports.ts` visible in C code.

**TODOWASM**: This is preventing trimming and should be replaced by generated P/Invokes that are IL trimmed first.
