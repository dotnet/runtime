import createDotnetRuntime from '@microsoft/dotnet-runtime'

const { MONO } = await createDotnetRuntime(() => ({
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
}));

const app_args = process.argv.slice(2);
const dllName = "Wasm.Console.Node.TS.Sample.dll";
await MONO.mono_run_main_and_exit(dllName, app_args);
