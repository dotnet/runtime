// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnet, exit } from './_framework/dotnet.js'
import { saveProfile } from './profiler.js'

// Read test case from query string
const params = new URLSearchParams(location.search);
const testCase = params.get("test");
if (testCase == null) {
    exit(2, new Error("Missing test scenario. Supply query argument 'test'."));
}

function testOutput(msg) {
    console.log(`TestOutput -> ${msg}`);
}

function countChars(str) {
    const length = str.length;
    return length;
}

// Prepare base runtime parameters
dotnet
    .withElementOnExit()
    .withExitCodeLogging()
    .withExitOnUnhandledError();

const logLevel = params.get("MONO_LOG_LEVEL");
const logMask = params.get("MONO_LOG_MASK");
if (logLevel !== null && logMask !== null) {
    dotnet.withDiagnosticTracing(true); // enable JavaScript tracing
    dotnet.withConfig({
        environmentVariables: {
            "MONO_LOG_LEVEL": logLevel,
            "MONO_LOG_MASK": logMask,
        }
    });
}

// Modify runtime start based on test case
switch (testCase) {
    case "SatelliteAssembliesTest":
        if (params.get("loadAllSatelliteResources") === "true") {
            dotnet.withConfig({ loadAllSatelliteResources: true });
        }
        break;
    case "AppSettingsTest":
        const applicationEnvironment = params.get("applicationEnvironment");
        if (applicationEnvironment) {
            dotnet.withApplicationEnvironment(applicationEnvironment);
        }
        break;
    case "LazyLoadingTest":
        dotnet.withDiagnosticTracing(true);
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
    case "DownloadThenInit":
        const originalFetch = globalThis.fetch;
        globalThis.fetch = (url, fetchArgs) => {
            testOutput("fetching " + url);
            return originalFetch(url, fetchArgs);
        };
        await dotnet.download();
        testOutput("download finished");
        break;
    case "MaxParallelDownloads":
        const maxParallelDownloads = params.get("maxParallelDownloads");
        let activeFetchCount = 0;
        const originalFetch2 = globalThis.fetch;
        globalThis.fetch = async (...args) => {
            activeFetchCount++;
            testOutput(`Fetch started. Active downloads: ${activeFetchCount}`);
            try {
                const response = await originalFetch2(...args);
                activeFetchCount--;
                testOutput(`Fetch completed. Active downloads: ${activeFetchCount}`);
                return response;
            } catch (error) {
                activeFetchCount--;
                testOutput(`Fetch failed. Active downloads: ${activeFetchCount}`);
                throw error;
            }
        };
        dotnet.withConfig({ maxParallelDownloads: maxParallelDownloads });
        break;
    case "AllocateLargeHeapThenInterop":
        dotnet.withEnvironmentVariable("MONO_LOG_LEVEL", "debug")
        dotnet.withEnvironmentVariable("MONO_LOG_MASK", "gc")
        dotnet.withModuleConfig({
            preRun: (Module) => {
                // wasting 2GB of memory
                for (let i = 0; i < 210; i++) {
                    testOutput(`wasting 10m ${Module._malloc(10 * 1024 * 1024)}`);
                }
                testOutput(`WASM ${Module.HEAP32.byteLength} bytes.`);
            }
        })
        break;
    case "LogProfilerTest":
        dotnet.withConfig({
            logProfilerOptions: {
                takeHeapshot: "LogProfilerTest::TakeHeapshot",
                configuration: "log:alloc,output=output.mlpd"
            }
        })
        break;
    case "EnvVariablesTest":
        dotnet.withEnvironmentVariable("foo", "bar");
        break;
    case "BrowserProfilerTest":
        break;
    case "OverrideBootConfigName":
        dotnet.withConfigSrc("boot.json");
        break;
    case "MainWithArgs":
        dotnet.withApplicationArgumentsFromQuery();
        break;
}

const { setModuleImports, Module, getAssemblyExports, getConfig, INTERNAL } = await dotnet.create();
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
const assemblyExtension = Object.keys(config.resources.coreAssembly)[0].endsWith('.wasm') ? ".wasm" : ".dll";

// Run the test case
try {
    switch (testCase) {
        case "SatelliteAssembliesTest":
            await exports.SatelliteAssembliesTest.Run(params.get("loadAllSatelliteResources") !== "true");
            exit(0);
            break;
        case "LazyLoadingTest":
            if (params.get("loadRequiredAssembly") !== "false") {
                let lazyAssemblyExtension = assemblyExtension;
                switch (params.get("lazyLoadingTestExtension")) {
                    case "wasm":
                        lazyAssemblyExtension = ".wasm";
                        break;
                    case "dll":
                        lazyAssemblyExtension = ".dll";
                        break;
                    case "NoExtension":
                        lazyAssemblyExtension = "";
                        break;
                    default:
                        lazyAssemblyExtension = assemblyExtension;
                        break;
                }

                await INTERNAL.loadLazyAssembly(`Json${lazyAssemblyExtension}`);
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
        case "DotnetRun":
        case "MainWithArgs":
            dotnet.run();
            break;
        case "DebugLevelTest":
            testOutput("WasmDebugLevel: " + config.debugLevel);
            exit(42);
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
        case "DownloadThenInit":
        case "MaxParallelDownloads":
            exit(0);
            break;
        case "AllocateLargeHeapThenInterop":
            setModuleImports('main.js', {
                countChars
            });
            exports.MemoryTest.Run();
            exit(0);
            break;
        case "EnvVariablesTest":
            console.log("not ready yet")
            const myExportsEnv = await getAssemblyExports(config.mainAssemblyName);
            const dumpVariables = myExportsEnv.EnvVariablesTest.DumpVariables;
            console.log("ready");

            const retVars = dumpVariables();
            document.getElementById("out").innerHTML = retVars;
            console.debug(`ret: ${retVars}`);

            exit(retVars == 42 ? 0 : 1);
            break;
        case "LogProfilerTest":
            console.log("not ready yet")
            const myExports = await getAssemblyExports(config.mainAssemblyName);
            const testMeaning = myExports.LogProfilerTest.TestMeaning;
            const takeHeapshot = myExports.LogProfilerTest.TakeHeapshot;
            console.log("ready");

            const ret = testMeaning();
            document.getElementById("out").innerHTML = ret;
            console.debug(`ret: ${ret}`);

            takeHeapshot();
            saveProfile(Module);

            let exit_code = ret == 42 ? 0 : 1;
            exit(exit_code);
            break;
        case "BrowserProfilerTest":
            console.log("not ready yet")
            const origMeasure = globalThis.performance.measure
            globalThis.performance.measure = (method, options) => {
                console.log(`performance.measure: ${method}`);
                origMeasure(method, options);
            };
            const myExportsB = await getAssemblyExports(config.mainAssemblyName);
            const testMeaningB = myExportsB.BrowserProfilerTest.TestMeaning;
            console.log("ready");

            const retB = testMeaningB();
            document.getElementById("out").innerHTML = retB;
            console.debug(`ret: ${retB}`);

            exit(retB == 42 ? 0 : 1);

            break;
        case "OverrideBootConfigName":
            testOutput("ConfigSrc: " + Module.configSrc);
            exports.OverrideBootConfigNameTest.Run();
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
