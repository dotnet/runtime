// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import { dotnet, exit } from './dotnet.js'

let runBenchmark;
let setTasks;
let getFullJsonResults;
let legacyExportTargetInt;
let jsExportTargetInt;
let legacyExportTargetString;
let jsExportTargetString;
let _jiterpreter_dump_stats;

function runLegacyExportInt(count) {
    for (let i = 0; i < count; i++) {
        legacyExportTargetInt(i);
    }
}

function runJSExportInt(count) {
    for (let i = 0; i < count; i++) {
        jsExportTargetInt(i);
    }
}

function runLegacyExportString(count) {
    for (let i = 0; i < count; i++) {
        legacyExportTargetString("A" + i);
    }
}

function runJSExportString(count) {
    for (let i = 0; i < count; i++) {
        jsExportTargetString("A" + i);
    }
}

function importTargetInt(value) {
    return value + 1;
}

function importTargetString(value) {
    return value + "A";
}

function importTargetManyArgs(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) {
    return 1 + arg1 + arg2 + arg3.length + arg4.length + arg7 + arg9 + arg10.valueOf();
}

async function importTargetTask(value) {
    await value;
    return;
}

function importTargetThrows(value) {
    throw new Error("test" + value);
}

class MainApp {
    async init({ getAssemblyExports, setModuleImports, BINDING, INTERNAL }) {
        const exports = await getAssemblyExports("Wasm.Browser.Bench.Sample.dll");
        INTERNAL.jiterpreter_apply_options({
            enableStats: true
        });
        _jiterpreter_dump_stats = INTERNAL.jiterpreter_dump_stats.bind(INTERNAL);
        runBenchmark = exports.Sample.Test.RunBenchmark;
        setTasks = exports.Sample.Test.SetTasks;
        getFullJsonResults = exports.Sample.Test.GetFullJsonResults;

        legacyExportTargetInt = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample]Sample.ImportsExportsHelper:LegacyExportTargetInt");
        jsExportTargetInt = exports.Sample.ImportsExportsHelper.JSExportTargetInt;
        legacyExportTargetString = BINDING.bind_static_method("[Wasm.Browser.Bench.Sample]Sample.ImportsExportsHelper:LegacyExportTargetString");
        jsExportTargetString = exports.Sample.ImportsExportsHelper.JSExportTargetString;

        setModuleImports("main.js", {
            Sample: {
                Test: {
                    runLegacyExportInt,
                    runJSExportInt,
                    runLegacyExportString,
                    runJSExportString,
                    importTargetInt,
                    importTargetString,
                    importTargetManyArgs,
                    importTargetTask,
                    importTargetThrows,
                }
            }
        });


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
                _jiterpreter_dump_stats();
                document.getElementById("out").innerHTML += "Finished";
                fetch("/results.json", {
                    method: 'POST',
                    body: getFullJsonResults()
                }).then(r => { console.log("post request complete, response: ", r); });
                fetch("/results.html", {
                    method: 'POST',
                    body: document.getElementById("out").innerHTML
                }).then(r => { console.log("post request complete, response: ", r); });
            }
        });
    }

    async pageShow() {
        try {
            await this.waitFor('pageshow');
        } finally {
            this.removeFrame();
        }
    }

    async frameReachedManaged() {
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
    globalThis.mainApp.FrameReachedManaged = globalThis.mainApp.frameReachedManaged.bind(globalThis.mainApp);
    globalThis.mainApp.PageShow = globalThis.mainApp.pageShow.bind(globalThis.mainApp);

    const runtime = await dotnet
        .withRuntimeOptions(["--jiterpreter-stats-enabled"])
        .withElementOnExit()
        .withExitCodeLogging()
        .create();

    await mainApp.init(runtime);
}
catch (err) {
    exit(1, err);
}
