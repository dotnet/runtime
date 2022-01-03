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

var Module = { 

    config: null,

    preInit: async function() {
        await MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.config implicitly
    },

    // Called when the runtime is initialized and wasm is ready
    onRuntimeInitialized: function () {
        if (!Module.config || Module.config.error) {
            console.log("No config found");
            return;
        }

        Module.config.loaded_cb = function () {
            try {
                BINDING.call_static_method("[System.Private.Runtime.InteropServices.JavaScript.Tests] System.Runtime.InteropServices.JavaScript.Tests.SimpleTest:Test", []);
            } catch (error) {
                throw (error);
            }
        };
        Module.config.fetch_file_cb = function (asset) {
            return fetch (asset, { credentials: 'same-origin' });
        }

        try
        {
            MONO.mono_load_runtime_and_bcl_args (Module.config);
        } catch (error) {
            throw(error);
        }
    },
};
