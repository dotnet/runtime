// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './dotnet.js'

function add(a, b) {
    return a + b;
}

let testAbort = true;
let testError = true;

try {
    const { runtimeBuildInfo, setModuleImports, getAssemblyExports, runMain, getConfig, Module } = await dotnet
        .withElementOnExit()
        // 'withModuleConfig' is internal lower level API 
        // here we show how emscripten could be further configured
        // It is prefered to use specific 'with***' methods instead in all other cases.
        .withModuleConfig({
            configSrc: "./mono-config.json",
            onConfigLoaded: (config) => {
                // This is called during emscripten `dotnet.wasm` instantiation, after we fetched config.
                console.log('user code Module.onConfigLoaded');
                // config is loaded and could be tweaked before the rest of the runtime startup sequence
                config.environmentVariables["MONO_LOG_LEVEL"] = "debug";
                config.browserProfilerOptions = {};
            },
            imports: {
                fetch: (url, fetchArgs) => {
                    console.log("fetching " + url);
                    // we are testing that we can retry loading of the assembly
                    if (testAbort && url.indexOf('System.Private.CoreLib') != -1) {
                        testAbort = false;
                        return fetch(url + "?testAbort=true", fetchArgs);
                    }
                    if (testError && url.indexOf('System.Console') != -1) {
                        testError = false;
                        return fetch(url + "?testError=true", fetchArgs);
                    }
                    return fetch(url, fetchArgs);
                }
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
        })
        .create();

    // at this point both emscripten and monoVM are fully initialized.
    console.log('user code after dotnet.create');
    setModuleImports("main.js", {
        Sample: {
            Test: {
                add
            }
        }
    });

    const config = getConfig();
    const exports = await getAssemblyExports(config.mainAssemblyName);
    const meaning = exports.Sample.Test.TestMeaning();
    if (typeof Module.GL !== "object") {
        exit(-10, "Can't find GL");
    }
    if (typeof Module.FS.filesystems.IDBFS !== "object") {
        exit(-10, "Can't find FS.filesystems.IDBFS");
    }
    console.debug(`meaning: ${meaning}`);
    if (!exports.Sample.Test.IsPrime(meaning)) {
        document.getElementById("out").innerHTML = `${meaning} as computed on dotnet ver ${runtimeBuildInfo.productVersion}`;
    }

    let exit_code = await runMain(config.mainAssemblyName, []);
    exit(exit_code);
}
catch (err) {
    exit(2, err);
}