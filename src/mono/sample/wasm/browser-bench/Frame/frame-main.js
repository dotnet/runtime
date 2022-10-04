// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import { dotnet, exit } from './dotnet.js'

class FrameApp {
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
        .withModuleConfig({
            printErr: () => undefined,
            print: () => undefined,
            onConfigLoaded: (config) => {
                // we are sharing the same mono-config.json with Main application
                // we don't want to load all of it, including it's [JSExport]s
                config.assets = config.assets.filter(a => a.name !== "Wasm.Browser.Bench.Main.dll" && "Wasm.Browser.Bench.Common.dll")

                if (window.parent != window) {
                    window.parent.resolveAppStartEvent("onConfigLoaded");
                }
                // config.diagnosticTracing = true;
            }
        })
        .create();

    if (window.parent != window) {
        window.parent.resolveAppStartEvent("onDotnetReady");
    }

    await runtime.runMain("Wasm.Browser.Bench.Frame.dll", []);
}
catch (err) {
    if (!mute) {
        console.error(`WASM ERROR ${err}`);
    }
    exit(1, err);
}
