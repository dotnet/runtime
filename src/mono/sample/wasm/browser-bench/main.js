// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

import { dotnet, exit } from './_framework/dotnet.js'

let runBenchmark;
let setTasks;
let setExclusions;
let getFullJsonResults;
let jsExportTargetInt;
let jsExportTargetString;
let _jiterpreter_dump_stats, _interp_pgo_save_data;

function runJSExportInt(count) {
    for (let i = 0; i < count; i++) {
        jsExportTargetInt(i);
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
    async init({ getAssemblyExports, setModuleImports, INTERNAL }) {
        const exports = await getAssemblyExports("Wasm.Browser.Bench.Sample.dll");
        // Capture these two internal APIs for use at the end of the benchmark run
        _jiterpreter_dump_stats = INTERNAL.jiterpreter_dump_stats.bind(INTERNAL);
        _interp_pgo_save_data = INTERNAL.interp_pgo_save_data.bind(INTERNAL);
        runBenchmark = exports.Sample.Test.RunBenchmark;
        setTasks = exports.Sample.Test.SetTasks;
        setExclusions = exports.Sample.Test.SetExclusions;
        getFullJsonResults = exports.Sample.Test.GetFullJsonResults;

        jsExportTargetInt = exports.Sample.ImportsExportsHelper.JSExportTargetInt;
        jsExportTargetString = exports.Sample.ImportsExportsHelper.JSExportTargetString;

        setModuleImports("main.js", {
            Sample: {
                Test: {
                    runJSExportInt,
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
        let exclusions = url.searchParams.getAll('exclusions');
        if (exclusions != '') {
            setExclusions(exclusions.join(','));
        }

        const r = await fetch("/bootstrap.flag", {
            method: 'POST',
            body: "ok"
        });
        console.log("bootstrap post request complete, response: ", r);

        while (true) {
            const resultString = await this.yieldBench();
            if (resultString.length == 0) break;
            document.getElementById("out").innerHTML += resultString;
            console.log(resultString);
        }

        document.getElementById("out").innerHTML += "Finished";
        const r1 = await fetch("/results.json", {
            method: 'POST',
            body: getFullJsonResults()
        });
        console.log("post request complete, response: ", r1);
        const r2 = await fetch("/results.html", {
            method: 'POST',
            body: document.getElementById("out").innerHTML
        });
        console.log("post request complete, response: ", r2);

        // If the jiterpreter's statistics are enabled, make sure we dump them at the
        //  end of the benchmark run to have a definitive version of them that captures
        //  everything, since the automatic ones are triggered at a basically random
        //  interval
        if (_jiterpreter_dump_stats)
            _jiterpreter_dump_stats();

        // We explicitly save the interpreter PGO table after a run is complete
        //  in order to capture all the methods that were tiered while running benchmarks
        // The normal automatic save-on-startup-timeout behavior is insufficient for
        //  scenarios like this since we can't predict how long the benchmarks will
        //  take, and "startup" is spread over the whole run instead of the beginning
        if (_interp_pgo_save_data)
            await _interp_pgo_save_data(true);
    }

    yieldBench() {
        return new Promise(resolve => setTimeout(() => resolve(runBenchmark()), 0));
    }

    origin() {
        return window.location.origin;
    }

    async pageShow(guid, base) {
        try {
            await this.waitFor('pageshow', guid, base);
        } finally {
            this.removeFrame();
        }
    }

    async frameReachedManaged(guid, base) {
        try {
            await this.waitFor('reached', guid, base);
        } finally {
            this.removeFrame();
        }
    }

    async frameBlazorFirstUI(guid, base) {
        try {
            await this.waitFor('blazor: Rendered Index.razor', guid, base);
        } finally {
            this.removeFrame();
        }
    }

    framePage = 'appstart-frame.html';

    async setFramePage(page) {
        this.framePage = page;
    }

    async waitFor(eventName, guid, base) {
        try {
            let promise;
            let promiseResolve;
            this._frame = document.createElement('iframe');
            let page = (base ? base : '') + this.framePage;
            this._frame.src = guid
                ? 'unique/' + guid + '/' + page
                : page;

            let resolved = false;
            promise = new Promise(resolve => { promiseResolve = resolve; })
            window.resolveAppStartEvent = function (event) {
                if (!eventName || event == eventName) {
                    resolved = true;
                    promiseResolve();
                }
            }

            // The appstart event may never fire if something goes wrong, in that case
            //  we want to time out within a reasonable period of time so that the run
            //  of browser-bench will eventually complete instead of just freezing.
            setTimeout(function () {
                if (resolved)
                    return;
                console.error(`waitFor ${eventName} timed out`);
                promiseResolve();
                // Make sure this timeout is big enough that it won't cause measurements
                //  to be truncated! i.e. "Blazor Reach managed cold" is nearly 10k in some
                //  configurations right now
            }, 20000);

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
    globalThis.mainApp.FrameBlazorFirstUI = globalThis.mainApp.frameBlazorFirstUI.bind(globalThis.mainApp);
    globalThis.mainApp.FrameReachedManaged = globalThis.mainApp.frameReachedManaged.bind(globalThis.mainApp);
    globalThis.mainApp.PageShow = globalThis.mainApp.pageShow.bind(globalThis.mainApp);
    globalThis.mainApp.Origin = globalThis.mainApp.origin.bind(globalThis.mainApp);
    globalThis.mainApp.SetFramePage = globalThis.mainApp.setFramePage.bind(globalThis.mainApp);

    const runtime = await dotnet
        // We enable jiterpreter stats so that in local runs you can open the devtools
        //  console to see statistics on how much code it generated and whether any new opcodes
        //  are causing traces to fail to compile
        .withRuntimeOptions(["--jiterpreter-stats-enabled"])
        // We enable interpreter PGO so that you can exercise it in local tests, i.e.
        //  run browser-bench one, then refresh the tab to measure the performance improvement
        //  on the second run of browser-bench. The overall speed of the benchmarks won't
        //  improve much, but the time spent generating code during the run will go down
        .withInterpreterPgo(true, 30)
        .withElementOnExit()
        .withExitCodeLogging()
        .create();

    await mainApp.init(runtime);
}
catch (err) {
    exit(1, err);
}
