// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import { dotnet, exit } from './_framework/dotnet.js'

class FrameApp {
    async init({ getAssemblyExports }) {
        const exports = await getAssemblyExports("Wasm.Browser.Bench.Sample.dll");
        exports.Sample.AppStartTask.FrameApp.ReachedManaged();
    }

    reachedCallback() {
        if (window.parent != window) {
            window.parent.resolveAppStartEvent("reached");
        }
    }
}

let mute = false;
try {
    globalThis.frameApp = new FrameApp();
    globalThis.frameApp.ReachedCallback = globalThis.frameApp.reachedCallback.bind(globalThis.frameApp);
    if (window.parent != window) {
        window.addEventListener("pageshow", event => { window.parent.resolveAppStartEvent("pageshow"); })
    }

    window.muteErrors = () => {
        mute = true;
    }

    const runtime = await dotnet
        .withConfig({
            maxParallelDownloads: 10000,
            // diagnosticTracing:true,
        })
        .withModuleConfig({
            printErr: () => undefined,
            print: () => undefined
        })
        .create();

    await frameApp.init(runtime);
}
catch (err) {
    if (!mute) {
        console.error(`WASM ERROR ${err}`);
    }
    exit(1, err);
}
