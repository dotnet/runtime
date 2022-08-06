// @ts-check
// @ts-ignore
import createDotnetRuntime from './dotnet.js'
import process from 'process'

/**
 * @type {import('../../../wasm/runtime/dotnet').CreateDotnetRuntimeType}
 */
const createDotnetRuntimeTyped = createDotnetRuntime;

const { runMainAndExit } = await createDotnetRuntimeTyped(() => ({
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
}));

const app_args = process.argv.slice(2);
const dllName = "Wasm.Console.Node.Sample.dll";
await runMainAndExit(dllName, app_args);
