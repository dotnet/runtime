// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
"use strict";
import createDotnetRuntime from './dotnet.js'

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

        if (is_browser && document.getElementById("out"))
            document.getElementById("out").innerHTML = `error: ${reason}`;
    }

    if (runArgs && runArgs.forwardConsole) {
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
    if (App && App.runtime && App.runtime.INTERNAL)
        return App.runtime.INTERNAL.mono_wasm_stringify_as_error_with_stack(err);

    if (err.stack)
        return err.stack;

    if (typeof err == "string")
        return err;

    return JSON.stringify(err);
}

let runArgs = {};
let consoleWebSocket;

function initRunArgs() {
    // set defaults
    runArgs.applicationArguments = runArgs.applicationArguments === undefined ? [] : runArgs.applicationArguments;
    runArgs.workingDirectory = runArgs.workingDirectory === undefined ? '/' : runArgs.workingDirectory;
    runArgs.environmentVariables = runArgs.environmentVariables === undefined ? {} : runArgs.environmentVariables;
    runArgs.runtimeArgs = runArgs.runtimeArgs === undefined ? [] : runArgs.runtimeArgs;
    runArgs.diagnosticTracing = runArgs.diagnosticTracing === undefined ? false : runArgs.diagnosticTracing;
    runArgs.debugging = runArgs.debugging === undefined ? false : runArgs.debugging;
    runArgs.forwardConsole = runArgs.forwardConsole === undefined ? false : runArgs.forwardConsole;
}

function applyArguments() {
    if (!!runArgs.forwardConsole) {
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

App.run = async function run(main) {
    try {
        const argsResponse = await fetch('./runArgs.json')
        if (!argsResponse.ok) {
            console.debug(`could not load ./runArgs.json: ${argsResponse.status}. Ignoring`);
        } else {
            runArgs = await argsResponse.json();
            console.debug(`runArgs: ${JSON.stringify(runArgs)}`);
        }
        initRunArgs();
        applyArguments();

        const runtime = await createDotnetRuntime(({ Module, INTERNAL }) => ({
            disableDotnet6Compatibility: true,
            config: null,
            configSrc: "./mono-config.json",
            onConfigLoaded: (config) => {
                if (!config) {
                    const err = new Error("Could not find ./mono-config.json. Cancelling run");
                    set_exit_code(1);
                    throw err;
                }
                // Have to set env vars here to enable setting MONO_LOG_LEVEL etc.
                for (let variable in runArgs.environmentVariables) {
                    config.environmentVariables[variable] = runArgs.environmentVariables[variable];
                }
                config.diagnosticTracing = !!runArgs.diagnosticTracing;
                if (!!runArgs.debugging) {
                    if (config.debugLevel == 0)
                        config.debugLevel = -1;

                    config.waitForDebugger = -1;
                }
            },
            onDotnetReady: async () => {
                let wds = Module.FS.stat(runArgs.workingDirectory);
                if (wds === undefined || !Module.FS.isDir(wds.mode)) {
                    set_exit_code(1, `Could not find working directory ${runArgs.working_dir}`);
                    return;
                }

                Module.FS.chdir(runArgs.workingDirectory);

                if (runArgs.runtimeArgs.length > 0)
                    INTERNAL.mono_wasm_set_runtime_options(runArgs.runtimeArgs);
            },
            onAbort: (error) => {
                set_exit_code(1, error);
            },
        }));
        App.runtime = runtime;
        App.runArgs = runArgs;
        App.main = main;
        if (App.main) {
            let exit_code = await App.main(runArgs.applicationArguments);
            set_exit_code(exit_code ?? 0);
        }
        else {
            set_exit_code(1, "WASM ERROR: no App.main defined");
        }
    }
    catch (err) {
        set_exit_code(2, err);
    }
}