// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

let runBenchmark;
let setTasks;
let getFullJsonResults;

class MainApp {
    init({ BINDING }) {
        runBenchmark = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample] Sample.Test:RunBenchmark");
        setTasks = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample] Sample.Test:SetTasks");
        getFullJsonResults = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample] Sample.Test:GetFullJsonResults");

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
                fetch("/results.json", {
                    method: 'POST',
                    body: getFullJsonResults()
                }).then (r => { console.log("post request complete, response: ", r); });
                fetch("/results.html", {
                    method: 'POST',
                    body: document.getElementById("out").innerHTML
                }).then (r => { console.log("post request complete, response: ", r); });
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

globalThis.mainApp = new MainApp();

createDotnetRuntime(({ BINDING }) => ({
    disableDotnet6Compatibility: true,
    configSrc: "./mono-config.json",
    onDotnetReady: () => {
        try {
            mainApp.init({ BINDING });
        } catch (error) {
            set_exit_code(1, error);
            throw (error);
        }
    },
    onAbort: (error) => {
        set_exit_code(1, error);
    },
}));

function set_exit_code(exit_code, reason) {
    /* Set result in a tests_done element, to be read by xharness */
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    tests_done_elem.innerHTML = exit_code.toString();
    document.body.appendChild(tests_done_elem);

    console.log(`WASM EXIT ${exit_code}`);
};
