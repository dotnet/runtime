const MONO = {}, BINDING = {}, INTERNAL = {};
var ENVIRONMENT_IS_GLOBAL = typeof globalThis.Module === "object";
if (ENVIRONMENT_IS_GLOBAL) {
    globalThis.Module.ready = Module.ready;
    Module = createDotnetRuntime = globalThis.Module;
}
else if (typeof createDotnetRuntime === "function") {
    Module = { ready: Module.ready };
    Object.assign(Module, createDotnetRuntime({ MONO, BINDING, INTERNAL, Module }))
    createDotnetRuntime = Module;
}
