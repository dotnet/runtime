// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import createDotnetRuntime from './dotnet.js'

let runBenchmark;
let setTasks;

class MainApp {
    init({ BINDING }) {
        runBenchmark = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample] Sample.Test:RunBenchmark");
        setTasks = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample] Sample.Test:SetTasks");

        var url = new URL(decodeURI(window.location));
        let tasks = url.searchParams.getAll('task');
        if (tasks != '') {
            setTasks(tasks.join(','));
        }

        this.yieldBench();
    }


    yieldBench() {
        let promise = runBenchmark();
        promise.then(ret => {
            document.getElementById("out").innerHTML += ret;
            if (ret.length > 0) {
                setTimeout(() => { this.yieldBench(); }, 0);
            } else {
                document.getElementById("out").innerHTML += "Finished";
            }
        });
    }

    async PageShow() {
        try {
            await this.waitFor('pageshow');
        } finally {
            this.removeFrame();
        }
    }

    async ReachedManaged() {
        try {
            await this.waitFor('reached');
        } finally {
            this.removeFrame();
        }
    }

    async waitFor(eventName) {
        try {
            let promise;
            let promiseResolve;
            this._frame = document.createElement('iframe');
            this._frame.src = 'appstart-frame.html';

            promise = new Promise(resolve => { promiseResolve = resolve; })
            window.resolveAppStartEvent = function (event) {
                if (!eventName || event == eventName)
                    promiseResolve();
            }

            document.body.appendChild(this._frame);
            await promise;
        } catch (err) {
            console.log(err);
            throw err;
        }
    }

    removeFrame() {
        this._frame.contentWindow.muteErrors();
        document.body.removeChild(this._frame);
    }
}

try {
    globalThis.mainApp = new MainApp();

    const { BINDING } = await createDotnetRuntime();
    mainApp.init({ BINDING });
}
catch (err) {
    console.error(`WASM ERROR ${err}`);
}