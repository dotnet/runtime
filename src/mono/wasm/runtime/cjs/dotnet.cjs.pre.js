const MONO = {}, BINDING = {}, INTERNAL = {};
if (ENVIRONMENT_IS_GLOBAL) {
    if (globalThis.Module.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    globalThis.Module.ready = Module.ready;
    Module = createDotnetRuntime = globalThis.Module;
}
else if (typeof createDotnetRuntime === "object") {
    Module = { ready: Module.ready, __undefinedConfig: Object.keys(createDotnetRuntime).length === 1 };
    Object.assign(Module, createDotnetRuntime);
    createDotnetRuntime = Module;
}
else if (typeof createDotnetRuntime === "function") {
    Module = { ready: Module.ready };
    const extension = createDotnetRuntime({ MONO, BINDING, INTERNAL, Module })
    if (extension.ready) {
        throw new Error("MONO_WASM: Module.ready couldn't be redefined.")
    }
    Object.assign(Module, extension);
    createDotnetRuntime = Module;
}
else {
    throw new Error("MONO_WASM: Can't locate global Module object or moduleFactory callback of createDotnetRuntime function.")
}
