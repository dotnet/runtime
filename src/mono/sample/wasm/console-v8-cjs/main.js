load("./dotnet.js")

const dllName = "Wasm.Console.V8.CJS.Sample.dll";
const app_args = Array.from(arguments);
async function main() {
    try {
        const { MONO } = await createDotnetRuntime(() => ({
            disableDotnet6Compatibility: true,
            configSrc: "./mono-config.json",
        }));

        await MONO.mono_run_main_and_exit(dllName, app_args);
    } catch (error) {
        console.log("WASM ERROR " + error);
    }
}

main();
