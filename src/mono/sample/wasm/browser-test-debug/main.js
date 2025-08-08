// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './dotnet.js'

let testAbort = true;
let testError = true;

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

    const { runtimeBuildInfo, setModuleImports, getAssemblyExports, runMain, getConfig, Module } = await dotnet.create();
    
    globalThis.App = {
        runtime: {
            getAssemblyExports,
        }
    };

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);

    await exports.Sample.Test.DoTestMethod();

    let exit_code = await runMain(config.mainAssemblyName, []);
    
    exit(exit_code);
}
catch (err) {
    exit(2, err);
}
