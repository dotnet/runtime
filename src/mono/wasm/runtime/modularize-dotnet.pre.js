const MONO = {}, BINDING = {}, INTERNAL = {};
var ENVIRONMENT_IS_GLOBAL = typeof globalThis.Module === "object";
if (ENVIRONMENT_IS_GLOBAL) {
    createDotnetRuntime = globalThis.Module;
}
else if (typeof createDotnetRuntime === "function") {
    createDotnetRuntime = createDotnetRuntime({ MONO, BINDING, INTERNAL, Module });
}
createDotnetRuntime.ready = Module.ready;
Module = createDotnetRuntime;
