const MONO = {}, BINDING = {}, INTERNAL = {}, IMPORTS = {};
let ENVIRONMENT_IS_GLOBAL = false;
var require = require || undefined;
var __dirname = __dirname || '';
if (typeof createDotnetRuntime === "function") {
    Module = { ready: Module.ready };
    const extension = createDotnetRuntime({ MONO, BINDING, INTERNAL, Module, IMPORTS })
    if (extension.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    Object.assign(Module, extension);
    createDotnetRuntime = Module;
    if (!createDotnetRuntime.locateFile) createDotnetRuntime.locateFile = createDotnetRuntime.__locateFile = (path) => scriptDirectory + path;
}
else if (typeof createDotnetRuntime === "object") {
    Module = { ready: Module.ready, __undefinedConfig: Object.keys(createDotnetRuntime).length === 1 };
    Object.assign(Module, createDotnetRuntime);
    createDotnetRuntime = Module;
    if (!createDotnetRuntime.locateFile) createDotnetRuntime.locateFile = createDotnetRuntime.__locateFile = (path) => scriptDirectory + path;
}
else {
    throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.")
}