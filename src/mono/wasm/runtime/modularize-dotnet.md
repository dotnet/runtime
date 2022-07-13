# Linked javascript files
They are emcc way how to extend the dotnet.js script during linking, by appending the scripts.
See https://emscripten.org/docs/tools_reference/emcc.html#emcc-pre-js

There are `-extern-pre-js`,`-pre-js`, `-post-js`, `-extern-post-js`.
In `src\mono\wasm\build\WasmApp.Native.targets` we apply them by file naming convention as: `*.extpre.js`,`*.pre.js`, `*.post.js`, `*.extpost.js`
- For ES6 with `WasmEnableES6 == true` from `src/es6`folder
- For CommonJS with `WasmEnableES6 == false` from `src/cjs`folder

In `src\mono\wasm\runtime\CMakeLists.txt` which links only in-tree, we use same mapping explicitly. Right now CommonJS is default.

# dotnet.cjs.extpost.js
- Is at the end of file but is executed first (1)
- Applied only when linking CommonJS
- If `globalThis.Module` exist it takes it and start runtime with it.
- Otherwise user could still use the `createDotnetRuntime` export or `globalThis.createDotnetRuntime` if it was loaded into global namespace.

# dotnet.cjs.pre.js
- Executed second (2)
- Applied only when linking CommonJS
- Will try to see if it was executed with `globalThis.Module` and if so, it would use it's instance as `Module`. It would preserve emscripten's `Module.ready`
- Otherwise it would load it would assume it was called via `createDotnetRuntime` export same as described for `dotnet.es6.pre.js` below.

# dotnet.es6.pre.js
- Executed second (2)
- Applied only when linking ES6
- Will check that it was passed `moduleFactory` callback. Because of emscripten reasons it has confusing `createDotnetRuntime` name here.
- Will validate `Module.ready` is left un-overriden.

# runtime.*.iffe.js
- Executed third (3)
- this is produced from `*.ts` files in this directory by rollupJS.

# dotnet.*.post.js
- Executed last (4)
- When `onRuntimeInitialized` is overriden it would wait for emscriptens `Module.ready`
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

More complex scenario with using APIs, commented
```
import createDotnetRuntime from './dotnet.js'

export const { MONO, BINDING } = await createDotnetRuntime(({ MONO, BINDING, Module }) =>
// this is callback with no statement, the APIs are only empty shells here and are populated later.
({
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
    onConfigLoaded: () => {
        // This is called during emscripten `preInit` event, after we fetched config.

        // Module.config is loaded and could be tweaked before application
        Module.config.environment_variables["MONO_LOG_LEVEL"]="debug"

        // here we could use API passed into this callback
        // call some early available functions
        MONO.mono_wasm_setenv("HELLO", "WORLD);
    }
    onDotnetReady: () => {
        // Only when there is no `onRuntimeInitialized` override.
        // This is called after all assets are loaded , mapping to legacy `config.loaded_cb`.
        // It happens during emscripten `onRuntimeInitialized` after monoVm init + globalization + assemblies.
        // This also matches when the top level promise is resolved.
        // The original emscripten `Module.ready` promise is replaced with this.

        // at this point both emscripten and monoVM are fully initialized.
        Module.FS.chdir(processedArguments.working_dir);
    },
    onAbort: (error) => {
        set_exit_code(1, error);
    },
}));

// at this point both emscripten and monoVM are fully initialized.
// we could use the APIs returned and resolved from createDotnetRuntime promise
// both API exports are receiving the same API object instances, i.e. same `MONO` instance.
const run_all = BINDING.bind_static_method ("[debugger-test] DebuggerTest:run_all");
run_all();
```
