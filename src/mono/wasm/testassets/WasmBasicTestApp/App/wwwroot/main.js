// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'

// Read test case from query string
const params = new URLSearchParams(location.search);
const testCase = params.get("test");
if (testCase == null) {
    exit(2, new Error("Missing test scenario. Supply query argument 'test'."));
}

function testOutput(msg) {
    console.log(`TestOutput -> ${msg}`);
}

// Prepare base runtime parameters
dotnet
    .withElementOnExit()
    .withExitCodeLogging()
    .withExitOnUnhandledError();

// Modify runtime start based on test case
switch (testCase) {
    case "AppSettingsTest":
        dotnet.withApplicationEnvironment(params.get("applicationEnvironment"));
        break;
    case "DownloadResourceProgressTest":
        if (params.get("failAssemblyDownload") === "true") {
            let assemblyCounter = 0;
            let failAtAssemblyNumbers = [
                Math.floor(Math.random() * 5),
                Math.floor(Math.random() * 5) + 5,
                Math.floor(Math.random() * 5) + 10
            ];
            console.log(`Failing test at assembly indexes [${failAtAssemblyNumbers.join(", ")}]`);
            let alreadyFailed = [];
            dotnet.withDiagnosticTracing(true).withResourceLoader((type, name, defaultUri, integrity, behavior) => {
                if (type === "dotnetjs") {
                    // loadBootResource could return string with unqualified name of resource. 
                    // It assumes that we resolve it with document.baseURI
                    // we test it here
                    return `_framework/${name}`;
                }
                if (type !== "assembly") {
                    return defaultUri;
                }

                const currentCounter = assemblyCounter++;
                if (!failAtAssemblyNumbers.includes(currentCounter) || alreadyFailed.includes(defaultUri))
                    return defaultUri;

                alreadyFailed.push(defaultUri);
                testOutput("Throw error instead of downloading resource");
                const error = new Error("Simulating a failed fetch");
                error.silent = true;
                throw error;
            });
        }
        dotnet.withModuleConfig({
            onDownloadResourceProgress: (loaded, total) => {
                console.log(`DownloadResourceProgress: ${loaded} / ${total}`);
                if (loaded === total && loaded !== 0) {
                    testOutput("DownloadResourceProgress: Finished");
                }
            }
        });
        break;
    case "OutErrOverrideWorks":
        dotnet.withModuleConfig({
            out: (message) => {
                console.log("Emscripten out override works!");
                console.log(message)
            },
            err: (message) => {
                console.error("Emscripten err override works!");
                console.error(message)
            },
        });
        break;
    case "InterpPgoTest":
        dotnet
            .withConsoleForwarding()
            .withRuntimeOptions(['--interp-pgo-logging'])
            .withInterpreterPgo(true);
        break;
}

const { setModuleImports, getAssemblyExports, getConfig, INTERNAL } = await dotnet.create();
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const assemblyExtension = config.resources.assembly['System.Private.CoreLib.wasm'] !== undefined ? ".wasm" : ".dll";

// Run the test case
try {
    switch (testCase) {
        case "SatelliteAssembliesTest":
            await exports.SatelliteAssembliesTest.Run();
            exit(0);
            break;
        case "LazyLoadingTest":
            if (params.get("loadRequiredAssembly") !== "false") {
                await INTERNAL.loadLazyAssembly(`Json${assemblyExtension}`);
            }
            exports.LazyLoadingTest.Run();
            exit(0);
            break;
        case "LibraryInitializerTest":
            exit(0);
            break;
        case "AppSettingsTest":
            exports.AppSettingsTest.Run();
            exit(0);
            break;
        case "DownloadResourceProgressTest":
            exit(0);
            break;
        case "OutErrOverrideWorks":
            dotnet.run();
            break;
        case "DebugLevelTest":
            testOutput("WasmDebugLevel: " + config.debugLevel);
            exit(0);
            break;
        case "InterpPgoTest":
            setModuleImports('main.js', {
                window: {
                    location: {
                        href: () => globalThis.window.location.href
                    }
                }
            });
            const iterationCount = params.get("iterationCount") ?? 70;
            for (let i = 0; i < iterationCount; i++) { 
                exports.InterpPgoTest.Greeting(); 
            };
            await INTERNAL.interp_pgo_save_data();
            exit(0);
            break;
        default:
            console.error(`Unknown test case: ${testCase}`);
            exit(3);
            break;
    }
} catch (e) {
    exit(1, e);
}
