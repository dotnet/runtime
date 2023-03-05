// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import MonoWasmThreads from "consts:monoWasmThreads";
import WasmEnableLegacyJsInterop from "consts:WasmEnableLegacyJsInterop";
import { CharPtrNull, DotnetModule, RuntimeAPI, MonoConfig, MonoConfigInternal } from "./types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, INTERNAL, Module, runtimeHelpers } from "./imports";
import cwraps, { init_c_exports } from "./cwraps";
import { mono_wasm_raise_debug_event, mono_wasm_runtime_ready } from "./debug";
import { mono_wasm_globalization_init } from "./icu";
import { toBase64StringImpl } from "./base64";
import { mono_wasm_init_aot_profiler, mono_wasm_init_browser_profiler } from "./profiler";
import { mono_on_abort, mono_exit } from "./run";
import { initialize_marshalers_to_cs } from "./marshal-to-cs";
import { initialize_marshalers_to_js } from "./marshal-to-js";
import { init_polyfills_async } from "./polyfills";
import * as pthreads_worker from "./pthreads/worker";
import { createPromiseController } from "./promise-controller";
import { string_decoder } from "./strings";
import { init_managed_exports } from "./managed-exports";
import { cwraps_internal } from "./exports-internal";
import { CharPtr, InstantiateWasmCallBack, InstantiateWasmSuccessCallback } from "./types/emscripten";
import { instantiate_wasm_asset, mono_download_assets, resolve_asset_path, start_asset_download, wait_for_all_assets } from "./assets";
import { readSymbolMapFile } from "./logging";
import { mono_wasm_init_diagnostics } from "./diagnostics";
import { preAllocatePThreadWorkerPool, instantiateWasmPThreadWorkerPool } from "./pthreads/browser";
import { export_linker } from "./exports-linker";
import { endMeasure, MeasuredBlock, startMeasure } from "./profiler";

// legacy
import { init_legacy_exports } from "./net6-legacy/corebindings";
import { cwraps_binding_api, cwraps_mono_api } from "./net6-legacy/exports-legacy";
import { BINDING, MONO } from "./net6-legacy/imports";

let config: MonoConfigInternal = undefined as any;
let configLoaded = false;
export const dotnetReady = createPromiseController<any>();
export const afterConfigLoaded = createPromiseController<MonoConfig>();
export const afterInstantiateWasm = createPromiseController<void>();
export const beforePreInit = createPromiseController<void>();
export const afterPreInit = createPromiseController<void>();
export const afterPreRun = createPromiseController<void>();
export const beforeOnRuntimeInitialized = createPromiseController<void>();
export const afterOnRuntimeInitialized = createPromiseController<void>();
export const afterPostRun = createPromiseController<void>();

// default size if MonoConfig.pthreadPoolSize is undefined
const MONO_PTHREAD_POOL_SIZE = 4;

// we are making emscripten startup async friendly
// emscripten is executing the events without awaiting it and so we need to block progress via PromiseControllers above
export function configure_emscripten_startup(module: DotnetModule, exportedAPI: RuntimeAPI): void {
    const mark = startMeasure();
    // these all could be overridden on DotnetModuleConfig, we are chaing them to async below, as opposed to emscripten
    // when user set configSrc or config, we are running our default startup sequence.
    const userInstantiateWasm: undefined | ((imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback) => any) = module.instantiateWasm;
    const userPreInit: (() => void)[] = !module.preInit ? [] : typeof module.preInit === "function" ? [module.preInit] : module.preInit;
    const userPreRun: (() => void)[] = !module.preRun ? [] : typeof module.preRun === "function" ? [module.preRun] : module.preRun as any;
    const userpostRun: (() => void)[] = !module.postRun ? [] : typeof module.postRun === "function" ? [module.postRun] : module.postRun as any;
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    const userOnRuntimeInitialized: () => void = module.onRuntimeInitialized ? module.onRuntimeInitialized : () => { };

    // execution order == [0] ==
    // - default or user Module.instantiateWasm (will start downloading dotnet.wasm)
    module.instantiateWasm = (imports, callback) => instantiateWasm(imports, callback, userInstantiateWasm);
    // execution order == [1] ==
    module.preInit = [() => preInit(userPreInit)];
    // execution order == [2] ==
    module.preRun = [() => preRunAsync(userPreRun)];
    // execution order == [4] ==
    module.onRuntimeInitialized = () => onRuntimeInitializedAsync(userOnRuntimeInitialized);
    // execution order == [5] ==
    module.postRun = [() => postRunAsync(userpostRun)];
    // execution order == [6] ==

    module.ready.then(async () => {
        // wait for previous stage
        await afterPostRun.promise;
        // startup end
        endMeasure(mark, MeasuredBlock.emscriptenStartup);
        // - here we resolve the promise returned by createDotnetRuntime export
        // - any code after createDotnetRuntime is executed now
        dotnetReady.promise_control.resolve(exportedAPI);
    }).catch(err => {
        dotnetReady.promise_control.reject(err);
    });
    module.ready = dotnetReady.promise;
    // execution order == [*] ==
    if (!module.onAbort) {
        module.onAbort = () => mono_on_abort;
    }
}


function instantiateWasm(
    imports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
    userInstantiateWasm?: InstantiateWasmCallBack): any[] {
    // this is called so early that even Module exports like addRunDependency don't exist yet

    if (!Module.configSrc && !Module.config && !userInstantiateWasm) {
        Module.print("MONO_WASM: configSrc nor config was specified");
    }
    normalizeConfig();

    const mark = startMeasure();
    if (userInstantiateWasm) {
        const exports = userInstantiateWasm(imports, (instance: WebAssembly.Instance, module: WebAssembly.Module | undefined) => {
            endMeasure(mark, MeasuredBlock.instantiateWasm);
            afterInstantiateWasm.promise_control.resolve();
            successCallback(instance, module);
        });
        return exports;
    }

    instantiate_wasm_module(imports, successCallback);
    return []; // No exports
}

async function instantiateWasmWorker(
    imports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback
): Promise<void> {
    // wait for the config to arrive by message from the main thread
    await afterConfigLoaded.promise;

    const anyModule = Module as any;
    normalizeConfig();
    replace_linker_placeholders(imports, export_linker());

    // Instantiate from the module posted from the main thread.
    // We can just use sync instantiation in the worker.
    const instance = new WebAssembly.Instance(anyModule.wasmModule, imports);
    successCallback(instance, undefined);
    anyModule.wasmModule = null;
}

function preInit(userPreInit: (() => void)[]) {
    Module.addRunDependency("mono_pre_init");
    const mark = startMeasure();
    try {
        mono_wasm_pre_init_essential(false);
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: preInit");
        beforePreInit.promise_control.resolve();
        // all user Module.preInit callbacks
        userPreInit.forEach(fn => fn());
    } catch (err) {
        _print_error("MONO_WASM: user preInint() failed", err);
        abort_startup(err, true);
        throw err;
    }
    // this will start immediately but return on first await.
    // It will block our `preRun` by afterPreInit promise
    // It will block emscripten `userOnRuntimeInitialized` by pending addRunDependency("mono_pre_init")
    (async () => {
        try {
            await mono_wasm_pre_init_essential_async();

            // - download Module.config from configSrc
            // - start download assets like DLLs
            await mono_wasm_pre_init_full();

            endMeasure(mark, MeasuredBlock.preInit);
        } catch (err) {
            abort_startup(err, true);
            throw err;
        }
        // signal next stage
        afterPreInit.promise_control.resolve();
        Module.removeRunDependency("mono_pre_init");
    })();
}

async function preInitWorkerAsync() {
    console.debug("MONO_WASM: worker initializing essential C exports and APIs");
    const mark = startMeasure();
    try {
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: preInitWorker");
        beforePreInit.promise_control.resolve();
        mono_wasm_pre_init_essential(true);
        await init_polyfills_async();
        afterPreInit.promise_control.resolve();
        endMeasure(mark, MeasuredBlock.preInitWorker);
    } catch (err) {
        _print_error("MONO_WASM: user preInitWorker() failed", err);
        abort_startup(err, true);
        throw err;
    }
}

export function preRunWorker() {
    const mark = startMeasure();
    try {
        bindings_init();
        endMeasure(mark, MeasuredBlock.preRunWorker);
    } catch (err) {
        abort_startup(err, true);
        throw err;
    }
    // signal next stage
    afterPreRun.promise_control.resolve();
}

async function preRunAsync(userPreRun: (() => void)[]) {
    Module.addRunDependency("mono_pre_run_async");
    // wait for previous stages
    await afterInstantiateWasm.promise;
    await afterPreInit.promise;
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: preRunAsync");
    const mark = startMeasure();
    try {
        if (MonoWasmThreads) {
            await instantiateWasmPThreadWorkerPool();
        }
        // all user Module.preRun callbacks
        userPreRun.map(fn => fn());
        endMeasure(mark, MeasuredBlock.preRun);
    } catch (err) {
        _print_error("MONO_WASM: user callback preRun() failed", err);
        abort_startup(err, true);
        throw err;
    }
    // signal next stage
    afterPreRun.promise_control.resolve();
    Module.removeRunDependency("mono_pre_run_async");
}

async function onRuntimeInitializedAsync(userOnRuntimeInitialized: () => void) {
    // wait for previous stage
    await afterPreRun.promise;
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: onRuntimeInitialized");
    const mark = startMeasure();
    // signal this stage, this will allow pending assets to allocate memory
    beforeOnRuntimeInitialized.promise_control.resolve();
    try {
        await wait_for_all_assets();
        // load runtime
        await mono_wasm_before_user_runtime_initialized();

        if (config.runtimeOptions) {
            mono_wasm_set_runtime_options(config.runtimeOptions);
        }
        // call user code
        try {
            userOnRuntimeInitialized();
        }
        catch (err: any) {
            _print_error("MONO_WASM: user callback onRuntimeInitialized() failed", err);
            throw err;
        }
        // finish
        await mono_wasm_after_user_runtime_initialized();
        endMeasure(mark, MeasuredBlock.onRuntimeInitialized);
    } catch (err) {
        _print_error("MONO_WASM: onRuntimeInitializedAsync() failed", err);
        abort_startup(err, true);
        throw err;
    }
    // signal next stage
    afterOnRuntimeInitialized.promise_control.resolve();
}

async function postRunAsync(userpostRun: (() => void)[]) {
    // wait for previous stage
    await afterOnRuntimeInitialized.promise;
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: postRunAsync");
    try {
        const mark = startMeasure();
        // all user Module.postRun callbacks
        userpostRun.map(fn => fn());
        endMeasure(mark, MeasuredBlock.postRun);
    } catch (err) {
        _print_error("MONO_WASM: user callback posRun() failed", err);
        abort_startup(err, true);
        throw err;
    }
    // signal next stage
    afterPostRun.promise_control.resolve();
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function abort_startup(reason: any, should_exit: boolean): void {
    if (runtimeHelpers.diagnosticTracing) console.trace("MONO_WASM: abort_startup");
    dotnetReady.promise_control.reject(reason);
    afterInstantiateWasm.promise_control.reject(reason);
    beforePreInit.promise_control.reject(reason);
    afterPreInit.promise_control.reject(reason);
    afterPreRun.promise_control.reject(reason);
    beforeOnRuntimeInitialized.promise_control.reject(reason);
    afterOnRuntimeInitialized.promise_control.reject(reason);
    afterPostRun.promise_control.reject(reason);
    if (should_exit) {
        mono_exit(1, reason);
    }
}

// runs in both blazor and non-blazor
function mono_wasm_pre_init_essential(isWorker: boolean): void {
    if (!isWorker)
        Module.addRunDependency("mono_wasm_pre_init_essential");

    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_pre_init_essential");

    // init_polyfills() is already called from export.ts
    init_c_exports();
    cwraps_internal(INTERNAL);
    if (WasmEnableLegacyJsInterop) {
        cwraps_mono_api(MONO);
        cwraps_binding_api(BINDING);
    }
    // removeRunDependency triggers the dependenciesFulfilled callback (runCaller) in
    // emscripten - on a worker since we don't have any other dependencies that causes run() to get
    // called too soon; and then it will get called a second time when dotnet.js calls it directly.
    // on a worker run() short-cirtcuits and just calls   readyPromiseResolve, initRuntime and postMessage.
    // sending postMessage twice will break instantiateWasmPThreadWorkerPool on the main thread.
    if (!isWorker)
        Module.removeRunDependency("mono_wasm_pre_init_essential");
}

// runs in both blazor and non-blazor
async function mono_wasm_pre_init_essential_async(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_pre_init_essential_async");
    Module.addRunDependency("mono_wasm_pre_init_essential_async");

    await init_polyfills_async();
    await mono_wasm_load_config(Module.configSrc);

    if (MonoWasmThreads) {
        preAllocatePThreadWorkerPool(MONO_PTHREAD_POOL_SIZE, config);
    }

    Module.removeRunDependency("mono_wasm_pre_init_essential_async");
}

// runs just in non-blazor
async function mono_wasm_pre_init_full(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_pre_init_full");
    Module.addRunDependency("mono_wasm_pre_init_full");

    await mono_download_assets();

    Module.removeRunDependency("mono_wasm_pre_init_full");
}

// runs just in non-blazor
async function mono_wasm_before_user_runtime_initialized(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_before_user_runtime_initialized");

    try {
        await _apply_configuration_from_args();
        mono_wasm_globalization_init();

        if (!runtimeHelpers.mono_wasm_load_runtime_done) mono_wasm_load_runtime("unused", config.debugLevel);
        if (!runtimeHelpers.mono_wasm_runtime_is_ready) mono_wasm_runtime_ready();
        if (!runtimeHelpers.mono_wasm_symbols_are_ready) readSymbolMapFile("dotnet.js.symbols");

        setTimeout(() => {
            // when there are free CPU cycles
            string_decoder.init_fields();
        });
    } catch (err: any) {
        _print_error("MONO_WASM: Error in mono_wasm_before_user_runtime_initialized", err);
        throw err;
    }
}

// runs in both blazor and non-blazor
async function mono_wasm_after_user_runtime_initialized(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_after_user_runtime_initialized");
    try {
        if (!Module.disableDotnet6Compatibility && Module.exports) {
            // Export emscripten defined in module through EXPORTED_RUNTIME_METHODS
            // Useful to export IDBFS or other similar types generally exposed as
            // global types when emscripten is not modularized.
            const globalThisAny = globalThis as any;
            for (let i = 0; i < Module.exports.length; ++i) {
                const exportName = Module.exports[i];
                const exportValue = (<any>Module)[exportName];

                if (exportValue != undefined) {
                    globalThisAny[exportName] = exportValue;
                }
                else {
                    console.warn(`MONO_WASM: The exported symbol ${exportName} could not be found in the emscripten module`);
                }
            }
        }
        // for Blazor, init diagnostics after their "onRuntimeInitalized" sets env variables, but before their postRun callback (which calls mono_wasm_load_runtime)
        if (MonoWasmThreads) {
            await mono_wasm_init_diagnostics();
        }

        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: Initializing mono runtime");

        if (Module.onDotnetReady) {
            try {
                await Module.onDotnetReady();
            }
            catch (err: any) {
                _print_error("MONO_WASM: onDotnetReady () failed", err);
                throw err;
            }
        }
    } catch (err: any) {
        _print_error("MONO_WASM: Error in mono_wasm_after_user_runtime_initialized", err);
        throw err;
    }
}


function _print_error(message: string, err: any): void {
    Module.printErr(`${message}: ${JSON.stringify(err)}`);
    if (err.stack) {
        Module.printErr("MONO_WASM: Stacktrace: \n");
        Module.printErr(err.stack);
    }
}

// Set environment variable NAME to VALUE
// Should be called before mono_load_runtime_and_bcl () in most cases
export function mono_wasm_setenv(name: string, value: string): void {
    cwraps.mono_wasm_setenv(name, value);
}

export function mono_wasm_set_runtime_options(options: string[]): void {
    if (!Array.isArray(options))
        throw new Error("Expected runtimeOptions to be an array of strings");

    const argv = Module._malloc(options.length * 4);
    let aindex = 0;
    for (let i = 0; i < options.length; ++i) {
        const option = options[i];
        if (typeof (option) !== "string")
            throw new Error("Expected runtimeOptions to be an array of strings");
        Module.setValue(<any>argv + (aindex * 4), cwraps.mono_wasm_strdup(option), "i32");
        aindex += 1;
    }
    cwraps.mono_wasm_parse_runtime_options(options.length, argv);
}

function replace_linker_placeholders(
    imports: WebAssembly.Imports,
    realFunctions: any
) {
    // the output from emcc contains wrappers for these linker imports which add overhead,
    //  but now we have what we need to replace them with the actual functions
    const env = imports.env;
    for (const k in realFunctions) {
        const v = realFunctions[k];
        if (typeof (v) !== "function")
            continue;
        if (k in env)
            env[k] = v;
    }
}

async function instantiate_wasm_module(
    imports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
): Promise<void> {
    // this is called so early that even Module exports like addRunDependency don't exist yet
    try {
        replace_linker_placeholders(imports, export_linker());
        await mono_wasm_load_config(Module.configSrc);
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module");
        const assetToLoad = resolve_asset_path("dotnetwasm");
        // FIXME: this would not apply re-try (on connection reset during download) for dotnet.wasm because we could not download the buffer before we pass it to instantiate_wasm_asset
        await start_asset_download(assetToLoad);
        await beforePreInit.promise;
        Module.addRunDependency("instantiate_wasm_module");
        await instantiate_wasm_asset(assetToLoad, imports, successCallback);
        assetToLoad.pendingDownloadInternal = null as any; // GC
        assetToLoad.pendingDownload = null as any; // GC
        assetToLoad.buffer = null as any; // GC

        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module done");
        afterInstantiateWasm.promise_control.resolve();
    } catch (err) {
        _print_error("MONO_WASM: instantiate_wasm_module() failed", err);
        abort_startup(err, true);
        throw err;
    }
    Module.removeRunDependency("instantiate_wasm_module");
}

// runs just in non-blazor
async function _apply_configuration_from_args() {
    try {
        const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
        if (tz) mono_wasm_setenv("TZ", tz);
    } catch {
        console.info("MONO_WASM: failed to detect timezone, will fallback to UTC");
    }

    // create /usr/share folder which is SpecialFolder.CommonApplicationData
    Module["FS_createPath"]("/", "usr", true, true);
    Module["FS_createPath"]("/", "usr/share", true, true);

    for (const k in config.environmentVariables) {
        const v = config.environmentVariables![k];
        if (typeof (v) === "string")
            mono_wasm_setenv(k, v);
        else
            throw new Error(`Expected environment variable '${k}' to be a string but it was ${typeof v}: '${v}'`);
    }

    if (config.runtimeOptions)
        mono_wasm_set_runtime_options(config.runtimeOptions);

    if (config.aotProfilerOptions)
        mono_wasm_init_aot_profiler(config.aotProfilerOptions);

    if (config.browserProfilerOptions)
        mono_wasm_init_browser_profiler(config.browserProfilerOptions);

    // for non-Blazor, init diagnostics after environment variables are set
    if (MonoWasmThreads) {
        await mono_wasm_init_diagnostics();
    }
}

export function mono_wasm_load_runtime(unused?: string, debugLevel?: number): void {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_load_runtime");
    if (runtimeHelpers.mono_wasm_load_runtime_done) {
        return;
    }
    runtimeHelpers.mono_wasm_load_runtime_done = true;
    try {
        const mark = startMeasure();
        if (debugLevel == undefined) {
            debugLevel = 0;
            if (config && config.debugLevel) {
                debugLevel = 0 + debugLevel;
            }
        }
        cwraps.mono_wasm_load_runtime(unused || "unused", debugLevel);
        endMeasure(mark, MeasuredBlock.loadRuntime);
        runtimeHelpers.waitForDebugger = config.waitForDebugger;

        if (!runtimeHelpers.mono_wasm_bindings_is_ready) bindings_init();
    } catch (err: any) {
        _print_error("MONO_WASM: mono_wasm_load_runtime () failed", err);

        abort_startup(err, false);
        if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
            const wasm_exit = cwraps.mono_wasm_exit;
            wasm_exit(1);
        }
        throw err;
    }
}

export function bindings_init(): void {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: bindings_init");
    if (runtimeHelpers.mono_wasm_bindings_is_ready) {
        return;
    }
    runtimeHelpers.mono_wasm_bindings_is_ready = true;
    try {
        const mark = startMeasure();
        init_managed_exports();
        if (WasmEnableLegacyJsInterop) {
            init_legacy_exports();
        }
        initialize_marshalers_to_js();
        initialize_marshalers_to_cs();
        runtimeHelpers._i52_error_scratch_buffer = <any>Module._malloc(4);
        endMeasure(mark, MeasuredBlock.bindingsInit);
    } catch (err) {
        _print_error("MONO_WASM: Error in bindings_init", err);
        throw err;
    }
}

/**
 * Loads the mono config file (typically called mono-config.json) asynchroniously
 * Note: the run dependencies are so emsdk actually awaits it in order.
 *
 * @param {string} configFilePath - relative path to the config file
 * @throws Will throw an error if the config file loading fails
 */
export async function mono_wasm_load_config(configFilePath?: string): Promise<void> {
    if (configLoaded) {
        await afterConfigLoaded.promise;
        return;
    }
    configLoaded = true;
    if (!configFilePath) {
        normalizeConfig();
        afterConfigLoaded.promise_control.resolve(runtimeHelpers.config);
        return;
    }
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_load_config");
    try {
        const resolveSrc = runtimeHelpers.locateFile(configFilePath);
        const configResponse = await runtimeHelpers.fetch_like(resolveSrc);
        const loadedConfig: MonoConfigInternal = (await configResponse.json()) || {};
        if (loadedConfig.environmentVariables && typeof (loadedConfig.environmentVariables) !== "object")
            throw new Error("Expected config.environmentVariables to be unset or a dictionary-style object");

        // merge
        loadedConfig.assets = [...(loadedConfig.assets || []), ...(config.assets || [])];
        loadedConfig.environmentVariables = { ...(loadedConfig.environmentVariables || {}), ...(config.environmentVariables || {}) };
        loadedConfig.runtimeOptions = [...(loadedConfig.runtimeOptions || []), ...(config.runtimeOptions || [])];
        config = runtimeHelpers.config = Module.config = Object.assign(Module.config as any, loadedConfig);

        normalizeConfig();

        if (Module.onConfigLoaded) {
            try {
                await Module.onConfigLoaded(<MonoConfig>runtimeHelpers.config);
                normalizeConfig();
            }
            catch (err: any) {
                _print_error("MONO_WASM: onConfigLoaded() failed", err);
                throw err;
            }
        }
        afterConfigLoaded.promise_control.resolve(runtimeHelpers.config);
    } catch (err) {
        const errMessage = `Failed to load config file ${configFilePath} ${err}`;
        abort_startup(errMessage, true);
        config = runtimeHelpers.config = Module.config = <any>{ message: errMessage, error: err, isError: true };
        throw err;
    }

}

function normalizeConfig() {
    // normalize
    Module.config = config = runtimeHelpers.config = Object.assign(runtimeHelpers.config, Module.config || {});
    config.environmentVariables = config.environmentVariables || {};
    config.assets = config.assets || [];
    config.runtimeOptions = config.runtimeOptions || [];
    config.globalizationMode = config.globalizationMode || "auto";
    if (config.debugLevel === undefined && BuildConfiguration === "Debug") {
        config.debugLevel = -1;
    }
    if (config.diagnosticTracing === undefined && BuildConfiguration === "Debug") {
        config.diagnosticTracing = true;
    }
    runtimeHelpers.diagnosticTracing = !!runtimeHelpers.config.diagnosticTracing;

    runtimeHelpers.enablePerfMeasure = !!config.browserProfilerOptions
        && globalThis.performance
        && typeof globalThis.performance.measure === "function";
}


export function mono_wasm_asm_loaded(assembly_name: CharPtr, assembly_ptr: number, assembly_len: number, pdb_ptr: number, pdb_len: number): void {
    // Only trigger this codepath for assemblies loaded after app is ready
    if (runtimeHelpers.mono_wasm_runtime_is_ready !== true)
        return;

    const assembly_name_str = assembly_name !== CharPtrNull ? Module.UTF8ToString(assembly_name).concat(".dll") : "";
    const assembly_data = new Uint8Array(Module.HEAPU8.buffer, assembly_ptr, assembly_len);
    const assembly_b64 = toBase64StringImpl(assembly_data);

    let pdb_b64;
    if (pdb_ptr) {
        const pdb_data = new Uint8Array(Module.HEAPU8.buffer, pdb_ptr, pdb_len);
        pdb_b64 = toBase64StringImpl(pdb_data);
    }

    mono_wasm_raise_debug_event({
        eventName: "AssemblyLoaded",
        assembly_name: assembly_name_str,
        assembly_b64,
        pdb_b64
    });
}

export function mono_wasm_set_main_args(name: string, allRuntimeArguments: string[]): void {
    const main_argc = allRuntimeArguments.length + 1;
    const main_argv = <any>Module._malloc(main_argc * 4);
    let aindex = 0;
    Module.setValue(main_argv + (aindex * 4), cwraps.mono_wasm_strdup(name), "i32");
    aindex += 1;
    for (let i = 0; i < allRuntimeArguments.length; ++i) {
        Module.setValue(main_argv + (aindex * 4), cwraps.mono_wasm_strdup(allRuntimeArguments[i]), "i32");
        aindex += 1;
    }
    cwraps.mono_wasm_set_main_args(main_argc, main_argv);
}

/// Called when dotnet.worker.js receives an emscripten "load" event from the main thread.
/// This method is comparable to configure_emscripten_startup function
///
/// Notes:
/// 1. Emscripten skips a lot of initialization on the pthread workers, Module may not have everything you expect.
/// 2. Emscripten does not run any event but preInit in the workers.
/// 3. At the point when this executes there is no pthread assigned to the worker yet.
export async function mono_wasm_pthread_worker_init(module: DotnetModule, exportedAPI: RuntimeAPI): Promise<DotnetModule> {
    pthreads_worker.setupPreloadChannelToMainThread();
    // This is a good place for subsystems to attach listeners for pthreads_worker.currentWorkerThreadEvents
    pthreads_worker.currentWorkerThreadEvents.addEventListener(pthreads_worker.dotnetPthreadCreated, (ev) => {
        console.debug("MONO_WASM: pthread created", ev.pthread_self.pthread_id);
    });

    // this is the only event which is called on worker
    module.preInit = [() => preInitWorkerAsync()];
    module.instantiateWasm = instantiateWasmWorker;

    await afterPreInit.promise;
    return exportedAPI.Module;
}
