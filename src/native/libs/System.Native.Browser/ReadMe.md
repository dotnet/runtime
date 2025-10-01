# System.Native.Browser

This library implements PAL for the VM/runtime.

It is a TypeScript project that is compiled via `src\native\rollup.config.js` into **Emscripten library**.
And a native library compiled by CMake into `libSystem.Native.Browser.a` as part of `/src/native/libs/CMakeLists.txt`

## Emscripten library
- `native/index` compiled -> `libSystem.Native.Browser.js` linked ->`dotnet.native.js`
- `libSystem.Native.Browser.footer.js` compiled -> `libSystem.Native.Browser.js` linked ->`dotnet.native.js`
- `libSystem.Browser.Utils.footer.js` compiled -> `libSystem.Browser.Utils.js` linked ->`dotnet.native.js`
- `libSystem.Native.Browser.extpost.js` linked ->`dotnet.native.js`
- `ententrypoints.c` compiled -> `libSystem.Native.Browser.a` linked -> `dotnet.native.wasm`

## Build
TypeScript is compiled by `src/native/rollup.config.js`
JavaScript tools like `npm`, `tsc`, `rollup` are installed in `src/native/package.json`.
JS tools depend on `nodeJS` installation of Emscripten.
Emscripten compilations is part of `/src/native/libs/CMakeLists.txt`
Final static linking happens in `/src/native/corehost/browserhost/CMakeLists.txt`
