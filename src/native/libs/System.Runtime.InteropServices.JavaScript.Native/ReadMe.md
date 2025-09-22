# System.Runtime.InteropServices.JavaScript.Native

This library implements interop between JS and .NET
This library also implements public JS API related JS interop.

## Native interop helpers
- `native/index` compiled -> `libSystem.Runtime.InteropServices.JavaScript.Native.js` linked ->`dotnet.native.js`
- `libSystem.Runtime.InteropServices.JavaScript.Native.footer.js` compiled -> `libSystem.Runtime.InteropServices.JavaScript.Native.js` linked ->`dotnet.native.js`
- `ententrypoints.c` compiled -> `libSystem.Runtime.InteropServices.JavaScript.Native.a` linked -> `dotnet.native.wasm`

## JavaScript interop helpers
- `dotnet.runtime.ts` compiled -> `dotnet.runtime.js`

## Build
TypeScript is compiled by `src/native/rollup.config.js`
JavaScript tools like `npm`, `tsc`, `rollup` are installed in `src/native/package.json`. 
JS tools depend on `nodeJS` installation of Emscripten.
Emscripten compilations is part of `/src/native/libs/CMakeLists.txt`
Final static linking happens in `/src/native/corehost/browserhost/CMakeLists.txt`
