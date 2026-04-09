// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

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

    // receive blazor benchmark event and forward it to resolveAppStartEvent
    window.receiveBenchmarkEvent = function (name) {
        if (window !== window.parent) {
            window.parent.resolveAppStartEvent("blazor: " + name);
        }
    };

    window.muteErrors = () => {
        mute = true;
    }
}
catch (err) {
    if (!mute) {
        console.error(`WASM ERROR ${err}`);
    }
}
