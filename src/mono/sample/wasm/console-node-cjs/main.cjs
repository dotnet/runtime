const createDotnetRuntime = require("./dotnet.js");

async function main() {
    const { MONO } = await createDotnetRuntime();
    const app_args = process.argv.slice(2);
    const dllName = "Wasm.Console.Node.CJS.Sample.dll";
    await MONO.mono_run_main_and_exit(dllName, app_args);
};
main();
