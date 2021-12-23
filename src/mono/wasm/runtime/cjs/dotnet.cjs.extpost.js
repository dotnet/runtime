var require = require || undefined;
// if loaded into global namespace and configured with global Module, we will self start in compatibility mode
let ENVIRONMENT_IS_GLOBAL = typeof globalThis.Module === "object" && globalThis.__dotnet_runtime === __dotnet_runtime;
if (ENVIRONMENT_IS_GLOBAL) {
    createDotnetRuntime(() => { return globalThis.Module; }).then((exports) => exports);
}
else if (typeof globalThis.__onDotnetRuntimeLoaded === "function") {
    globalThis.__onDotnetRuntimeLoaded(createDotnetRuntime);
}