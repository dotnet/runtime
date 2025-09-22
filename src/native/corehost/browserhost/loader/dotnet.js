//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.
//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js


/*! bundlerFriendlyImports */

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
let Module;
let runtimeApi;
let Logger = {};
let Assert = {};
let JSEngine = {};
let loaderExports = {};
let runtimeExports = {};
let hostExports = {};
let interopExports = {};
let nativeBrowserExports = {};
let dotnetInternals;
function getInternals() {
    return dotnetInternals;
}
function setInternals(internal) {
    dotnetInternals = internal;
    runtimeApi = dotnetInternals.runtimeApi;
    Module = dotnetInternals.runtimeApi.Module;
}
function updateAllInternals() {
    if (dotnetInternals.updates === undefined) {
        dotnetInternals.updates = [];
    }
    for (const updateImpl of dotnetInternals.updates) {
        updateImpl();
    }
}
function updateMyInternals() {
    if (Object.keys(loaderExports).length === 0 && dotnetInternals.loaderExportsTable) {
        loaderExports = {};
        Logger = {};
        Assert = {};
        JSEngine = {};
        expandLE(dotnetInternals.loaderExportsTable, Logger, Assert, JSEngine, loaderExports);
    }
    if (Object.keys(runtimeExports).length === 0 && dotnetInternals.runtimeExportsTable) {
        runtimeExports = {};
        expandRE(dotnetInternals.runtimeExportsTable, runtimeExports);
    }
    if (Object.keys(hostExports).length === 0 && dotnetInternals.hostNativeExportsTable) {
        hostExports = {};
        expandHE(dotnetInternals.hostNativeExportsTable, hostExports);
    }
    if (Object.keys(interopExports).length === 0 && dotnetInternals.interopJavaScriptNativeExportsTable) {
        interopExports = {};
        expandJSNE(dotnetInternals.interopJavaScriptNativeExportsTable, interopExports);
    }
    if (Object.keys(nativeBrowserExports).length === 0 && dotnetInternals.nativeBrowserExportsTable) {
        nativeBrowserExports = {};
        expandNBE(dotnetInternals.nativeBrowserExportsTable, nativeBrowserExports);
    }
}
/**
 * Functions below allow our JS modules to exchange internal interfaces by passing tables of functions in known order instead of using string symbols.
 * IMPORTANT: If you need to add more functions, make sure that you add them at the end of the table, so that the order of existing functions does not change.
 */
function tabulateLE(logger, assert, loaderExports) {
    return [
        logger.info,
        logger.warn,
        logger.error,
        assert.check,
        loaderExports.ENVIRONMENT_IS_NODE,
        loaderExports.ENVIRONMENT_IS_SHELL,
        loaderExports.ENVIRONMENT_IS_WEB,
        loaderExports.ENVIRONMENT_IS_WORKER,
        loaderExports.ENVIRONMENT_IS_SIDECAR,
        loaderExports.browserHostResolveMain,
        loaderExports.browserHostRejectMain,
        loaderExports.getRunMainPromise,
    ];
}
function expandLE(table, logger, assert, jsEngine, loaderExports) {
    const loggerLocal = {
        info: table[0],
        warn: table[1],
        error: table[2],
    };
    const assertLocal = {
        check: table[3],
    };
    const loaderExportsLocal = {
        ENVIRONMENT_IS_NODE: table[4],
        ENVIRONMENT_IS_SHELL: table[5],
        ENVIRONMENT_IS_WEB: table[6],
        ENVIRONMENT_IS_WORKER: table[7],
        ENVIRONMENT_IS_SIDECAR: table[8],
        browserHostResolveMain: table[9],
        browserHostRejectMain: table[10],
        getRunMainPromise: table[11],
    };
    const jsEngineLocal = {
        IS_NODE: loaderExportsLocal.ENVIRONMENT_IS_NODE(),
        IS_SHELL: loaderExportsLocal.ENVIRONMENT_IS_SHELL(),
        IS_WEB: loaderExportsLocal.ENVIRONMENT_IS_WEB(),
        IS_WORKER: loaderExportsLocal.ENVIRONMENT_IS_WORKER(),
        IS_SIDECAR: loaderExportsLocal.ENVIRONMENT_IS_SIDECAR(),
    };
    Object.assign(loaderExports, loaderExportsLocal);
    Object.assign(logger, loggerLocal);
    Object.assign(assert, assertLocal);
    Object.assign(jsEngine, jsEngineLocal);
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function tabulateRE(map) {
    return [];
}
function expandRE(table, runtime) {
    Object.assign(runtime, {});
}
function tabulateHE(map) {
    return [
        map.registerDllBytes,
        map.isSharedArrayBuffer,
    ];
}
function expandHE(table, native) {
    const nativeLocal = {
        registerDllBytes: table[0],
        isSharedArrayBuffer: table[1],
    };
    Object.assign(native, nativeLocal);
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function tabulateJSNE(map) {
    return [];
}
function expandJSNE(table, interop) {
    const interopLocal = {};
    Object.assign(interop, interopLocal);
}
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function tabulateNBE(map) {
    return [];
}
function expandNBE(table, interop) {
    const interopLocal = {};
    Object.assign(interop, interopLocal);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
const config = {};
let isConfigDownloaded = false;
async function downloadConfig(url, loadBootResource) {
    if (loadBootResource)
        throw new Error("TODO: loadBootResource is not implemented yet");
    if (isConfigDownloaded)
        return; // only download config once
    if (!url) {
        url = "./dotnet.boot.js";
    }
    // url ends with .json
    if (url.endsWith(".json")) {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Failed to download config from ${url}: ${response.status} ${response.statusText}`);
        }
        const newConfig = await response.json();
        mergeConfig(newConfig);
    }
    else if (url.endsWith(".js") || url.endsWith(".mjs")) {
        const module = await import(/* webpackIgnore: true */ url);
        mergeConfig(module.config);
    }
    isConfigDownloaded = true;
}
function getConfig() {
    return config;
}
function mergeConfig(source) {
    normalizeConfig(config);
    normalizeConfig(source);
    mergeConfigs(config, source);
}
function mergeConfigs(target, source) {
    // no need to merge the same object
    if (target === source || source === undefined || source === null)
        return target;
    mergeResources(target.resources, source.resources);
    source.appendElementOnExit = source.appendElementOnExit !== undefined ? source.appendElementOnExit : target.appendElementOnExit;
    source.logExitCode = source.logExitCode !== undefined ? source.logExitCode : target.logExitCode;
    source.exitOnUnhandledError = source.exitOnUnhandledError !== undefined ? source.exitOnUnhandledError : target.exitOnUnhandledError;
    source.loadAllSatelliteResources = source.loadAllSatelliteResources !== undefined ? source.loadAllSatelliteResources : target.loadAllSatelliteResources;
    source.mainAssemblyName = source.mainAssemblyName !== undefined ? source.mainAssemblyName : target.mainAssemblyName;
    source.virtualWorkingDirectory = source.virtualWorkingDirectory !== undefined ? source.virtualWorkingDirectory : target.virtualWorkingDirectory;
    source.debugLevel = source.debugLevel !== undefined ? source.debugLevel : target.debugLevel;
    source.diagnosticTracing = source.diagnosticTracing !== undefined ? source.diagnosticTracing : target.diagnosticTracing;
    source.environmentVariables = { ...target.environmentVariables, ...source.environmentVariables };
    source.runtimeOptions = [...target.runtimeOptions, ...source.runtimeOptions];
    Object.assign(target, source);
    if (target.resources.coreAssembly.length) {
        isConfigDownloaded = true;
    }
    return target;
}
function mergeResources(target, source) {
    // no need to merge the same object
    if (target === source || source === undefined || source === null)
        return target;
    source.coreAssembly = [...target.coreAssembly, ...source.coreAssembly];
    source.assembly = [...target.assembly, ...source.assembly];
    source.lazyAssembly = [...target.lazyAssembly, ...source.lazyAssembly];
    source.corePdb = [...target.corePdb, ...source.corePdb];
    source.pdb = [...target.pdb, ...source.pdb];
    source.jsModuleWorker = [...target.jsModuleWorker, ...source.jsModuleWorker];
    source.jsModuleNative = [...target.jsModuleNative, ...source.jsModuleNative];
    source.jsModuleDiagnostics = [...target.jsModuleDiagnostics, ...source.jsModuleDiagnostics];
    source.jsModuleRuntime = [...target.jsModuleRuntime, ...source.jsModuleRuntime];
    source.wasmSymbols = [...target.wasmSymbols, ...source.wasmSymbols];
    source.wasmNative = [...target.wasmNative, ...source.wasmNative];
    source.icu = [...target.icu, ...source.icu];
    source.vfs = [...target.vfs, ...source.vfs];
    source.modulesAfterConfigLoaded = [...target.modulesAfterConfigLoaded, ...source.modulesAfterConfigLoaded];
    source.modulesAfterRuntimeReady = [...target.modulesAfterRuntimeReady, ...source.modulesAfterRuntimeReady];
    source.extensions = { ...target.extensions, ...source.extensions };
    for (const key in source.satelliteResources) {
        source.satelliteResources[key] = [...target.satelliteResources[key] || [], ...source.satelliteResources[key] || []];
    }
    return Object.assign(target, source);
}
function normalizeConfig(target) {
    if (!target.resources)
        target.resources = {};
    normalizeResources(target.resources);
    if (!target.environmentVariables)
        target.environmentVariables = {};
    if (!target.runtimeOptions)
        target.runtimeOptions = [];
    if (target.appendElementOnExit === undefined)
        target.appendElementOnExit = false;
    if (target.logExitCode === undefined)
        target.logExitCode = false;
    if (target.exitOnUnhandledError === undefined)
        target.exitOnUnhandledError = false;
    if (target.loadAllSatelliteResources === undefined)
        target.loadAllSatelliteResources = false;
    if (target.debugLevel === undefined)
        target.debugLevel = 0;
    if (target.diagnosticTracing === undefined)
        target.diagnosticTracing = false;
    if (target.virtualWorkingDirectory === undefined)
        target.virtualWorkingDirectory = "/";
    if (target.mainAssemblyName === undefined)
        target.mainAssemblyName = "HelloWorld.dll";
}
function normalizeResources(target) {
    if (!target.coreAssembly)
        target.coreAssembly = [];
    if (!target.assembly)
        target.assembly = [];
    if (!target.lazyAssembly)
        target.lazyAssembly = [];
    if (!target.corePdb)
        target.corePdb = [];
    if (!target.pdb)
        target.pdb = [];
    if (!target.jsModuleWorker)
        target.jsModuleWorker = [];
    if (!target.jsModuleNative)
        target.jsModuleNative = [];
    if (!target.jsModuleDiagnostics)
        target.jsModuleDiagnostics = [];
    if (!target.jsModuleRuntime)
        target.jsModuleRuntime = [];
    if (!target.wasmSymbols)
        target.wasmSymbols = [];
    if (!target.wasmNative)
        target.wasmNative = [];
    if (!target.icu)
        target.icu = [];
    if (!target.modulesAfterConfigLoaded)
        target.modulesAfterConfigLoaded = [];
    if (!target.modulesAfterRuntimeReady)
        target.modulesAfterRuntimeReady = [];
    if (!target.satelliteResources)
        target.satelliteResources = {};
    if (!target.extensions)
        target.extensions = {};
    if (!target.vfs)
        target.vfs = [];
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// eslint-disable-next-line @typescript-eslint/no-unused-vars
function exit(exit_code, reason) {
    const reasonStr = reason ? (reason.stack ? reason.stack || reason.message : reason.toString()) : "";
    if (exit_code !== 0) {
        Logger.error(`Exit with code ${exit_code} ${reason ? "and reason: " + reasonStr : ""}`);
    }
    if (JSEngine.IS_NODE) {
        globalThis.process.exit(exit_code);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/// a unique symbol used to mark a promise as controllable
const promise_control_symbol = Symbol.for("wasm promise_control");
/// Creates a new promise together with a controller that can be used to resolve or reject that promise.
/// Optionally takes callbacks to be called immediately after a promise is resolved or rejected.
function createPromiseController(afterResolve, afterReject) {
    let promiseControl = null;
    const promise = new Promise((resolve, reject) => {
        promiseControl = {
            isDone: false,
            promise: null,
            resolve: (data) => {
                if (!promiseControl.isDone) {
                    promiseControl.isDone = true;
                    resolve(data);
                    if (afterResolve) {
                        afterResolve();
                    }
                }
            },
            reject: (reason) => {
                if (!promiseControl.isDone) {
                    promiseControl.isDone = true;
                    reject(reason);
                    if (afterReject) {
                        afterReject();
                    }
                }
            },
            propagateFrom: (other) => {
                other.then(promiseControl.resolve).catch(promiseControl.reject);
            }
        };
    });
    promiseControl.promise = promise;
    const controllablePromise = promise;
    controllablePromise[promise_control_symbol] = promiseControl;
    return promiseControl;
}
function getPromiseController(promise) {
    return promise[promise_control_symbol];
}
function isControllablePromise(promise) {
    return promise[promise_control_symbol] !== undefined;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
let CoreCLRInitialized = false;
const runMainPromiseController = createPromiseController();
function browserHostInitializeCoreCLR() {
    if (CoreCLRInitialized) {
        return;
    }
    // int browserHostInitializeCoreCLR(void)
    const res = Module.ccall("browserHostInitializeCoreCLR", "number");
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseController.reject(reason);
        exit(res, reason);
    }
    CoreCLRInitialized = true;
}
function browserHostResolveMain(exitCode) {
    runMainPromiseController.resolve(exitCode);
}
function browserHostRejectMain(reason) {
    runMainPromiseController.reject(reason);
}
function getRunMainPromise() {
    return runMainPromiseController.promise;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
const scriptUrlQuery = /*! webpackIgnore: true */ import.meta.url;
const queryIndex = scriptUrlQuery.indexOf("?");
const modulesUniqueQuery = queryIndex > 0 ? scriptUrlQuery.substring(queryIndex) : "";
const scriptUrl = normalizeFileUrl(scriptUrlQuery);
const scriptDirectory = normalizeDirectoryUrl(scriptUrl);
const nativeModulePromiseController = createPromiseController(() => {
    updateAllInternals();
});
// WASM-TODO: retry logic
// WASM-TODO: throttling logic
// WASM-TODO: load icu data
// WASM-TODO: invokeLibraryInitializers
// WASM-TODO: webCIL
async function createRuntime(downloadOnly, loadBootResource) {
    const config = getConfig();
    if (!config.resources || !config.resources.coreAssembly || !config.resources.coreAssembly.length)
        throw new Error("Invalid config, resources is not set");
    const coreAssembliesPromise = Promise.all(config.resources.coreAssembly.map(fetchDll));
    const assembliesPromise = Promise.all(config.resources.assembly.map(fetchDll));
    const runtimeModulePromise = loadModule(config.resources.jsModuleRuntime[0], loadBootResource);
    const nativeModulePromise = loadModule(config.resources.jsModuleNative[0], loadBootResource);
    const nativeModule = await nativeModulePromise;
    const modulePromise = nativeModule.initialize(getInternals());
    nativeModulePromiseController.propagateFrom(modulePromise);
    const runtimeModule = await runtimeModulePromise;
    const runtimeModuleReady = runtimeModule.initialize(getInternals());
    await nativeModulePromiseController.promise;
    await coreAssembliesPromise;
    if (!downloadOnly) {
        browserHostInitializeCoreCLR();
    }
    await assembliesPromise;
    await runtimeModuleReady;
}
async function loadModule(asset, loadBootResource) {
    if (loadBootResource)
        throw new Error("TODO: loadBootResource is not implemented yet");
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    if (!asset.resolvedUrl)
        throw new Error("Invalid config, resources is not set");
    return await import(/* webpackIgnore: true */ asset.resolvedUrl);
}
function fetchWasm(asset) {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    return fetchBytes(asset);
}
async function fetchDll(asset) {
    if (asset.name && !asset.resolvedUrl) {
        asset.resolvedUrl = locateFile(asset.name);
    }
    const bytes = await fetchBytes(asset);
    await nativeModulePromiseController.promise;
    hostExports.registerDllBytes(bytes, asset);
}
async function fetchBytes(asset) {
    Assert.check(asset && asset.resolvedUrl, "Bad asset.resolvedUrl");
    if (JSEngine.IS_NODE) {
        const { promises: fs } = await import('fs');
        const { fileURLToPath } = await import(/*! webpackIgnore: true */ 'url');
        const isFileUrl = asset.resolvedUrl.startsWith("file://");
        if (isFileUrl) {
            asset.resolvedUrl = fileURLToPath(asset.resolvedUrl);
        }
        const buffer = await fs.readFile(asset.resolvedUrl);
        return new Uint8Array(buffer);
    }
    else {
        const response = await fetch(asset.resolvedUrl);
        if (!response.ok) {
            throw new Error(`Failed to load ${asset.resolvedUrl} with ${response.status} ${response.statusText}`);
        }
        const buffer = await response.arrayBuffer();
        return new Uint8Array(buffer);
    }
}
function locateFile(path) {
    if ("URL" in globalThis) {
        return new URL(path, scriptDirectory).toString();
    }
    if (isPathAbsolute(path))
        return path;
    return scriptDirectory + path + modulesUniqueQuery;
}
function normalizeFileUrl(filename) {
    // unix vs windows
    // remove query string
    return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
}
function normalizeDirectoryUrl(dir) {
    return dir.slice(0, dir.lastIndexOf("/")) + "/";
}
const protocolRx = /^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//;
const windowsAbsoluteRx = /[a-zA-Z]:[\\/]/;
function isPathAbsolute(path) {
    if (JSEngine.IS_NODE || JSEngine.IS_SHELL) {
        // unix /x.json
        // windows \x.json
        // windows C:\x.json
        // windows C:/x.json
        return path.startsWith("/") || path.startsWith("\\") || path.indexOf("///") !== -1 || windowsAbsoluteRx.test(path);
    }
    // anything with protocol is always absolute
    // windows file:///C:/x.json
    // windows http://C:/x.json
    return protocolRx.test(path);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
let configUrl = undefined;
let applicationArguments = [];
let loadBootResourceCallback = undefined;
/* eslint-disable @typescript-eslint/no-unused-vars */
class HostBuilder {
    withConfig(config) {
        mergeConfig(config);
        return this;
    }
    withConfigSrc(configSrc) {
        configUrl = configSrc;
        return this;
    }
    withApplicationArguments(...args) {
        applicationArguments = args;
        return this;
    }
    withEnvironmentVariable(name, value) {
        mergeConfig({
            environmentVariables: {
                [name]: value
            }
        });
        return this;
    }
    withEnvironmentVariables(variables) {
        mergeConfig({
            environmentVariables: variables
        });
        return this;
    }
    withVirtualWorkingDirectory(vfsPath) {
        mergeConfig({
            virtualWorkingDirectory: vfsPath
        });
        return this;
    }
    withDiagnosticTracing(enabled) {
        mergeConfig({
            diagnosticTracing: enabled
        });
        return this;
    }
    withDebugging(level) {
        mergeConfig({
            debugLevel: level
        });
        return this;
    }
    withMainAssembly(mainAssemblyName) {
        mergeConfig({
            mainAssemblyName: mainAssemblyName
        });
        return this;
    }
    withApplicationArgumentsFromQuery() {
        if (!globalThis.window) {
            throw new Error("Missing window to the query parameters from");
        }
        if (typeof globalThis.URLSearchParams == "undefined") {
            throw new Error("URLSearchParams is supported");
        }
        const params = new URLSearchParams(globalThis.window.location.search);
        const values = params.getAll("arg");
        return this.withApplicationArguments(...values);
    }
    withApplicationEnvironment(applicationEnvironment) {
        mergeConfig({
            applicationEnvironment: applicationEnvironment
        });
        return this;
    }
    withApplicationCulture(applicationCulture) {
        mergeConfig({
            applicationCulture: applicationCulture
        });
        return this;
    }
    withResourceLoader(loadBootResource) {
        loadBootResourceCallback = loadBootResource;
        return this;
    }
    // internal
    withModuleConfig(moduleConfig) {
        Object.assign(Module, moduleConfig);
        return this;
    }
    // internal
    withConsoleForwarding() {
        // TODO
        return this;
    }
    // internal
    withExitOnUnhandledError() {
        // TODO
        return this;
    }
    // internal
    withAsyncFlushOnExit() {
        // TODO
        return this;
    }
    // internal
    withExitCodeLogging() {
        // TODO
        return this;
    }
    // internal
    withElementOnExit() {
        // TODO
        return this;
    }
    // internal
    withInteropCleanupOnExit() {
        // TODO
        return this;
    }
    async download() {
        try {
            await downloadConfig(configUrl, loadBootResourceCallback);
            return createRuntime(true, loadBootResourceCallback);
        }
        catch (err) {
            exit(1, err);
            throw err;
        }
    }
    async create() {
        try {
            await downloadConfig(configUrl, loadBootResourceCallback);
            await createRuntime(false, loadBootResourceCallback);
            this.runtimeApi = runtimeApi;
            return this.runtimeApi;
        }
        catch (err) {
            exit(1, err);
            throw err;
        }
    }
    async run() {
        try {
            if (!this.runtimeApi) {
                await this.create();
            }
            const config = getConfig();
            return this.runtimeApi.runMainAndExit(config.mainAssemblyName, applicationArguments);
        }
        catch (err) {
            exit(1, err);
            throw err;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
function initPolyfills() {
    if (typeof globalThis.WeakRef !== "function") {
        class WeakRefPolyfill {
            constructor(value) {
                this._value = value;
            }
            deref() {
                return this._value;
            }
        }
        globalThis.WeakRef = WeakRefPolyfill;
    }
    if (typeof globalThis.fetch !== "function") {
        globalThis.fetch = fetchLike;
    }
}
async function initPolyfillsAsync() {
    if (JSEngine.IS_NODE) {
        if (!globalThis.crypto) {
            globalThis.crypto = {};
        }
        if (!globalThis.crypto.getRandomValues) {
            let nodeCrypto = undefined;
            try {
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                // @ts-ignore:
                nodeCrypto = await import(/*! webpackIgnore: true */ 'node:crypto');
            }
            catch (err) {
                // Noop, error throwing polyfill provided bellow
            }
            if (!nodeCrypto) {
                globalThis.crypto.getRandomValues = () => {
                    throw new Error("Using node without crypto support. To enable current operation, either provide polyfill for 'globalThis.crypto.getRandomValues' or enable 'node:crypto' module.");
                };
            }
            else if (nodeCrypto.webcrypto) {
                globalThis.crypto = nodeCrypto.webcrypto;
            }
            else if (nodeCrypto.randomBytes) {
                const getRandomValues = (buffer) => {
                    if (buffer) {
                        buffer.set(nodeCrypto.randomBytes(buffer.length));
                    }
                };
                globalThis.crypto.getRandomValues = getRandomValues;
            }
        }
        if (!globalThis.performance) {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore:
            globalThis.performance = (await import(/*! webpackIgnore: true */ 'perf_hooks')).performance;
        }
    }
}
async function fetchLike(url, init) {
    let node_fs = undefined;
    let node_url = undefined;
    try {
        // this need to be detected only after we import node modules in onConfigLoaded
        const hasFetch = typeof (globalThis.fetch) === "function";
        if (JSEngine.IS_NODE) {
            const isFileUrl = url.startsWith("file://");
            if (!isFileUrl && hasFetch) {
                return globalThis.fetch(url, init || { credentials: "same-origin" });
            }
            if (!node_fs) {
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                // @ts-ignore:
                node_url = await import(/*! webpackIgnore: true */ 'url');
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                // @ts-ignore:
                node_fs = await import(/*! webpackIgnore: true */ 'fs');
            }
            if (isFileUrl) {
                url = node_url.fileURLToPath(url);
            }
            const arrayBuffer = await node_fs.promises.readFile(url);
            return {
                ok: true,
                headers: {
                    length: 0,
                    get: () => null
                },
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(arrayBuffer),
                text: () => {
                    throw new Error("NotImplementedException");
                }
            };
        }
        else if (hasFetch) {
            return globalThis.fetch(url, init || { credentials: "same-origin" });
        }
        else if (typeof (read) === "function") {
            // note that it can't open files with unicode names, like Stra<unicode char - Latin Small Letter Sharp S>e.xml
            // https://bugs.chromium.org/p/v8/issues/detail?id=12541
            return {
                ok: true,
                url,
                headers: {
                    length: 0,
                    get: () => null
                },
                arrayBuffer: () => {
                    return new Uint8Array(read(url, "binary"));
                },
                json: () => {
                    return JSON.parse(read(url, "utf8"));
                },
                text: () => read(url, "utf8")
            };
        }
    }
    catch (e) {
        return {
            ok: false,
            url,
            status: 500,
            headers: {
                length: 0,
                get: () => null
            },
            statusText: "ERR28: " + e,
            arrayBuffer: () => {
                throw e;
            },
            json: () => {
                throw e;
            },
            text: () => {
                throw e;
            }
        };
    }
    throw new Error("No fetch implementation available");
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
let runtimeList;
class RuntimeList {
    constructor() {
        this.list = {};
    }
    registerRuntime(api) {
        if (api.runtimeId === undefined) {
            api.runtimeId = Object.keys(this.list).length;
        }
        this.list[api.runtimeId] = new globalThis.WeakRef(api);
        return api.runtimeId;
    }
    getRuntime(runtimeId) {
        const wr = this.list[runtimeId];
        return wr ? wr.deref() : undefined;
    }
}
function registerRuntime(api) {
    const globalThisAny = globalThis;
    // this code makes it possible to find dotnet runtime on a page via global namespace, even when there are multiple runtimes at the same time
    if (!globalThisAny.getDotnetRuntime) {
        globalThisAny.getDotnetRuntime = (runtimeId) => globalThisAny.getDotnetRuntime.__list.getRuntime(runtimeId);
        globalThisAny.getDotnetRuntime.__list = runtimeList = new RuntimeList();
    }
    else {
        runtimeList = globalThisAny.getDotnetRuntime.__list;
    }
    return runtimeList.registerRuntime(api);
}

var ProductVersion = "10.0.0-dev";

var BuildConfiguration = "Debug";

var GitHash = "3f5c7d9c448b99bdeae770df46ffc3c923f7ccf4";

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// eslint-disable-next-line @typescript-eslint/no-unused-vars
async function invokeLibraryInitializers(functionName, args) {
    throw new Error("Not implemented");
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// WASMTODO inline the code
function check(condition, messageFactory) {
    if (!condition) {
        const message = typeof messageFactory === "string" ? messageFactory : messageFactory();
        throw new Error(`Assert failed: ${message}`);
    }
}
/* eslint-disable no-console */
const prefix = "CLR_WASM: ";
function info(msg, ...data) {
    console.info(prefix + msg, ...data);
}
function warn(msg, ...data) {
    console.warn(prefix + msg, ...data);
}
function error(msg, ...data) {
    if (data && data.length > 0 && data[0] && typeof data[0] === "object") {
        // don't log silent errors
        if (data[0].silent) {
            return;
        }
        if (data[0].toString) {
            console.error(prefix + msg, data[0].toString());
            return;
        }
    }
    console.error(prefix + msg, ...data);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
function initialize() {
    const ENVIRONMENT_IS_NODE = () => typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
    const ENVIRONMENT_IS_WEB_WORKER = () => typeof importScripts == "function";
    const ENVIRONMENT_IS_SIDECAR = () => ENVIRONMENT_IS_WEB_WORKER() && typeof dotnetSidecar !== "undefined"; // sidecar is emscripten main running in a web worker
    const ENVIRONMENT_IS_WORKER = () => ENVIRONMENT_IS_WEB_WORKER() && !ENVIRONMENT_IS_SIDECAR(); // we redefine what ENVIRONMENT_IS_WORKER, we replace it in emscripten internals, so that sidecar works
    const ENVIRONMENT_IS_WEB = () => typeof window == "object" || (ENVIRONMENT_IS_WEB_WORKER() && !ENVIRONMENT_IS_NODE());
    const ENVIRONMENT_IS_SHELL = () => !ENVIRONMENT_IS_WEB() && !ENVIRONMENT_IS_NODE();
    const runtimeApi = {
        INTERNAL: {},
        Module: {},
        runtimeId: -1,
        runtimeBuildInfo: {
            productVersion: ProductVersion,
            gitHash: GitHash,
            buildConfiguration: BuildConfiguration,
            wasmEnableThreads: false,
            wasmEnableSIMD: true,
            wasmEnableExceptionHandling: true,
        },
    };
    const updates = [];
    setInternals({
        config: config,
        runtimeApi: runtimeApi,
        updates,
    });
    const runtimeApiFunctions = {
        getConfig,
        exit,
        invokeLibraryInitializers,
    };
    const loaderFunctions = {
        ENVIRONMENT_IS_NODE,
        ENVIRONMENT_IS_SHELL,
        ENVIRONMENT_IS_WEB,
        ENVIRONMENT_IS_WORKER,
        ENVIRONMENT_IS_SIDECAR,
        getRunMainPromise,
        browserHostRejectMain,
        browserHostResolveMain,
    };
    const jsEngine = {
        IS_NODE: ENVIRONMENT_IS_NODE(),
        IS_SHELL: ENVIRONMENT_IS_SHELL(),
        IS_WEB: ENVIRONMENT_IS_WEB(),
        IS_WORKER: ENVIRONMENT_IS_WORKER(),
        IS_SIDECAR: ENVIRONMENT_IS_SIDECAR(),
    };
    const logger = {
        info,
        warn,
        error,
    };
    const assert = {
        check,
    };
    Object.assign(runtimeApi, runtimeApiFunctions);
    Object.assign(Logger, logger);
    Object.assign(Assert, assert);
    Object.assign(JSEngine, jsEngine);
    Object.assign(loaderExports, loaderFunctions);
    dotnetInternals.loaderExportsTable = [...tabulateLE(Logger, Assert, loaderExports)];
    updates.push(updateMyInternals);
    updateAllInternals();
    return runtimeApi;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
initPolyfills();
registerRuntime(initialize());
await initPolyfillsAsync();
const dotnet = new HostBuilder();

export { dotnet, exit };
