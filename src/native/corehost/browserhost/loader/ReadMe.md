# System.JavaScript.Loader

Is TypeScript project that is compiled via `src\native\libs\Browser\rollup.config.js` into JavaScript ES6 module.

## Dotnet.Loader.ts -> dotnet.js

`System.JavaScript.Loader.ts` is root of **JavaScript module** that would become of `dotnet.js`.
It implements host for the browser together with `src/native/corehost/browserhost`.
It exposes the public JS runtime APIs that is implemented in `Dotnet.Runtime.ts`.
It's good to keep this file small.


And one native library compiled by CMake into `System.JavaScript.a` as part of `/src/native/libs/CMakeLists.txt`


