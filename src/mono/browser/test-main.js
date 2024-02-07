// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
// Run runtime tests under a JS shell or a browser
//
import { dotnet, exit } from './_framework/dotnet.js';


/*****************************************************************************
 * Please don't use this as template for startup code.
 * There are simpler and better samples like src\mono\sample\wasm\browser\main.js
 * It has edge case polyfills.
 * It handles strange things which happen with XHarness.
 ****************************************************************************/


//glue code to deal with the differences between chrome, ch, d8, jsc and sm.

// keep in sync with src\mono\browser\runtime\loader\globals.ts and src\mono\browser\runtime\globals.ts
export const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
export const ENVIRONMENT_IS_WEB_WORKER = typeof importScripts == "function";
export const ENVIRONMENT_IS_SIDECAR = ENVIRONMENT_IS_WEB_WORKER && typeof dotnetSidecar !== "undefined"; // sidecar is emscripten main running in a web worker
export const ENVIRONMENT_IS_WORKER = ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_SIDECAR; // we redefine what ENVIRONMENT_IS_WORKER, we replace it in emscripten internals, so that sidecar works
export const ENVIRONMENT_IS_WEB = typeof window == "object" || (ENVIRONMENT_IS_WEB_WORKER && !ENVIRONMENT_IS_NODE);
export const ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE;

if (ENVIRONMENT_IS_NODE && process.versions.node.split(".")[0] < 14) {
    throw new Error(`NodeJS at '${process.execPath}' has too low version '${process.versions.node}'`);
}

if (ENVIRONMENT_IS_NODE) {
    // the emscripten 3.1.34 stopped handling these when MODULARIZE is enabled
    process.on('uncaughtException', function (ex) {
        // ignore UnhandledPromiseRejection exceptions with exit status
        if (ex !== 'unwind' && (ex.name !== "UnhandledPromiseRejection" || !ex.message.includes('"#<ExitStatus>"'))) {
            throw ex;
        }
    });
}

if (!ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WEB && typeof globalThis.crypto === 'undefined') {
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

if (ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_WORKER) {
    console.log("Running at: " + globalThis.location.href);
}

let v8args;
if (typeof arguments !== "undefined") {
    // this must be captured in top level scope in V8
    v8args = arguments;
}

async function getArgs() {
    let queryArguments = [];
    if (ENVIRONMENT_IS_NODE) {
        queryArguments = process.argv.slice(2);
    } else if (ENVIRONMENT_IS_WEB) {
        // We expect to be run by tests/runtime/run.js which passes in the arguments using http parameters
        const url = new URL(decodeURI(globalThis.location));
        let urlArguments = []
        for (let param of url.searchParams) {
            if (param[0] == "arg") {
                urlArguments.push(param[1]);
            }
        }
        queryArguments = urlArguments;
    } else if (v8args !== undefined) {
        queryArguments = Array.from(v8args);
    } else if (typeof scriptArgs !== "undefined") {
        queryArguments = Array.from(scriptArgs);
    } else if (typeof WScript !== "undefined" && WScript.Arguments) {
        queryArguments = Array.from(WScript.Arguments);
    }

    let runArgsJson;
    // ToDo: runArgs should be read for all kinds of hosts, but
    // fetch is added to node>=18 and current Windows's emcc node<18
    if (ENVIRONMENT_IS_WEB) {
        const response = await globalThis.fetch('./runArgs.json');
        if (response.ok) {
            runArgsJson = initRunArgs(await response.json());
        } else {
            console.debug(`could not load /runArgs.json: ${response.status}. Ignoring`);
        }
    }
    if (!runArgsJson)
        runArgsJson = initRunArgs({});
    return processArguments(queryArguments, runArgsJson);
}

function initRunArgs(runArgs) {
    // set defaults
    runArgs.applicationArguments = runArgs.applicationArguments === undefined ? [] : runArgs.applicationArguments;
    runArgs.profilers = runArgs.profilers === undefined ? [] : runArgs.profilers;
    runArgs.workingDirectory = runArgs.workingDirectory === undefined ? '/' : runArgs.workingDirectory;
    runArgs.environmentVariables = runArgs.environmentVariables === undefined ? {} : runArgs.environmentVariables;
    runArgs.runtimeArgs = runArgs.runtimeArgs === undefined ? [] : runArgs.runtimeArgs;
    runArgs.enableGC = runArgs.enableGC === undefined ? true : runArgs.enableGC;
    runArgs.diagnosticTracing = runArgs.diagnosticTracing === undefined ? false : runArgs.diagnosticTracing;
    runArgs.debugging = runArgs.debugging === undefined ? false : runArgs.debugging;
    runArgs.configSrc = runArgs.configSrc === undefined ? './_framework/blazor.boot.json' : runArgs.configSrc;
    // default'ing to true for tests, unless debugging
    runArgs.forwardConsole = runArgs.forwardConsole === undefined ? !runArgs.debugging : runArgs.forwardConsole;
    runArgs.memorySnapshot = runArgs.memorySnapshot === undefined ? true : runArgs.memorySnapshot;
    runArgs.interpreterPgo = runArgs.interpreterPgo === undefined ? false : runArgs.interpreterPgo;

    return runArgs;
}

function processArguments(incomingArguments, runArgs) {
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
            runArgs.environmentVariables[parts[0]] = parts[1];
        } else if (currentArg.startsWith("--runtime-arg=")) {
            const arg = currentArg.substring("--runtime-arg=".length);
            runArgs.runtimeArgs.push(arg);
        } else if (currentArg == "--disable-on-demand-gc") {
            runArgs.enableGC = false;
        } else if (currentArg == "--diagnostic-tracing") {
            runArgs.diagnosticTracing = true;
        } else if (currentArg.startsWith("--working-dir=")) {
            const arg = currentArg.substring("--working-dir=".length);
            runArgs.workingDirectory = arg;
        } else if (currentArg == "--debug") {
            runArgs.debugging = true;
        } else if (currentArg == "--no-forward-console") {
            runArgs.forwardConsole = false;
        } else if (currentArg == "--no-memory-snapshot") {
            runArgs.memorySnapshot = false;
        } else if (currentArg == "--interpreter-pgo") {
            runArgs.interpreterPgo = true;
        } else if (currentArg == "--no-interpreter-pgo") {
            runArgs.interpreterPgo = false;
        } else if (currentArg.startsWith("--fetch-random-delay=")) {
            const arg = currentArg.substring("--fetch-random-delay=".length);
            if (ENVIRONMENT_IS_WEB) {
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
        } else if (currentArg.startsWith("--config-src=")) {
            const arg = currentArg.substring("--config-src=".length);
            runArgs.configSrc = arg;
        } else {
            break;
        }
        incomingArguments = incomingArguments.slice(1);
    }

    runArgs.applicationArguments = incomingArguments;
    // cheap way to let the testing infrastructure know we're running in a browser context (or not)
    runArgs.environmentVariables["IsBrowserDomSupported"] = ENVIRONMENT_IS_WEB.toString().toLowerCase();
    runArgs.environmentVariables["IsNodeJS"] = ENVIRONMENT_IS_NODE.toString().toLowerCase();

    return runArgs;
}

// we may have dependencies on NPM packages, depending on the test case
// some of them polyfill for browser built-in stuff
function loadNodeModules(config, require, modulesToLoad) {
    modulesToLoad.split(',').forEach(module => {
        const { 0: moduleName, 1: globalAlias } = module.split(':');

        let message = `Loading npm '${moduleName}'`;
        let moduleExport = require(moduleName);

        if (globalAlias) {
            message += ` and attaching to global as '${globalAlias}'`;
            globalThis[globalAlias] = moduleExport;
        } else if (moduleName == "node-fetch") {
            message += ' and attaching to global';
            globalThis.fetch = moduleExport.default;
            globalThis.Headers = moduleExport.Headers;
            globalThis.Request = moduleExport.Request;
            globalThis.Response = moduleExport.Response;
        } else if (moduleName == "node-abort-controller") {
            message += ' and attaching to global';
            globalThis.AbortController = moduleExport.AbortController;
        }

        console.log(message);
    });
    // Must be after loading npm modules.
    config.environmentVariables["IsWebSocketSupported"] = ("WebSocket" in globalThis).toString().toLowerCase();
}

let mono_exit = (code, reason) => {
    console.log(`test-main failed early ${code} ${reason} ${new Error().stack}`);
};

const App = {
    create_function(...args) {
        const code = args.pop();
        const arg_count = args.length;
        args.push("INTERNAL");

        const userFunction = new Function(...args, code);
        return function (...args) {
            args[arg_count] = globalThis.App.runtime.INTERNAL;
            return userFunction(...args);
        };
    },

    invoke_js(js_code) {
        const closedEval = function (Module, INTERNAL, code) {
            return eval(code);
        };
        const res = closedEval(globalThis.App.runtime.Module, globalThis.App.runtime.INTERNAL, js_code);
        return (res === undefined || res === null || typeof res === "string")
            ? null
            : res.toString();
    }
};
globalThis.App = App; // Necessary as tests use it

function configureRuntime(dotnet, runArgs) {
    dotnet
        .withVirtualWorkingDirectory(runArgs.workingDirectory)
        .withEnvironmentVariables(runArgs.environmentVariables)
        .withDiagnosticTracing(runArgs.diagnosticTracing)
        .withExitOnUnhandledError()
        .withExitCodeLogging()
        .withElementOnExit()
        .withInteropCleanupOnExit()
        .withAssertAfterExit()
        .withDumpThreadsOnNonZeroExit()
        .withConfig({
            loadAllSatelliteResources: true
        });

    if (ENVIRONMENT_IS_NODE) {
        dotnet
            .withEnvironmentVariable("NodeJSPlatform", process.platform)
            .withAsyncFlushOnExit();

        const modulesToLoad = runArgs.environmentVariables["NPM_MODULES"];
        if (modulesToLoad) {
            dotnet.withModuleConfig({
                onConfigLoaded: (config, { INTERNAL }) => {
                    loadNodeModules(config, INTERNAL.require, modulesToLoad)
                }
            })
        }
    }

    /*
    dotnet.withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
    dotnet.withEnvironmentVariable("MONO_LOG_MASK", "gc")
    dotnet.withEnvironmentVariable("MONO_GC_DEBUG", "9")
    dotnet.withEnvironmentVariable("DOTNET_DebugWriteToStdErr", "1")
    */

    if (ENVIRONMENT_IS_WEB) {
        if (runArgs.memorySnapshot)
            dotnet.withStartupMemoryCache(true);
        if (runArgs.interpreterPgo)
            dotnet.withInterpreterPgo(true);
        dotnet.withEnvironmentVariable("IsWebSocketSupported", "true");
    }
    if (runArgs.runtimeArgs.length > 0) {
        dotnet.withRuntimeOptions(runArgs.runtimeArgs);
    }
    if (runArgs.debugging) {
        dotnet.withDebugging(-1);
        dotnet.withWaitingForDebugger(-1);
    }
    if (runArgs.forwardConsole) {
        dotnet.withConsoleForwarding();
    }
}

async function dry_run(runArgs) {
    try {
        console.log("Silently starting separate runtime instance as another ES6 module to populate caches...");
        // this separate instance of the ES6 module, in which we just populate the caches
        const { dotnet } = await import('./_framework/dotnet.js?dry_run=true');
        configureRuntime(dotnet, runArgs);
        // silent minimal startup
        await dotnet.withConfig({
            forwardConsoleLogsToWS: false,
            diagnosticTracing: false,
            appendElementOnExit: false,
            logExitCode: false,
            virtualWorkingDirectory: undefined,
            pthreadPoolSize: 0,
            interopCleanupOnExit: false,
            dumpThreadsOnNonZeroExit: false,
            // this just means to not continue startup after the snapshot is taken.
            // If there was previously a matching snapshot, it will be used.
            exitAfterSnapshot: true
        }).create();
        console.log("Separate runtime instance finished loading.");
    } catch (err) {
        if (err && err.status === 0) {
            return true;
        }
        console.log("Separate runtime instance failed loading.", err);
        return false;
    }
    return true;
}

async function run() {
    try {
        const runArgs = await getArgs();
        console.log("Application arguments: " + runArgs.applicationArguments.join(' '));

        if (ENVIRONMENT_IS_WEB && runArgs.memorySnapshot) {
            if (globalThis.isSecureContext) {
                const dryOk = await dry_run(runArgs);
                if (!dryOk) {
                    mono_exit(1, "Failed during dry run");
                    return;
                }
            } else {
                console.log("Skipping dry run as the context is not secure and the snapshot would be not trusted.");
            }
        }

        // this is subsequent run with the actual tests. It will use whatever was cached in the previous run.
        // This way, we are testing that the cached version works.
        mono_exit = exit;

        if (runArgs.applicationArguments.length == 0) {
            mono_exit(1, "Missing required --run argument");
            return;
        }

        configureRuntime(dotnet, runArgs);

        App.runtime = await dotnet.create();
        App.runArgs = runArgs

        console.info("Initializing dotnet version " + App.runtime.runtimeBuildInfo.productVersion + " commit hash " + App.runtime.runtimeBuildInfo.gitHash);

        for (let i = 0; i < runArgs.profilers.length; ++i) {
            const init = App.runtime.Module.cwrap('mono_wasm_load_profiler_' + runArgs.profilers[i], 'void', ['string']);
            init("");
        }


        if (runArgs.applicationArguments[0] == "--regression") {
            const exec_regression = App.runtime.Module.cwrap('mono_wasm_exec_regression', 'number', ['number', 'string']);

            let res = 0;
            try {
                res = exec_regression(10, runArgs.applicationArguments[1]);
                console.log("REGRESSION RESULT: " + res);
            } catch (e) {
                console.error("ABORT: " + e);
                console.error(e.stack);
                res = 1;
            }

            if (res) mono_exit(1, "REGRESSION TEST FAILED");

            return;
        }

        if (runArgs.applicationArguments[0] == "--run") {
            // Run an exe
            if (runArgs.applicationArguments.length == 1) {
                mono_exit(1, "Error: Missing main executable argument.");
                return;
            }
            try {
                const main_assembly_name = runArgs.applicationArguments[1];
                const app_args = runArgs.applicationArguments.slice(2);
                const result = await App.runtime.runMain(main_assembly_name, app_args);
                console.log(`test-main.js exiting ${app_args.length > 1 ? main_assembly_name + " " + app_args[0] : main_assembly_name} with result ${result}`);
                mono_exit(result);
            } catch (error) {
                if (error.name != "ExitStatus") {
                    mono_exit(1, error);
                }
            }
        } else {
            mono_exit(1, "Unhandled argument: " + runArgs.applicationArguments[0]);
        }
    } catch (err) {
        mono_exit(1, err)
    }
}

await run();
