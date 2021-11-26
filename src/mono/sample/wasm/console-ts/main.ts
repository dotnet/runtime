import { createRequire } from 'module';
import createDotnetRuntime from 'dotnet'
import { dirname } from 'path';
import { fileURLToPath } from 'url';
import { exit } from 'process';

const { BINDING } = await createDotnetRuntime(() => ({
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
    const app_args = process.argv.slice(2);
    const result = await BINDING.call_assembly_entry_point("Wasm.Console.TypeScript.Sample.dll", [app_args], "m");
    exit(result)
} catch (error) {
    console.log("WASM ERROR " + error);
}
