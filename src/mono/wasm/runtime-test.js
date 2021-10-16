// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
// Run runtime tests under a JS shell or a browser
//
"use strict";

//glue code to deal with the differences between chrome, ch, d8, jsc and sm.
const is_browser = typeof window != "undefined";
const is_node = !is_browser && typeof process != 'undefined';

// setup the globalThis pollyfill as it is not defined on older versions of node
if (is_node && !global.globalThis) {
    global.globalThis = global;
}

// if the engine doesn't provide a console
if (typeof (globalThis.console) === "undefined") {
    globalThis.console = {
        log: globalThis.print,
        clear: function () { }
    };
}

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
    consoleWebSocket.onopen = function (event) {
        proxyJson(function (msg) { consoleWebSocket.send(msg); });
        console.log("browser: Console websocket connected.");
    };
    consoleWebSocket.onerror = function (event) {
        console.error(`websocket error: ${event}`);
    };
}

if (typeof globalThis.crypto === 'undefined') {
    // **NOTE** this is a simple insecure polyfill for testing purposes only
    // /dev/random doesn't work on js shells, so define our own
    // See library_fs.js:createDefaultDevices ()
    globalThis.crypto = {
        getRandomValues: function (buffer) {
            for (var i = 0; i < buffer.length; i++)
                buffer[i] = (Math.random() * 256) | 0;
        }
    }
}
if (is_node) {
    var { performance } = require("perf_hooks");
} else if (typeof performance === 'undefined') {
    // performance.now() is used by emscripten and doesn't work in JSC
    var performance = {
        now: function () {
            return Date.now();
        }
    }
}

// abstract all IO into a compact universally available method so that it is consistent and reliable
const IOHandler = {
    /** Load js file into project and evaluate it
     * @type {(file: string) => Promise<void> | null}
     * @param {string} file path to the file to load
    */
    load: null,

    /** Read and return the contents of a file as a string
     * @type {(file: string) => Promise<string> | null}
     * @param {string} file the path to the file to read
     * @return {string} the contents of the file
    */
    read: null,

    /** Sets up the load and read functions for later
     * @type {() => void}
     */
    init: function () {
        // load: function that loads and executes a script
        let loadFunc = globalThis.load; // shells (v8, JavaScriptCore, Spidermonkey)
        if (!loadFunc) {
            if (typeof WScript !== "undefined") { // Chakra
                loadFunc = WScript.LoadScriptFile;

            } else if (is_node) { // NodeJS
                loadFunc = async function (file) {
                    let req = require(file);

                    // sometimes the require returns a function which returns a promise (such as in dotnet.js).
                    // othertimes it returns the variable or object that is needed. We handle both cases
                    if (typeof (req) === 'function') {
                        req = await req(Module); // pass Module so emsdk can use it
                    }

                    // add to Module
                    Module = Object.assign(req, Module);
                };
            } else if (is_browser) { // vanila JS in browser
                loadFunc = function (file) {
                    const script = document.createElement("script");
                    script.src = file;
                    document.head.appendChild(script);
                }
            }
        }
        IOHandler.load = async (file) => await loadFunc(file);

        // read: function that just reads a file into a variable
        let readFunc = globalThis.read; // shells (v8, JavaScriptCore, Spidermonkey)
        if (!readFunc) {
            if (typeof WScript !== "undefined") {
                readFunc = WScript.LoadBinaryFile; // Chakra

            } else if (is_node) { // NodeJS
                const fs = require('fs');
                readFunc = function (path) {
                    return fs.readFileSync(path).toString();
                };
            } else if (is_browser) {  // vanila JS in browser
                readFunc = fetch;
            }
        }
        IOHandler.read = async (file) => await readFunc(file);
    },

    /** Returns an async fetch request
     * @type {(path: string, params: object) => Promise<{ok: boolean, url: string, arrayBuffer: Promise<Uint8Array>}>}
     * @param {string} path the path to the file to fetch
     * @param {object} params additional parameters to fetch with. Only used on browser
     * @returns {Promise<{ok: boolean, url: string, arrayBuffer: Promise<Uint8Array>}>} The result of the request
     */
    fetch: function (path, params) {
        if (is_browser) {
            return fetch(path, params);

        } else { // shells and node
            return new Promise((resolve, reject) => {
                let bytes = null, error = null;
                try {
                    if (is_node) {
                        const fs = require('fs');
                        bytes = fs.readFileSync(path);
                    } else {
                        bytes = read(path, 'binary');
                    }
                } catch (exc) {
                    error = exc;
                }
                const response = {
                    ok: (bytes && !error),
                    url: path,
                    arrayBuffer: function () {
                        return new Promise((resolve2, reject2) => {
                            if (error)
                                reject2(error);
                            else
                                resolve2(new Uint8Array(bytes));
                        }
                        )
                    }
                }
                resolve(response);
            });
        }
    }
};
IOHandler.init();
// end of all the nice shell glue code.

function fail_exec(exit_code, reason) {
    if (reason) {
        console.error(reason);
    }
    if (is_browser) {
        // Notify the selenium script
        Module.exit_code = exit_code;
        console.log("WASM EXIT " + exit_code);
        const tests_done_elem = document.createElement("label");
        tests_done_elem.id = "tests_done";
        tests_done_elem.innerHTML = exit_code.toString();
        document.body.appendChild(tests_done_elem);
    } else { // shell or node
        Module.INTERNAL.mono_wasm_exit(exit_code);
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

function processArguments(incommingArguments) {
    console.log("Incomming arguments: " + incommingArguments.join(' '));
    let profilers = [];
    let setenv = {};
    let runtime_args = [];
    let enable_gc = true;
    let working_dir = '/';
    while (incommingArguments && incommingArguments.length > 0) {
        const currentArg = incommingArguments[0];
        if (currentArg.startsWith("--profile=")) {
            const arg = currentArg.substring("--profile=".length);
            profilers.push(arg);
        } else if (currentArg.startsWith("--setenv=")) {
            const arg = currentArg.substring("--setenv=".length);
            const parts = arg.split('=');
            if (parts.length != 2)
                fail_exec(1, "Error: malformed argument: '" + currentArg);
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
        incommingArguments = incommingArguments.slice(1);
    }

    // cheap way to let the testing infrastructure know we're running in a browser context (or not)
    setenv["IsBrowserDomSupported"] = is_browser.toString().toLowerCase();

    console.log("Application arguments: " + incommingArguments.join(' '));

    return {
        applicationArgs: incommingArguments,
        profilers,
        setenv,
        runtime_args,
        enable_gc,
        working_dir,
    }
}

// must be var as dotnet.js uses it
var Module = {
    no_global_exports: true,
    mainScriptUrlOrBlob: "dotnet.js",
    config: null,

    /** Called before the runtime is loaded and before it is run
     * @type {() => Promise<void>}
     */
    preInit: async function () {
        await Module.MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.config implicitly
    },

    /** Called after an exception occurs during execution
     * @type {(x: string|number=) => void}
     * @param {string|number} x error message
     */
    onAbort: function (x) {
        console.log("ABORT: " + x);
        const err = new Error();
        console.log("Stacktrace: \n");
        console.error(err.stack);
        fail_exec(1);
    },

    /** Called after the runtime is loaded but before it is run mostly prepares runtime and config for the tests
     * @type {() => void}
     */
    onRuntimeInitialized: function () {
        if (!Module.config) {
            console.error("Could not find ./mono-config.json. Cancelling run");
            fail_exec(1);
        }
        // Have to set env vars here to enable setting MONO_LOG_LEVEL etc.
        for (let variable in processedArguments.setenv) {
            Module.MONO.mono_wasm_setenv(variable, processedArguments.setenv[variable]);
        }

        if (!processedArguments.enable_gc) {
            Module.INTERNAL.mono_wasm_enable_on_demand_gc(0);
        }

        Module.config.loaded_cb = function () {
            let wds = Module.FS.stat(processedArguments.working_dir);
            if (wds === undefined || !Module.FS.isDir(wds.mode)) {
                fail_exec(1, `Could not find working directory ${processedArguments.working_dir}`);
                return;
            }

            Module.FS.chdir(processedArguments.working_dir);
            App.init();
        };
        Module.config.fetch_file_cb = function (asset) {
            return IOHandler.fetch(asset, { credentials: 'same-origin' });
        };

        Module.MONO.mono_load_runtime_and_bcl_args(Module.config);
    },
};

var App = {
    /** Runs the tests (runtime is now loaded and running)
     * @type {() => void}
     */
    init: function () {
        console.info("Initializing.....");

        for (let i = 0; i < processedArguments.profilers.length; ++i) {
            const init = Module.cwrap('mono_wasm_load_profiler_' + processedArguments.profilers[i], 'void', ['string']);
            init("");
        }

        if (processedArguments.applicationArgs.length == 0) {
            fail_exec(1, "Missing required --run argument");
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
                fail_exec(1, "REGRESSION TEST FAILED");

            return;
        }

        if (processedArguments.runtime_args.length > 0)
            Module.INTERNAL.mono_wasm_set_runtime_options(processedArguments.runtime_args);

        if (processedArguments.applicationArgs[0] == "--run") {
            // Run an exe
            if (processedArguments.applicationArgs.length == 1) {
                fail_exec(1, "Error: Missing main executable argument.");
                return;
            }

            const main_assembly_name = processedArguments.applicationArgs[1];
            const app_args = processedArguments.applicationArgs.slice(2);
            Module.INTERNAL.mono_wasm_set_main_args(processedArguments.applicationArgs[1], app_args);

            // Automatic signature isn't working correctly
            const result = Module.BINDING.call_assembly_entry_point(main_assembly_name, [app_args], "m");
            const onError = function (error) {
                console.error(error);
                if (error.stack)
                    console.error(error.stack);

                fail_exec(1);
            }
            try {
                result.then(fail_exec).catch(onError);
            } catch (error) {
                onError(error);
            }

        } else {
            fail_exec(1, "Unhandled argument: " + processedArguments.applicationArgs[0]);
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
            return Module.INTERNAL.call_static_method(fqn, args || [], signature);
        } catch (exc) {
            console.error("exception thrown in", fqn);
            throw exc;
        }
    }
};
globalThis.App = App; // Necessary as System.Runtime.InteropServices.JavaScript.Tests.MarshalTests (among others) call the App.call_test_method directly

// load the config and runtime files which will start the runtime init and subsiquently the tests
// uses promise chain as loading is async but we can't use await here
IOHandler
    .load("./dotnet.js")
    .catch(function (err) {
        console.error(err);
        fail_exec(1, "failed to load the dotnet.js file");
    });