createDotnetRuntime.ready = createDotnetRuntime.ready.then(() => {
    return __dotnet_exportedAPI;
});

return createDotnetRuntime.ready
});}) ();
__dotnet_runtime.__setEmscriptenEntrypoint(createDotnetRuntime);
const dotnet = __dotnet_runtime.moduleExports.dotnet;
const exit = __dotnet_runtime.moduleExports.exit;
const INTERNAL = __dotnet_runtime.moduleExports.requirePromise;
export { dotnet, exit, INTERNAL };
var ignoredIffe = (() => { return (function () {
