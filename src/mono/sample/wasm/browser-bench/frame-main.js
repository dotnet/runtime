// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import createDotnetRuntime from './dotnet.js'

class FrameApp {
    async init({ BINDING }) {
        const reachManagedReached = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample] Sample.AppStartTask/ReachManaged:Reached");
        await reachManagedReached();
    }

    reached() {
        window.parent.resolveAppStartEvent("reached");
    }
}

try {
    globalThis.frameApp = new FrameApp();

    let mute = false;
    window.addEventListener("pageshow", event => { window.parent.resolveAppStartEvent("pageshow"); })

    window.muteErrors = () => {
        mute = true;
    }

    const { BINDING } = await createDotnetRuntime(() => ({
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
        onAbort: (error) => {
            wasm_exit(1, error);
        },
    }));

    window.parent.resolveAppStartEvent("onDotnetReady");
    await frameApp.init({ BINDING });
}
catch (err) {
    if (!mute) {
        console.error(`WASM ERROR ${err}`);
    }
    wasm_exit(1, err);
}

function wasm_exit(exit_code, reason) {
    /* Set result in a tests_done element, to be read by xharness */
    var tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    if (exit_code) tests_done_elem.style.background = "red";
    document.body.appendChild(tests_done_elem);

    if (reason) console.error(reason);
    console.log(`WASM EXIT ${exit_code}`);
};