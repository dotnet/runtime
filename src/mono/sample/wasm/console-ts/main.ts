import { createRequire } from 'module';
import createDotnetRuntime from 'dotnet'
import { dirname } from 'path';
import { fileURLToPath } from 'url';

const { MONO, BINDING, INTERNAL, Module, RuntimeBuildInfo } = await createDotnetRuntime(() => ({
    imports: {
        require: createRequire(import.meta.url)
    },
    scriptDirectory: dirname(fileURLToPath(import.meta.url)) + '/',
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
    onAbort: (err: any) => {
        console.log("WASM ABORT " + err);
    },
}));

try {

    const main_assembly_name = "Wasm.Console.TypeScript.Sample.dll";
    const app_args = process.argv.slice(4);
    INTERNAL.mono_wasm_set_main_args(main_assembly_name, app_args);

    // Automatic signature isn't working correctly
    const result = await BINDING.call_assembly_entry_point(main_assembly_name, [app_args], "m");

    console.log("WASM EXIT " + result);
} catch (error) {
    console.log("WASM ERROR " + error);
}
