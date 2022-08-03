// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
"use strict";
import createDotnetRuntime from './dotnet.js'
import { promises } from "fs";
import { argv } from "process";
const { readFile, stat } = promises;

const is_browser = false;
const is_node = !is_browser && typeof process === 'object' && typeof process.versions === 'object' && typeof process.versions.node === 'string';
export const App = {};

if (!is_node)
    throw new Error(`This file only supports nodejs`);

if (is_node && process.versions.node.split(".")[0] < 14) {
    throw new Error(`NodeJS at '${process.execPath}' has too low version '${process.versions.node}'`);
}

function stringify_as_error_with_stack(err) {
    if (!err)
        return "";

    // FIXME:
    if (App && App.runtime.INTERNAL)
        return App.runtime.INTERNAL.mono_wasm_stringify_as_error_with_stack(err);

    if (err.stack)
        return err.stack;

    if (typeof err == "string")
        return err;

    return JSON.stringify(err);
}

function set_exit_code(exit_code, reason) {
    if (reason) {
        if (reason instanceof Error)
            console.error(stringify_as_error_with_stack(reason));
        else if (typeof reason == "string")
            console.error(reason);
        else
            console.error(JSON.stringify(reason));
    }

    if (App && App.runtime && App.runtime.INTERNAL) {
        let _flush = function (_stream) {
            return new Promise((resolve, reject) => {
                _stream.on('error', (error) => reject(error));
                _stream.write('', function () { resolve() });
            });
        };
        let stderrFlushed = _flush(process.stderr);
        let stdoutFlushed = _flush(process.stdout);

        Promise.all([stdoutFlushed, stderrFlushed])
            .then(
                () => App.runtime.INTERNAL.mono_wasm_exit(exit_code),
                reason => {
                    console.error(`flushing std* streams failed: ${reason}`);
                    App.runtime.INTERNAL.mono_wasm_exit(123456);
                });
    }
}

let runArgs = {};
let is_debugging = false;

function initRunArgs() {
    // set defaults
    runArgs.applicationArguments = []; // not set in runArgs.json for non-browser cases
    runArgs.workingDirectory = runArgs.workingDirectory === undefined ? '/' : runArgs.workingDirectory;
    runArgs.environmentVariables = runArgs.environmentVariables === undefined ? {} : runArgs.environmentVariables;
    runArgs.runtimeArgs = runArgs.runtimeArgs === undefined ? [] : runArgs.runtimeArgs;
    runArgs.diagnosticTracing = runArgs.diagnosticTracing === undefined ? false : runArgs.diagnosticTracing;
    runArgs.debugging = runArgs.debugging === undefined ? false : runArgs.debugging;
    runArgs.forwardConsole = false; // not relevant for non-browser
}

function mergeArguments() {
    let incomingArguments = argv.slice(2);

    while (incomingArguments && incomingArguments.length > 0) {
        const currentArg = incomingArguments[0];
        if (currentArg.startsWith("--setenv=")) {
            const arg = currentArg.substring("--setenv=".length);
            const parts = arg.split('=');
            if (parts.length != 2)
                set_exit_code(1, "Error: malformed argument: '" + currentArg);
            runArgs.environmentVariables[parts[0]] = parts[1];
        } else if (currentArg.startsWith("--runtime-arg=")) {
            const arg = currentArg.substring("--runtime-arg=".length);
            runArgs.runtimeArgs.push(arg);
        } else if (currentArg == "--diagnostic-tracing") {
            runArgs.diagnosticTracing = true;
        } else if (currentArg.startsWith("--working-dir=")) {
            const arg = currentArg.substring("--working-dir=".length);
            runArgs.workingDirectory = arg;
        } else if (currentArg == "--debug") {
            runArgs.debugging = true;
        } else {
            break;
        }
        incomingArguments = incomingArguments.slice(1);
    }
    runArgs.applicationArguments = incomingArguments;

    is_debugging = runArgs.debugging === true;
}

App.run = async function run(main) {
    try {
        try {
            if (await stat('./runArgs.json')) {
                const argsJson = await readFile('./runArgs.json', { encoding: "utf8" });
                runArgs = JSON.parse(argsJson);
                console.debug(`runArgs: ${JSON.stringify(runArgs)}`);
            }
        } catch (err) {
            console.debug(`could not load ./runArgs.json: ${err}. Ignoring`);
        }
        initRunArgs();
        mergeArguments();

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
                if (is_debugging) {
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