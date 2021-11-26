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
else {
    throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.")
}
let require = (name) => { return Module.imports.require(name) };
var __dirname = '';