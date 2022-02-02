import createDotnetRuntime from "./dotnet.js";

const { MONO } = await createDotnetRuntime();
const app_args = process.argv.slice(2);
await MONO.mono_run_main_and_exit("console.dll", app_args);
