// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './dotnet.js'

let testAbort = true;
let testError = true;
let Module;
function printGcLog(Module) {
    try {
        // List all files in the root directory
        const files = Module.FS.readdir("/");
        // Find files matching gc_log.txt or gc_log.txt.<number>
        const gcLogPattern = /^gc_log\.txt(\.\d+)?$/;
        const gcLogFile = files.find(f => gcLogPattern.test(f));
        if (gcLogFile) {
            const contents = Module.FS.readFile(gcLogFile, { encoding: "utf8" });
            console.log(`=== ${gcLogFile} ===\n${contents}`);
        } else {
            console.log("gc_log.txt (with or without suffix) not found in Emscripten FS.");
        }
    } catch (e) {
        console.error("Error reading gc_log.txt:", e);
    }
}

try {    
    dotnet
        .withElementOnExit()
        // 'withModuleConfig' is internal lower level API 
        // here we show how emscripten could be further configured
        // It is preferred to use specific 'with***' methods instead in all other cases.
        .withConfig({
            startupMemoryCache: true,
            maxParallelDownloads: 1,
            resources: {
                modulesAfterConfigLoaded: {
                    "advanced-sample.lib.module.js": ""
                }
            }
        })
        .withModuleConfig({
            configSrc: "./dotnet.boot.js",
            onConfigLoaded: (config) => {
                // This is called during emscripten `dotnet.wasm` instantiation, after we fetched config.
                console.log('user code Module.onConfigLoaded');
                // config is loaded and could be tweaked before the rest of the runtime startup sequence
                config.environmentVariables["MONO_LOG_LEVEL"] = "debug";
                config.environmentVariables["MONO_LOG_MASK"] = "gc";
                config.environmentVariables["MONO_GC_DEBUG"] = "8:gc_log.txt,print-gchandles,clear-nursery-at-gc";
                // 1 or lower = None, 2 = info, 3  = debug, 4 = verbose, 5 = trace
                config.environmentVariables["MH_LOG_VERBOSITY"] = "3";
            },
            preInit: () => { console.log('user code Module.preInit'); },
            preRun: () => { console.log('user code Module.preRun'); },
            onRuntimeInitialized: () => {
                console.log('user code Module.onRuntimeInitialized');
                // here we could use API passed into this callback
                // Module.FS.chdir("/");
            },
            onDotnetReady: () => {
                // This is called after all assets are loaded.
                console.log('user code Module.onDotnetReady');
            },
            postRun: () => { console.log('user code Module.postRun'); },
            out: (text) => { console.log("ADVANCED:" + text) },
        })
        .withResourceLoader((type, name, defaultUri, integrity, behavior) => {
            // loadBootResource could return string with unqualified name of resource. It assumes that we resolve it with document.baseURI
            return name;
        });

    await dotnet.download();

    const { runtimeBuildInfo, setModuleImports, getAssemblyExports, runMain, getConfig, Module: localModule } = await dotnet.create();
    Module = localModule;

    globalThis.App = {
        runtime: {
            getAssemblyExports,
        }
    };

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);

    document.getElementById("out").innerHTML = `NOT PASSED`;

    await exports.Sample.Test.DoTestMethod();
    document.getElementById("out").innerHTML = `PASSED`;


    console.log('user code Module.onRuntimeInitialized');
    printGcLog(Module);       
    exit(exit_code);
}
catch (err) {
    printGcLog(Module);
    exit(2, err);
}
