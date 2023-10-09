if (_nativeModuleLoaded) throw new Error("Native module already loaded");
_nativeModuleLoaded = true;
// see https://github.com/emscripten-core/emscripten/issues/19832
Module["getMemory"] = function () { return wasmMemory; }
createDotnetRuntime = Module = createDotnetRuntime(Module);
Module["getMemory"] = function () { return wasmMemory; }
