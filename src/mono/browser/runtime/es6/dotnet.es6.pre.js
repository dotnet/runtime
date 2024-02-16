if (_nativeModuleLoaded) throw new Error("Native module already loaded");
_nativeModuleLoaded = true;
createDotnetRuntime = Module = createDotnetRuntime(Module);
