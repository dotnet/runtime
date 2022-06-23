// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
"use strict";

//glue code to deal with the differences between chrome, ch, d8, jsc and sm.
const is_browser = false;
const is_node = !is_browser && typeof process === 'object' && typeof process.versions === 'object' && typeof process.versions.node === 'string';
const App = {};

if (!is_node)
    throw new Error(`This file only supports nodejs`);

if (is_node && process.versions.node.split(".")[0] < 14) {
    throw new Error(`NodeJS at '${process.execPath}' has too low version '${process.versions.node}'`);
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

function set_exit_code(exit_code, reason) {
    if (reason) {
        if (reason instanceof Error)
            console.error(stringify_as_error_with_stack(reason));
        else if (typeof reason == "string")
            console.error(reason);
        else
            console.error(JSON.stringify(reason));
    }

    if (App && App.INTERNAL) {
        App.INTERNAL.mono_wasm_exit(exit_code);
    }
}

let processedArguments = null;
let is_debugging = false;

function processArguments(incomingArguments) {
    console.log("Incoming arguments: " + incomingArguments.join(' '));
    let profilers = [];
    let setenv = {};
    let runtime_args = [];
    let enable_gc = true;
    let diagnostic_tracing = false;
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
        } else if (currentArg == "--diagnostic_tracing") {
            diagnostic_tracing = true;
        } else if (currentArg.startsWith("--working-dir=")) {
            const arg = currentArg.substring("--working-dir=".length);
            working_dir = arg;
        } else if (currentArg == "--debug") {
            is_debugging = true;
        } else {
            break;
        }
        incomingArguments = incomingArguments.slice(1);
    }

    // cheap way to let the testing infrastructure know we're running in a browser context (or not)
    setenv["IsBrowserDomSupported"] = is_browser.toString().toLowerCase();
    setenv["IsNodeJS"] = is_node.toString().toLowerCase();

    console.log("Application arguments: " + incomingArguments.join(' '));

    return {
        applicationArgs: incomingArguments,
        profilers,
        setenv,
        runtime_args,
        enable_gc,
        diagnostic_tracing,
        working_dir,
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
    } else if (typeof globalThis.load !== 'undefined') {
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
try {
    if (is_node) {
        processedArguments = processArguments(process.argv.slice(2));
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

if (is_node) {
    const modulesToLoad = processedArguments.setenv["NPM_MODULES"];
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

if (is_node) {
    module.exports.App = App;
    module.exports.is_browser = is_browser;
    module.exports.is_node = is_node;
    module.exports.set_exit_code = set_exit_code;
}

let toAbsoluteUrl = function(possiblyRelativeUrl) { return possiblyRelativeUrl; }

// Must be after loading npm modules.
processedArguments.setenv["IsWebSocketSupported"] = ("WebSocket" in globalThis).toString().toLowerCase();

loadDotnet("./dotnet.js").then((createDotnetRuntime) => {
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
            for (let variable in processedArguments.setenv) {
                config.environment_variables[variable] = processedArguments.setenv[variable];
            }
            config.diagnostic_tracing = !!processedArguments.diagnostic_tracing;
            if (is_debugging && config.debug_level == 0)
                config.debug_level = -1;
        },
        preRun: () => {
            if (!processedArguments.enable_gc) {
                INTERNAL.mono_wasm_enable_on_demand_gc(0);
            }
        },
        onDotnetReady: () => {
            let wds = Module.FS.stat(processedArguments.working_dir);
            if (wds === undefined || !Module.FS.isDir(wds.mode)) {
                set_exit_code(1, `Could not find working directory ${processedArguments.working_dir}`);
                return;
            }

            Module.FS.chdir(processedArguments.working_dir);

            if (processedArguments.runtime_args.length > 0)
                INTERNAL.mono_wasm_set_runtime_options(processedArguments.runtime_args);

            console.info("Initializing.....");
            Object.assign(App, { MONO, INTERNAL, BINDING, Module, processedArguments });

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
                set_exit_code(1, err);
            }
        },
        onAbort: (error) => {
            set_exit_code(1, stringify_as_error_with_stack(new Error()));
        },
    }))
}).catch(function (err) {
    set_exit_code(1, "failed to load the dotnet.js file.\n" + stringify_as_error_with_stack(err));
});

