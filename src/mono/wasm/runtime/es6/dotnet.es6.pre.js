if (_nativeModuleLoaded) throw new Error("Native module already loaded");
_nativeModuleLoaded = true;
Module["getMemory"] = function () { return wasmMemory; }
Module["getWasmIndirectFunctionTable"] = function () { return wasmTable; }
createDotnetRuntime = Module = createDotnetRuntime(Module);
Module["getMemory"] = function () { return wasmMemory; }
Module["getWasmIndirectFunctionTable"] = function () { return wasmTable; }
