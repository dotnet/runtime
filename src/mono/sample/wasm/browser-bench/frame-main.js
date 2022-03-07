// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";
class FrameApp {
    async init({ BINDING }) {
        const reachManagedReached = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample] Sample.AppStartTask/ReachManaged:Reached");
        await reachManagedReached();
    }

    reached() {
        window.parent.resolveAppStartEvent("reached");
    }
}

globalThis.frameApp = new FrameApp();

let mute = false;
createDotnetRuntime(({ BINDING }) => {
    return {
        disableDotnet6Compatibility: true,
        configSrc: "./mono-config.json",
        printErr: function () {
            if (!mute) {
                console.error(...arguments);
            }
        },
        onConfigLoaded: () => {
            window.parent.resolveAppStartEvent("onConfigLoaded");
            // Module.config.diagnostic_tracing = true;
        },
        onDotnetReady: async () => {
            window.parent.resolveAppStartEvent("onDotnetReady");
            try {
                await frameApp.init({ BINDING });
            } catch (error) {
                set_exit_code(1, error);
                throw (error);
            }
        },
        onAbort: (error) => {
            set_exit_code(1, error);
        },
    }
}).catch(err => {
    if (!mute) {
        console.error(`WASM ERROR ${err}`);
    }
})

window.addEventListener("pageshow", event => { window.parent.resolveAppStartEvent("pageshow"); })

window.muteErrors = () => {
    mute = true;
}

function set_exit_code(exit_code, reason) {
    /* Set result in a tests_done element, to be read by xharness */
    var tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
};