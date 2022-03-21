load("./dotnet.js")

const dllName = "Wasm.Console.V8.CJS.Sample.dll";
const app_args = Array.from(arguments);

async function main() {
    const { MONO } = await createDotnetRuntime();
    await MONO.mono_run_main_and_exit(dllName, app_args);
}

main();