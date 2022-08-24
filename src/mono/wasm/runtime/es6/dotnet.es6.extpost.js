
const MONO = {}, BINDING = {}, INTERNAL = {}, IMPORTS = {};

// TODO duplicated from emscripten, so we can use them in the __setEmscriptenEntrypoint
var ENVIRONMENT_IS_WEB = typeof window == 'object';
var ENVIRONMENT_IS_WORKER = typeof importScripts == 'function';
var ENVIRONMENT_IS_NODE = typeof process == 'object' && typeof process.versions == 'object' && typeof process.versions.node == 'string';
var ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;

__dotnet_runtime.__setEmscriptenEntrypoint(createDotnetRuntime, { isNode: ENVIRONMENT_IS_NODE, isShell: ENVIRONMENT_IS_SHELL, isWeb: ENVIRONMENT_IS_WEB, isWorker: ENVIRONMENT_IS_WORKER });
const dotnet = __dotnet_runtime.moduleExports.dotnet;
const exit = __dotnet_runtime.moduleExports.exit;
export { dotnet, exit, INTERNAL };
