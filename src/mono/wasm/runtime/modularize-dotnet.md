# Linked javascript files
They are emcc way how to extend the dotnet.js script during linking, by appending the scripts.
See https://emscripten.org/docs/tools_reference/emcc.html#emcc-pre-js

There are `-extern-pre-js`,`-pre-js`, `-post-js`, `-extern-post-js`.
In `src\mono\wasm\build\WasmApp.Native.targets` we apply them by file naming convention as: `*.extpre.js`,`*.pre.js`, `*.post.js`, `*.extpost.js`

In `src\mono\wasm\runtime\CMakeLists.txt` which links only in-tree, we use same mapping explicitly. Right now CommonJS is default.

# dotnet.es6.pre.js
- Executed second (2)
- Applied only when linking ES6
- Will check that it was passed `moduleFactory` callback. Because of emscripten reasons it has confusing `createDotnetRuntime` name here.
- Will validate `Module.ready` is left un-overridden.

# runtime.*.iffe.js
- Executed third (3)
- this is produced from `*.ts` files in this directory by rollupJS.

# dotnet.*.post.js
- Executed last (4)
- When `onRuntimeInitialized` is overridden it would wait for emscriptens `Module.ready`
- Otherwise it would wait for MonoVM to load all assets and assemblies.
- It would pass on the API exports

# About new API
The signature is
```
function createDotnetRuntime(moduleFactory: (api: DotnetPublicAPI) => DotnetModuleConfig): Promise<DotNetExports>
```

Simplest intended usage looks like this in ES6:
```
import createDotnetRuntime from './dotnet.js'

await createDotnetRuntime(() => ({
    configSrc: "./mono-config.json",
}));
```

For more complex scenario with using APIs see `src\mono\sample\wasm`
