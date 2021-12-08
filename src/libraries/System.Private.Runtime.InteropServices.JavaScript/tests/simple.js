const { exit } = require("process");
const createDotnetRuntime = require("./dotnet.js");

class TimersHelper {
    install() {
        const measuredCallbackName = "mono_wasm_set_timeout_exec";
        globalThis.registerCounter = 0;
        globalThis.hitCounter = 0;
        console.log("install")
        if (!globalThis.originalSetTimeout) {
            globalThis.originalSetTimeout = globalThis.setTimeout;
        }
        globalThis.setTimeout = (cb, time) => {
            var start = Date.now().valueOf();
            if (cb.name === measuredCallbackName) {
                globalThis.registerCounter++;
                console.log(`registerCounter: ${globalThis.registerCounter} now:${start} delay:${time}`)
            }
            return globalThis.originalSetTimeout(() => {
                if (cb.name === measuredCallbackName) {
                    var hit = Date.now().valueOf();
                    globalThis.hitCounter++;
                    var delta = hit - start;
                    console.log(`hitCounter: ${globalThis.hitCounter} now:${hit} delay:${time} delta:${delta}`)
                }
                cb();
            }, time);
        };
    }

    getRegisterCount() {
        console.log(`registerCounter: ${globalThis.registerCounter} `)
        return globalThis.registerCounter;
    }

    getHitCount() {
        console.log(`hitCounter: ${globalThis.hitCounter} `)
        return globalThis.hitCounter;
    }

    cleanup() {
        console.log(`cleanup registerCounter: ${globalThis.registerCounter} hitCounter: ${globalThis.hitCounter} `)
        globalThis.setTimeout = globalThis.originalSetTimeout;
    }
}

globalThis.timersHelper = new TimersHelper();

async function main() {
    try {
        const { BINDING } = await createDotnetRuntime(() => ({
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
