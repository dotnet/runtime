const MONO = {}, BINDING = {}, INTERNAL = {};
let ENVIRONMENT_IS_GLOBAL = false;
if (typeof createDotnetRuntime === "function") {
    Module = { ready: Module.ready };
    const extension = createDotnetRuntime({ MONO, BINDING, INTERNAL, Module })
    if (extension.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    Object.assign(Module, extension);
    createDotnetRuntime = Module;
}
else if (typeof createDotnetRuntime === "object") {
    Module = { ready: Module.ready, __undefinedConfig: Object.keys(createDotnetRuntime).length === 1 };
    Object.assign(Module, createDotnetRuntime);
    createDotnetRuntime = Module;
}
else {
    throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.")
}
var require = require || undefined;
var __dirname = __dirname || '';