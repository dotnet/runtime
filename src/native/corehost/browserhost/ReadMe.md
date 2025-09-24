# browserhost

**Emscripten application** and **JavaScript wrapper and a loader** for it.

## Loader dotnet.js

It loads application manifest and based on the manifest, it downloads other JS modules, managed assemblies and other assets.
It plays role of a host and a public API for JavaScript user code.
It's good to keep this file small, so that it could start the download cascade as soon as possible.
It's **JavaScript ES6 module**

- `loader/dotnet.ts` compiled -> `dotnet.js`

## Native host

Implements native part of the CoreCLR host and exposes it as an internal JavaScript interface to the loader.
It is **Emscripten application** statically linked from libraries.

- `host/index.ts` -> compiled -> `libBrowserHost.js` linked -> `dotnet.native.js`
- `libBrowserHost.footer.js` -> compiled -> `libBrowserHost.js` linked -> `dotnet.native.js`
- `libSystem.Native.Browser.js` linked -> `dotnet.native.js`
- `libSystem.Runtime.InteropServices.JavaScript.Native.js` linked -> `dotnet.native.js`
- `browserhost.cpp` compiled + linked -> `dotnet.native.wasm`
- `libSystem.Native.Browser.a` linked -> `dotnet.native.wasm`
- `libSystem.Runtime.InteropServices.JavaScript.Native.a` linked -> `dotnet.native.wasm`

## Build
TypeScript is compiled by `src/native/rollup.config.js`
JavaScript tools like `npm`, `tsc`, `rollup` are installed in `src/native/package.json`.
JS tools depend on `nodeJS` installation of Emscripten.
Emscripten compilations is part of `src/native/corehost/CMakeLists.txt`
Final app static linking happens here in `CMakeLists.txt`
