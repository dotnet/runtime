const { exit } = require("process");
const createDotnetRuntime = require("./dotnet.js");

async function main() {
    try {
        const { BINDING, RuntimeBuildInfo } = await createDotnetRuntime(() => ({
            imports: {
                require
            },
            disableDotnet6Compatibility: true,
            configSrc: "./mono-config.json",
            onAbort: (err) => {
                console.log(`WASM ERROR ${err}`);
                exit(-1)
            },
        }));

        const exit_code = await BINDING.call_assembly_entry_point("System.Private.Runtime.InteropServices.JavaScript.Tests.dll", [""], "m");
        console.log("WASM EXIT " + exit_code);
        exit(exit_code);
    } catch (error) {
        console.log("WASM ERROR " + error);
        console.log(error.stack);
        throw error;
    }
};

main();
