const MONO = {}, BINDING = {}, INTERNAL = {};
let ENVIRONMENT_IS_GLOBAL = false;

function defaultLocateFile(path, scriptDirectory) {
    if (ENVIRONMENT_IS_NODE && path == "dotnet.wasm") {
        return new URL('dotnet.wasm', import.meta.url).toString();
    }
    
    if (!ENVIRONMENT_IS_NODE && scriptDirectory.startsWith("file:")) {
        return path;
    }

    return scriptDirectory + path;
}

if (typeof createDotnetRuntime === "function") {
    Module = { ready: Module.ready, locateFile: defaultLocateFile };
    const extension = createDotnetRuntime({ MONO, BINDING, INTERNAL, Module })
    if (extension.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    Object.assign(Module, extension);
    createDotnetRuntime = Module;
}
else if (typeof createDotnetRuntime === "object") {
    Module = { ready: Module.ready, locateFile: defaultLocateFile, __undefinedConfig: Object.keys(createDotnetRuntime).length === 1 };
    Object.assign(Module, createDotnetRuntime);
    createDotnetRuntime = Module;
}
else {
    throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.")
}
var require = require || undefined;
var __dirname = __dirname || '';