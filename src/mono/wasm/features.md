# Browser or JS engine features

dotnet for wasm could be compiled with various MsBuild flags which enable use of browser features. If you need to target older version of the browser, you may need to disable some of the dotnet features or optimizations.

For full set of MSBuild properties [see top of](src\mono\wasm\build\WasmApp.targets)
For set of [browser WASM features see](https://webassembly.org/roadmap/)

# Multi-threading
Is enabled by `<WasmEnableThreads>true</WasmEnableThreads>`.
It requires HTTP headers similar to `Cross-Origin-Embedder-Policy:require-corp` and `Cross-Origin-Opener-Policy:same-origin`.
See also https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/SharedArrayBuffer#security_requirements

# SIMD - Single instruction, multiple data
Is performance optimization enabled by default. It requires recent version of browser.
You can disable it by `<WasmEnableSIMD>false</WasmEnableSIMD><WasmBuildNative>true</WasmBuildNative>`.
[See also](https://github.com/WebAssembly/simd/blob/master/proposals/simd/SIMD.md)

# EH - Exception handling
Is performance optimization enabled by default. It requires recent version of browser.
You can disable it by `<WasmEnableExceptionHandling>false</WasmEnableExceptionHandling><WasmBuildNative>true</WasmBuildNative>`.
[See also](https://github.com/WebAssembly/exception-handling/blob/master/proposals/exception-handling/Exceptions.md)

# BigInt
Is required if the application uses Int64 marshaling in JS interop.
[See also](https://github.com/WebAssembly/JS-BigInt-integration)

# fetch browser API
Is required if the application uses [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)
NodeJS needs to install `node-fetch` and `node-abort-controller` npm packages.

# WebSocket browser API
Is required if the application uses [WebSocketClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket)
NodeJS needs to install `ws` npm package.
