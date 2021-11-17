// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
// Run runtime tests under a JS shell or a browser
//
"use strict";

//glue code to deal with the differences between chrome, ch, d8, jsc and sm.
const is_browser = typeof window != "undefined";
const is_node = !is_browser && typeof process === 'object' && typeof process.versions === 'object' && typeof process.versions.node === 'string';

// if the engine doesn't provide a console
if (typeof (console) === "undefined") {
    globalThis.console = {
        log: globalThis.print,
        clear: function () { }
    };
}
const originalConsole = {
    log: console.log,
    error: console.error
};

let isXUnitDoneCheck = false;

function proxyMethod(prefix, func, asJson) {
    return function () {
        const args = [...arguments];
        let payload = args[0];
        if (payload === undefined) payload = 'undefined';
        else if (payload === null) payload = 'null';
        else if (typeof payload === 'function') payload = payload.toString();
        else if (typeof payload !== 'string') {
            try {
                payload = JSON.stringify(payload);
            } catch (e) {
                payload = payload.toString();
            }
        }
        if (payload.indexOf("=== TEST EXECUTION SUMMARY ===") != -1) {
            isXUnitDoneCheck = true;
        }

        if (asJson) {
            func(JSON.stringify({
                method: prefix,
                payload: payload,
                arguments: args
            }));
        } else {
            func([prefix + payload, ...args.slice(1)]);
        }
    };
};

const methods = ["debug", "trace", "warn", "info", "error"];
for (let m of methods) {
    if (typeof (console[m]) !== "function") {
        console[m] = proxyMethod(`console.${m}: `, console.log, false);
    }
}

function proxyJson(func) {
    for (let m of ["log", ...methods])
        console[m] = proxyMethod(`console.${m}`, func, true);
}

if (is_browser) {
    const consoleUrl = `${window.location.origin}/console`.replace('http://', 'ws://');

    let consoleWebSocket = new WebSocket(consoleUrl);
    // redirect output so that when emscripten starts it's already redirected
    proxyJson(function (msg) {
        if (consoleWebSocket.readyState === WebSocket.OPEN) {
            consoleWebSocket.send(msg);
        }
        else {
            originalConsole.log(msg);
        }
    });

    consoleWebSocket.onopen = function (event) {
        originalConsole.log("browser: Console websocket connected.");
    };
    consoleWebSocket.onerror = function (event) {
        originalConsole.error(`websocket error: ${event}`);
    };
}

if (typeof globalThis.crypto === 'undefined') {
    // **NOTE** this is a simple insecure polyfill for testing purposes only
    // /dev/random doesn't work on js shells, so define our own
    // See library_fs.js:createDefaultDevices ()
    globalThis.crypto = {
        getRandomValues: function (buffer) {
            for (let i = 0; i < buffer.length; i++)
                buffer[i] = (Math.random() * 256) | 0;
        }
    }
}

if (typeof globalThis.performance === 'undefined') {
    if (is_node) {
        const { performance } = require("perf_hooks");
        globalThis.performance = performance;
    } else {
        // performance.now() is used by emscripten and doesn't work in JSC
        globalThis.performance = {
            now: function () {
                return Date.now();
            }
        }
    }
}
var Module = {
    config: null,
    configSrc: "./mono-config.json",
    onConfigLoaded: () => {
        if (!Module.config) {
            const err = new Error("Could not find ./mono-config.json. Cancelling run");
            set_exit_code(1,);
            throw err;
        }
        // Have to set env vars here to enable setting MONO_LOG_LEVEL etc.
        for (let variable in processedArguments.setenv) {
            Module.config.environment_variables[variable] = processedArguments.setenv[variable];
        }

        if (!processedArguments.enable_gc) {
            INTERNAL.mono_wasm_enable_on_demand_gc(0);
        }
    },
    onDotNetReady: () => {
        let wds = Module.FS.stat(processedArguments.working_dir);
        if (wds === undefined || !Module.FS.isDir(wds.mode)) {
            set_exit_code(1, `Could not find working directory ${processedArguments.working_dir}`);
            return;
        }

        Module.FS.chdir(processedArguments.working_dir);

        App.init();
    },
    onAbort: (error) => {
        console.log("ABORT: " + error);
        const err = new Error();
        console.log("Stacktrace: \n");
        console.error(err.stack);
        set_exit_code(1, error);
    },
};


const App = {
    init: async function () {
        console.info("Initializing.....");
        for (let i = 0; i < processedArguments.profilers.length; ++i) {
            const init = Module.cwrap('mono_wasm_load_profiler_' + processedArguments.profilers[i], 'void', ['string']);
            init("");
        }

        if (processedArguments.applicationArgs.length == 0) {
            set_exit_code(1, "Missing required --run argument");
            return;
        }

        if (processedArguments.applicationArgs[0] == "--regression") {
            const exec_regression = Module.cwrap('mono_wasm_exec_regression', 'number', ['number', 'string']);

            let res = 0;
            try {
                res = exec_regression(10, processedArguments.applicationArgs[1]);
                console.log("REGRESSION RESULT: " + res);
            } catch (e) {
                console.error("ABORT: " + e);
                console.error(e.stack);
                res = 1;
            }

            if (res)
                set_exit_code(1, "REGRESSION TEST FAILED");

            return;
        }

        if (processedArguments.runtime_args.length > 0)
            INTERNAL.mono_wasm_set_runtime_options(processedArguments.runtime_args);

        if (processedArguments.applicationArgs[0] == "--run") {
            // Run an exe
            if (processedArguments.applicationArgs.length == 1) {
                set_exit_code(1, "Error: Missing main executable argument.");
                return;
            }
            try {

                const main_assembly_name = processedArguments.applicationArgs[1];
                const app_args = processedArguments.applicationArgs.slice(2);
                INTERNAL.mono_wasm_set_main_args(processedArguments.applicationArgs[1], app_args);

                // Automatic signature isn't working correctly
                const result = await BINDING.call_assembly_entry_point(main_assembly_name, [app_args], "m");
                set_exit_code(result);
            } catch (error) {
                set_exit_code(1, error);
            }
        } else {
            set_exit_code(1, "Unhandled argument: " + processedArguments.applicationArgs[0]);
        }
    },

    /** Runs a particular test
     * @type {(method_name: string, args: any[]=, signature: any=) => return number}
     */
    call_test_method: function (method_name, args, signature) {
        // note: arguments here is the array of arguments passsed to this function
        if ((arguments.length > 2) && (typeof (signature) !== "string"))
            throw new Error("Invalid number of arguments for call_test_method");

        const fqn = "[System.Private.Runtime.InteropServices.JavaScript.Tests]System.Runtime.InteropServices.JavaScript.Tests.HelperMarshal:" + method_name;
        try {
            return INTERNAL.call_static_method(fqn, args || [], signature);
        } catch (exc) {
            console.error("exception thrown in", fqn);
            throw exc;
        }
    }
};
globalThis.App = App; // Necessary as System.Runtime.InteropServices.JavaScript.Tests.MarshalTests (among others) call the App.call_test_method directly

function set_exit_code(exit_code, reason) {
    if (reason) {
        console.error(reason.toString());
        if (reason.stack) {
            console.error(reason.stack);
        }
    }
    if (is_browser) {
        const stack = (new Error()).stack.replace(/\n/g, "").replace(/[ ]*at/g, " at").replace(/https?:\/\/[0-9.:]*/g, "").replace("Error", "");
        const messsage = `Exit called with ${exit_code} when isXUnitDoneCheck=${isXUnitDoneCheck} ${stack}.`;

        // Notify the selenium script
        Module.exit_code = exit_code;

        //Tell xharness WasmBrowserTestRunner what was the exit code
        const tests_done_elem = document.createElement("label");
        tests_done_elem.id = "tests_done";
        tests_done_elem.innerHTML = exit_code.toString();
        document.body.appendChild(tests_done_elem);

        // need to flush streams (stdout/stderr)
        for (const stream of Module.FS.streams) {
            if (stream && stream.stream_ops && stream.stream_ops.flush) {
                stream.stream_ops.flush(stream);
            }
        }
        console.log("Flushed stdout!");

        console.log('1 ' + messsage);
        setTimeout(() => {
            originalConsole.log('2 ' + messsage);
            // tell xharness WasmTestMessagesProcessor we are done. 
            console.log("WASM EXIT " + exit_code);
        }, 100);
    } else if (INTERNAL) {
        INTERNAL.mono_wasm_exit(exit_code);
    }
}

function processArguments(incomingArguments) {
    console.log("Incoming arguments: " + incomingArguments.join(' '));
    let profilers = [];
    let setenv = {};
    let runtime_args = [];
    let enable_gc = true;
    let working_dir = '/';
    while (incomingArguments && incomingArguments.length > 0) {
        const currentArg = incomingArguments[0];
        if (currentArg.startsWith("--profile=")) {
            const arg = currentArg.substring("--profile=".length);
            profilers.push(arg);
        } else if (currentArg.startsWith("--setenv=")) {
            const arg = currentArg.substring("--setenv=".length);
            const parts = arg.split('=');
            if (parts.length != 2)
                set_exit_code(1, "Error: malformed argument: '" + currentArg);
            setenv[parts[0]] = parts[1];
        } else if (currentArg.startsWith("--runtime-arg=")) {
            const arg = currentArg.substring("--runtime-arg=".length);
            runtime_args.push(arg);
        } else if (currentArg == "--disable-on-demand-gc") {
            enable_gc = false;
        } else if (currentArg.startsWith("--working-dir=")) {
            const arg = currentArg.substring("--working-dir=".length);
            working_dir = arg;
        } else {
            break;
        }
        incomingArguments = incomingArguments.slice(1);
    }

    // cheap way to let the testing infrastructure know we're running in a browser context (or not)
    setenv["IsBrowserDomSupported"] = is_browser.toString().toLowerCase();

    console.log("Application arguments: " + incomingArguments.join(' '));

    return {
        applicationArgs: incomingArguments,
        profilers,
        setenv,
        runtime_args,
        enable_gc,
        working_dir,
    }
}

let processedArguments = null;
// this can't be function because of `arguments` scope
try {
    if (is_node) {
        processedArguments = processArguments(process.argv.slice(2));
    } else if (is_browser) {
        // We expect to be run by tests/runtime/run.js which passes in the arguments using http parameters
        const url = new URL(decodeURI(window.location));
        let urlArguments = []
        for (let param of url.searchParams) {
            if (param[0] == "arg") {
                urlArguments.push(param[1]);
            }
        }
        processedArguments = processArguments(urlArguments);
    } else if (typeof arguments !== "undefined") {
        processedArguments = processArguments(Array.from(arguments));
    } else if (typeof scriptArgs !== "undefined") {
        processedArguments = processArguments(Array.from(scriptArgs));
    } else if (typeof WScript !== "undefined" && WScript.Arguments) {
        processedArguments = processArguments(Array.from(WScript.Arguments));
    }
} catch (e) {
    console.error(e);
}

async function loadDotnet(file) {
    let loadScript = undefined;
    if (typeof WScript !== "undefined") { // Chakra
        loadScript = function (file) {
            WScript.LoadScriptFile(file);
            return globalThis.Module;
        };
    } else if (is_node) { // NodeJS
        loadScript = async function (file) {
            return require(file);
        };
    } else if (is_browser) { // vanila JS in browser
        loadScript = async function (file) {
            const script = document.createElement("script");
            script.src = file;
            document.head.appendChild(script);
            let timeout = 100;
            // bysy spin waiting for script to load into global namespace
            while (timeout > 0) {
                if (globalThis.Module) {
                    return globalThis.Module;
                }
                // delay 10ms
                await new Promise(resolve => setTimeout(resolve, 10));
                timeout--;
            }
            throw new Error("Can't load " + file);
        }
    }
    else if (typeof globalThis.load !== 'undefined') {
        loadScript = async function (file) {
            globalThis.load(file)
            return globalThis.Module;
        }
    }
    else {
        throw new Error("Unknown environment, can't load config");
    }

    return loadScript(file);
}

loadDotnet("./dotnet.js").catch(function (err) {
    console.error(err);
    set_exit_code(1, "failed to load the dotnet.js file");
    throw err;
});