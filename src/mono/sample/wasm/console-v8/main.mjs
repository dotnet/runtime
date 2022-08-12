import createDotnetRuntime from './dotnet.js'

const dllName = "Wasm.Console.V8.Sample.dll";
const app_args = Array.from(arguments);

async function main() {
    const { runMainAndExit } = await createDotnetRuntime();
    await runMainAndExit(dllName, app_args);
}

main();