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

if (is_node && process.versions.node.split(".")[0] < 14) {
    throw new Error(`NodeJS at '${process.execPath}' has too low version '${process.versions.node}'`);
}

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

let runArgs = {};
let consoleWebSocket;
let is_debugging = false;
let forward_console = true;

function proxyConsoleMethod(prefix, func, asJson) {
    return function () {
        try {
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

            if (payload.startsWith("STARTRESULTXML")) {
                originalConsole.log('Sending RESULTXML')
                func(payload);
            }
            else if (asJson) {
                func(JSON.stringify({
                    method: prefix,
                    payload: payload,
                    arguments: args
                }));
            } else {
                func([prefix + payload, ...args.slice(1)]);
            }
        } catch (err) {
            originalConsole.error(`proxyConsole failed: ${err}`)
        }
    };
};

function set_exit_code(exit_code, reason) {
    if (reason) {
        if (reason instanceof Error)
            console.error(stringify_as_error_with_stack(reason));
        else if (typeof reason == "string")
            console.error(reason);
        else
            console.error(JSON.stringify(reason));
    }

    if (is_browser) {
        if (App.Module) {
            // Notify the selenium script
            App.Module.exit_code = exit_code;
        }

        //Tell xharness WasmBrowserTestRunner what was the exit code
        const tests_done_elem = document.createElement("label");
        tests_done_elem.id = "tests_done";
        tests_done_elem.innerHTML = exit_code.toString();
        document.body.appendChild(tests_done_elem);

        if (forward_console) {
            const stop_when_ws_buffer_empty = () => {
                if (consoleWebSocket.bufferedAmount == 0) {
                    // tell xharness WasmTestMessagesProcessor we are done.
                    // note this sends last few bytes into the same WS
                    console.log("WASM EXIT " + exit_code);
                }
                else {
                    setTimeout(stop_when_ws_buffer_empty, 100);
                }
            };
            stop_when_ws_buffer_empty();
        } else {
            console.log("WASM EXIT " + exit_code);
        }

    } else if (App && App.INTERNAL) {
        if (is_node) {
            let _flush = function(_stream) {
                return new Promise((resolve, reject) => {
                    if (!_stream.write('')) {
                        _stream.on('drain', () => resolve());
                        setTimeout(reject, 3000);
                    } else {
                        resolve();
                    }
                });
            };
            let stderrFlushed = _flush(process.stderr);
            let stdoutFlushed = _flush(process.stdout);

            Promise.all([ stdoutFlushed, stderrFlushed ])
                    .then(
                        () => App.INTERNAL.mono_wasm_exit(exit_code),
                        reason => {
                            console.error(`flushing std* streams failed: ${reason}`);
                            App.INTERNAL.mono_wasm_exit(123);
                        });
        } else {
            App.INTERNAL.mono_wasm_exit(exit_code);
        }
    }
}

function stringify_as_error_with_stack(err) {
    if (!err)
        return "";

    // FIXME:
    if (App && App.INTERNAL)
        return App.INTERNAL.mono_wasm_stringify_as_error_with_stack(err);

    if (err.stack)
        return err.stack;

    if (typeof err == "string")
        return err;

    return JSON.stringify(err);
}

function initRunArgs() {
    // set defaults
    runArgs.applicationArguments = runArgs.applicationArguments === undefined ? [] : runArgs.applicationArguments;
    runArgs.profilers = runArgs.profilers === undefined ? [] : runArgs.profilers;
    runArgs.workingDirectory = runArgs.workingDirectory === undefined ? '/' : runArgs.workingDirectory;
    runArgs.environment_variables = runArgs.environment_variables === undefined ? {} : runArgs.environment_variables;
    runArgs.runtimeArgs = runArgs.runtimeArgs === undefined ? [] : runArgs.runtimeArgs;
    runArgs.enableGC = runArgs.enableGC === undefined ? true : runArgs.enableGC;
    runArgs.diagnosticTracing = runArgs.diagnosticTracing === undefined ? false : runArgs.diagnosticTracing;
    runArgs.debugging = runArgs.debugging === undefined ? false : runArgs.debugging;
    // default'ing to true for tests, unless debugging
    runArgs.forwardConsole = runArgs.forwardConsole === undefined ? !runArgs.debugging : runArgs.forwardConsole;
}

function processQueryArguments(incomingArguments) {
    initRunArgs();

    console.log("Incoming arguments: " + incomingArguments.join(' '));
    while (incomingArguments && incomingArguments.length > 0) {
        const currentArg = incomingArguments[0];
        if (currentArg.startsWith("--profile=")) {
            const arg = currentArg.substring("--profile=".length);
            runArgs.profilers.push(arg);
        } else if (currentArg.startsWith("--setenv=")) {
            const arg = currentArg.substring("--setenv=".length);
            const parts = arg.split('=');
            if (parts.length != 2)
                set_exit_code(1, "Error: malformed argument: '" + currentArg);
            runArgs.environment_variables[parts[0]] = parts[1];
        } else if (currentArg.startsWith("--runtime-arg=")) {
            const arg = currentArg.substring("--runtime-arg=".length);
            runArgs.runtimeArgs.push(arg);
        } else if (currentArg == "--disable-on-demand-gc") {
            runArgs.enableGC = false;
        } else if (currentArg == "--diagnostic_tracing") {
            runArgs.diagnosticTracing = true;
        } else if (currentArg.startsWith("--working-dir=")) {
            const arg = currentArg.substring("--working-dir=".length);
            runArgs.workingDirectory = arg;
        } else if (currentArg == "--debug") {
            runArgs.debugging = true;
        } else if (currentArg == "--no-forward-console") {
            runArgs.forwardConsole = false;
        } else if (currentArg.startsWith("--fetch-random-delay=")) {
            const arg = currentArg.substring("--fetch-random-delay=".length);
            if (is_browser) {
                const delayms = Number.parseInt(arg) || 100;
                const originalFetch = globalThis.fetch;
                globalThis.fetch = async (url, options) => {
                    // random sleep
                    const ms = delayms + (Math.random() * delayms);
                    console.log(`fetch ${url} started ${ms}`)
                    await new Promise(resolve => setTimeout(resolve, ms));
                    console.log(`fetch ${url} delayed ${ms}`)
                    const res = await originalFetch(url, options);
                    console.log(`fetch ${url} done ${ms}`)
                    return res;
                }
            } else {
                console.warn("--fetch-random-delay only works on browser")
            }
        } else {
            break;
        }
        incomingArguments = incomingArguments.slice(1);
    }

    runArgs.applicationArguments = incomingArguments;
    // cheap way to let the testing infrastructure know we're running in a browser context (or not)
    runArgs.environment_variables["IsBrowserDomSupported"] = is_browser.toString().toLowerCase();
    runArgs.environment_variables["IsNodeJS"] = is_node.toString().toLowerCase();

    return runArgs;
}

function applyArguments() {
    initRunArgs();

    // set defaults
    is_debugging = runArgs.debugging === true;
    forward_console = runArgs.forwardConsole === true;

    console.log("Application arguments: " + runArgs.applicationArguments.join(' '));

    if (forward_console) {
        const methods = ["debug", "trace", "warn", "info", "error"];
        for (let m of methods) {
            if (typeof (console[m]) !== "function") {
                console[m] = proxyConsoleMethod(`console.${m}: `, console.log, false);
            }
        }

        if (is_browser) {
            const consoleUrl = `${window.location.origin}/console`.replace('http://', 'ws://');

            consoleWebSocket = new WebSocket(consoleUrl);
            consoleWebSocket.onopen = function (event) {
                originalConsole.log("browser: Console websocket connected.");
            };
            consoleWebSocket.onerror = function (event) {
                originalConsole.error(`websocket error: ${event}`, event);
            };
            consoleWebSocket.onclose = function (event) {
                originalConsole.error(`websocket closed: ${event}`, event);
            };

            const send = (msg) => {
                if (consoleWebSocket.readyState === WebSocket.OPEN) {
                    consoleWebSocket.send(msg);
                }
                else {
                    originalConsole.log(msg);
                }
            }

            // redirect output early, so that when emscripten starts it's already redirected
            for (let m of ["log", ...methods])
                console[m] = proxyConsoleMethod(`console.${m}`, send, true);
        }
    }
}

async function loadDotnet(file) {
    let loadScript = undefined;
    if (typeof WScript !== "undefined") { // Chakra
        loadScript = function (file) {
            WScript.LoadScriptFile(file);
            return globalThis.createDotnetRuntime;
        };
    } else if (is_node) { // NodeJS
        loadScript = async function (file) {
            return require(file);
        };
    } else if (is_browser) { // vanila JS in browser
        loadScript = function (file) {
            var loaded = new Promise((resolve, reject) => {
                globalThis.__onDotnetRuntimeLoaded = (createDotnetRuntime) => {
                    // this is callback we have in CJS version of the runtime
                    resolve(createDotnetRuntime);
                };
                import(file).then(({ default: createDotnetRuntime }) => {
                    // this would work with ES6 default export
                    if (createDotnetRuntime) {
                        resolve(createDotnetRuntime);
                    }
                }, reject);
            });
            return loaded;
        }
    }
    else if (typeof globalThis.load !== 'undefined') {
        loadScript = async function (file) {
            globalThis.load(file)
            return globalThis.createDotnetRuntime;
        }
    }
    else {
        throw new Error("Unknown environment, can't load config");
    }

    return loadScript(file);
}

// this can't be function because of `arguments` scope
let queryArguments = [];
try {
    if (is_node) {
        queryArguments = process.argv.slice(2);
    } else if (is_browser) {
        // We expect to be run by tests/runtime/run.js which passes in the arguments using http parameters
        const url = new URL(decodeURI(window.location));
        let urlArguments = []
        for (let param of url.searchParams) {
            if (param[0] == "arg") {
                urlArguments.push(param[1]);
            }
        }
        queryArguments = urlArguments;
    } else if (typeof arguments !== "undefined") {
        queryArguments = Array.from(arguments);
    } else if (typeof scriptArgs !== "undefined") {
        queryArguments = Array.from(scriptArgs);
    } else if (typeof WScript !== "undefined" && WScript.Arguments) {
        queryArguments = Array.from(WScript.Arguments);
    }
} catch (e) {
    console.error(e);
}

let loadDotnetPromise = loadDotnet('./dotnet.js');
let argsPromise;

if (queryArguments.length > 0) {
    argsPromise = Promise.resolve(processQueryArguments(queryArguments));
} else {
    argsPromise = fetch('/runArgs.json')
                        .then(async (response) => {
                            if (!response.ok) {
                                console.debug(`could not load /args.json: ${response.status}. Ignoring`);
                            } else {
                                runArgs = await response.json();
                                console.debug(`runArgs: ${JSON.stringify(runArgs)}`);
                            }
                        })
                        .catch(error => console.error(`Failed to load args: ${stringify_as_error_with_stack(error)}`));
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

let toAbsoluteUrl = function(possiblyRelativeUrl) { return possiblyRelativeUrl; }
if (is_browser) {
    const anchorTagForAbsoluteUrlConversions = document.createElement('a');
    toAbsoluteUrl = function toAbsoluteUrl(possiblyRelativeUrl) {
       anchorTagForAbsoluteUrlConversions.href = possiblyRelativeUrl;
       return anchorTagForAbsoluteUrlConversions.href;
    }
}

Promise.all([ argsPromise, loadDotnetPromise ]).then(async ([ _, createDotnetRuntime ]) => {
    applyArguments();

    if (is_node) {
        const modulesToLoad = runArgs.environment_variables["NPM_MODULES"];
        if (modulesToLoad) {
            modulesToLoad.split(',').forEach(module => {
                const { 0:moduleName, 1:globalAlias } = module.split(':');

                let message = `Loading npm '${moduleName}'`;
                let moduleExport = require(moduleName);

                if (globalAlias) {
                    message += ` and attaching to global as '${globalAlias}'`;
                    globalThis[globalAlias] = moduleExport;
                } else if(moduleName == "node-fetch") {
                    message += ' and attaching to global';
                    globalThis.fetch = moduleExport.default;
                    globalThis.Headers = moduleExport.Headers;
                    globalThis.Request = moduleExport.Request;
                    globalThis.Response = moduleExport.Response;
                } else if(moduleName == "node-abort-controller") {
                    message += ' and attaching to global';
                    globalThis.AbortController = moduleExport.AbortController;
                }

                console.log(message);
            });
        }
    }

    // Must be after loading npm modules.
    runArgs.environment_variables["IsWebSocketSupported"] = ("WebSocket" in globalThis).toString().toLowerCase();

    return createDotnetRuntime(({ MONO, INTERNAL, BINDING, Module }) => ({
        disableDotnet6Compatibility: true,
        config: null,
        configSrc: "./mono-config.json",
        locateFile: (path, prefix) => {
            return toAbsoluteUrl(prefix + path);
        },
        onConfigLoaded: (config) => {
            if (!Module.config) {
                const err = new Error("Could not find ./mono-config.json. Cancelling run");
                set_exit_code(1);
                throw err;
            }
            // Have to set env vars here to enable setting MONO_LOG_LEVEL etc.
            for (let variable in runArgs.environment_variables) {
                config.environment_variables[variable] = runArgs.environment_variables[variable];
            }
            config.diagnostic_tracing = !!runArgs.diagnosticTracing;
            if (is_debugging) {
                if (config.debug_level == 0)
                    config.debug_level = -1;

                config.wait_for_debugger = -1;
            }
        },
        preRun: () => {
            if (!runArgs.enableGC) {
                INTERNAL.mono_wasm_enable_on_demand_gc(0);
            }
        },
        onDotnetReady: () => {
            let wds = Module.FS.stat(runArgs.workingDirectory);
            if (wds === undefined || !Module.FS.isDir(wds.mode)) {
                set_exit_code(1, `Could not find working directory ${runArgs.workingDirectory}`);
                return;
            }

            Module.FS.chdir(runArgs.workingDirectory);

            App.init({ MONO, INTERNAL, BINDING, Module, runArgs });
        },
        onAbort: (error) => {
            set_exit_code(1, stringify_as_error_with_stack(new Error()));
        },
    }))
}).catch(function (err) {
    set_exit_code(1, "failed to load the dotnet.js file.\n" + stringify_as_error_with_stack(err));
});

const App = {
    init: async function ({ MONO, INTERNAL, BINDING, Module, runArgs }) {
        Object.assign(App, { MONO, INTERNAL, BINDING, Module, runArgs });
        console.info("Initializing.....");

        for (let i = 0; i < runArgs.profilers.length; ++i) {
            const init = Module.cwrap('mono_wasm_load_profiler_' + runArgs.profilers[i], 'void', ['string']);
            init("");
        }

        if (runArgs.applicationArguments.length == 0) {
            set_exit_code(1, "Missing required --run argument");
            return;
        }

        if (runArgs.applicationArguments[0] == "--regression") {
            const exec_regression = Module.cwrap('mono_wasm_exec_regression', 'number', ['number', 'string']);

            let res = 0;
            try {
                res = exec_regression(10, runArgs.applicationArguments[1]);
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

        if (runArgs.runtimeArgs.length > 0)
            INTERNAL.mono_wasm_set_runtime_options(runArgs.runtimeArgs);

        if (runArgs.applicationArguments[0] == "--run") {
            // Run an exe
            if (runArgs.applicationArguments.length == 1) {
                set_exit_code(1, "Error: Missing main executable argument.");
                return;
            }
            try {
                const main_assembly_name = runArgs.applicationArguments[1];
                const app_args = runArgs.applicationArguments.slice(2);
                const result = await App.MONO.mono_run_main(main_assembly_name, app_args);
                set_exit_code(result);
            } catch (error) {
                if (error.name != "ExitStatus") {
                    set_exit_code(1, error);
                }
            }
        } else {
            set_exit_code(1, "Unhandled argument: " + runArgs.applicationArguments[0]);
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
            return App.INTERNAL.call_static_method(fqn, args || [], signature);
        } catch (exc) {
            console.error("exception thrown in", fqn);
            throw exc;
        }
    }
};
globalThis.App = App; // Necessary as System.Runtime.InteropServices.JavaScript.Tests.MarshalTests (among others) call the App.call_test_method directly
