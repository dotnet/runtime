import createDotnetRuntime from '@microsoft/dotnet-runtime'
import process from 'process'

const { runMainAndExit } = await createDotnetRuntime(() => ({
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
}));

const app_args = process.argv.slice(2);
const dllName = "Wasm.Console.Node.TS.Sample.dll";
await runMainAndExit(dllName, app_args);
