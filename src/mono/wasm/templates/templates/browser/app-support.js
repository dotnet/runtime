// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
"use strict";

const is_browser = typeof window != "undefined";
if (!is_browser)
    throw new Error(`Expected to be running in a browser`);

export const App = {};

const originalConsole = {
    log: console.log,
    error: console.error
};

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

            if (asJson) {
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

    if (forward_console)  {
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

let runArgs = {};
let consoleWebSocket;
let is_debugging = false;
let forward_console = false;

function initRunArgs() {
    // set defaults
    runArgs.applicationArguments = runArgs.applicationArguments === undefined ? [] : runArgs.applicationArguments;
    runArgs.workingDirectory = runArgs.workingDirectory === undefined ? '/' : runArgs.workingDirectory;
    runArgs.environment_variables = runArgs.environment_variables === undefined ? {} : runArgs.environment_variables;
    runArgs.runtimeArgs = runArgs.runtimeArgs === undefined ? [] : runArgs.runtimeArgs;
    runArgs.enableGC = runArgs.enableGC === undefined ? true : runArgs.enableGC;
    runArgs.diagnosticTracing = runArgs.diagnosticTracing === undefined ? false : runArgs.diagnosticTracing;
    runArgs.debugging = runArgs.debugging === undefined ? false : runArgs.debugging;
    runArgs.forwardConsole = runArgs.forwardConsole === undefined ? false : runArgs.forwardConsole;
}

function applyArguments() {
    initRunArgs();

    is_debugging = runArgs.debugging === true;
    forward_console = runArgs.forwardConsole === true;

    if (forward_console) {
        const methods = ["debug", "trace", "warn", "info", "error"];
        for (let m of methods) {
            if (typeof (console[m]) !== "function") {
                console[m] = proxyConsoleMethod(`console.${m}: `, console.log, false);
            }
        }

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

let toAbsoluteUrl = function(possiblyRelativeUrl) { return possiblyRelativeUrl; }
const anchorTagForAbsoluteUrlConversions = document.createElement('a');
toAbsoluteUrl = function toAbsoluteUrl(possiblyRelativeUrl) {
   anchorTagForAbsoluteUrlConversions.href = possiblyRelativeUrl;
   return anchorTagForAbsoluteUrlConversions.href;
}

let loadDotnetPromise = import('/dotnet.js');
let argsPromise = fetch('/runArgs.json')
                    .then(async (response) => {
                        if (!response.ok) {
                            console.debug(`could not load /args.json: ${response.status}. Ignoring`);
                        } else {
                            runArgs = await response.json();
                            console.debug(`runArgs: ${JSON.stringify(runArgs)}`);
                        }
                    })
                    .catch(error => console.error(`Failed to load args: ${stringify_as_error_with_stack(error)}`));

Promise.all([ argsPromise, loadDotnetPromise ]).then(async ([ _, { default: createDotnetRuntime } ]) => {
    applyArguments();

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
            for (let variable in runArgs.runtimeArgs) {
                config.environment_variables[variable] = runArgs.runtimeArgs[variable];
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
                set_exit_code(1, `Could not find working directory ${runArgs.working_dir}`);
                return;
            }

            Module.FS.chdir(runArgs.workingDirectory);

            if (runArgs.runtimeArgs.length > 0)
                INTERNAL.mono_wasm_set_runtime_options(runArgs.runtimeArgs);

            console.info("Initializing.....");
            Object.assign(App, { MONO, INTERNAL, BINDING, Module, runArgs });

            try {
                if (App.init)
                {
                    let ret = App.init();
                    Promise.resolve(ret).then(function (code) { set_exit_code(code ?? 0); });
                }
                else
                {
                    console.log("WASM ERROR: no App.init defined");
                    set_exit_code(1, "WASM ERROR: no App.init defined");
                }
            } catch (err) {
                console.log(`WASM ERROR ${err}`);
                if (is_browser && document.getElementById("out"))
                    document.getElementById("out").innerHTML = `error: ${err}`;
                set_exit_code(1, err);
            }
        },
        onAbort: (error) => {
            set_exit_code(1, stringify_as_error_with_stack(new Error()));
        },
    }));
}).catch(function (err) {
    set_exit_code(1, "failed to load the dotnet.js file.\n" + stringify_as_error_with_stack(err));
});
