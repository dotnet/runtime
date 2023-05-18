// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import WasmEnableLegacyJsInterop from "consts:WasmEnableLegacyJsInterop";

import { DotnetModuleInternal, CharPtrNull } from "./types/internal";
import { disableLegacyJsInterop, exportedRuntimeAPI, INTERNAL, loaderHelpers, Module, runtimeHelpers } from "./globals";
import cwraps, { init_c_exports } from "./cwraps";
import { mono_wasm_raise_debug_event, mono_wasm_runtime_ready } from "./debug";
import { toBase64StringImpl } from "./base64";
import { mono_wasm_init_aot_profiler, mono_wasm_init_browser_profiler } from "./profiler";
import { initialize_marshalers_to_cs } from "./marshal-to-cs";
import { initialize_marshalers_to_js } from "./marshal-to-js";
import { init_polyfills_async } from "./polyfills";
import * as pthreads_worker from "./pthreads/worker";
import { string_decoder } from "./strings";
import { init_managed_exports } from "./managed-exports";
import { cwraps_internal } from "./exports-internal";
import { CharPtr, InstantiateWasmCallBack, InstantiateWasmSuccessCallback } from "./types/emscripten";
import { instantiate_wasm_asset, wait_for_all_assets } from "./assets";
import { mono_wasm_init_diagnostics } from "./diagnostics";
import { preAllocatePThreadWorkerPool, instantiateWasmPThreadWorkerPool } from "./pthreads/browser";
import { export_linker } from "./exports-linker";
import { endMeasure, MeasuredBlock, startMeasure } from "./profiler";
import { getMemorySnapshot, storeMemorySnapshot, getMemorySnapshotSize } from "./snapshot";

// legacy
import { init_legacy_exports } from "./net6-legacy/corebindings";
import { cwraps_binding_api, cwraps_mono_api } from "./net6-legacy/exports-legacy";
import { BINDING, MONO } from "./net6-legacy/globals";
import { mono_log_debug, mono_log_warn } from "./logging";


// default size if MonoConfig.pthreadPoolSize is undefined
const MONO_PTHREAD_POOL_SIZE = 4;

// we are making emscripten startup async friendly
// emscripten is executing the events without awaiting it and so we need to block progress via PromiseControllers above
export function configureEmscriptenStartup(module: DotnetModuleInternal): void {
    const mark = startMeasure();

    if (!module.locateFile) {
        // this is dummy plug so that wasmBinaryFile doesn't try to use URL class
        module.locateFile = module.__locateFile = (path) => loaderHelpers.scriptDirectory + path;
    }

    if (!module.out) {
        // eslint-disable-next-line no-console
        module.out = console.log.bind(console);
    }

    if (!module.err) {
        // eslint-disable-next-line no-console
        module.err = console.error.bind(console);
    }
    loaderHelpers.out = module.out;
    loaderHelpers.err = module.err;
    module.mainScriptUrlOrBlob = loaderHelpers.scriptUrl;// this is needed by worker threads

    // these all could be overridden on DotnetModuleConfig, we are chaing them to async below, as opposed to emscripten
    // when user set configSrc or config, we are running our default startup sequence.
    const userInstantiateWasm: undefined | ((imports: WebAssembly.Imports, successCallback: InstantiateWasmSuccessCallback) => any) = module.instantiateWasm;
    const userPreInit: (() => void)[] = !module.preInit ? [] : typeof module.preInit === "function" ? [module.preInit] : module.preInit;
    const userPreRun: (() => void)[] = !module.preRun ? [] : typeof module.preRun === "function" ? [module.preRun] : module.preRun as any;
    const userpostRun: (() => void)[] = !module.postRun ? [] : typeof module.postRun === "function" ? [module.postRun] : module.postRun as any;
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    const userOnRuntimeInitialized: () => void = module.onRuntimeInitialized ? module.onRuntimeInitialized : () => { };

    // execution order == [0] ==
    // - default or user Module.instantiateWasm (will start downloading dotnet.native.wasm)
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
        await runtimeHelpers.afterPostRun.promise;
        // startup end
        endMeasure(mark, MeasuredBlock.emscriptenStartup);
        // - here we resolve the promise returned by createDotnetRuntime export
        // - any code after createDotnetRuntime is executed now
        runtimeHelpers.dotnetReady.promise_control.resolve(exportedRuntimeAPI);
        runtimeHelpers.runtimeReady = true;
    }).catch(err => {
        runtimeHelpers.dotnetReady.promise_control.reject(err);
    });
    module.ready = runtimeHelpers.dotnetReady.promise;
    // execution order == [*] ==
    if (!module.onAbort) {
        module.onAbort = (error) => {
            loaderHelpers.abort_startup(error, false);
            loaderHelpers.mono_exit(1, error);
        };
    }
}

function instantiateWasm(
    imports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
    userInstantiateWasm?: InstantiateWasmCallBack): any[] {
    // this is called so early that even Module exports like addRunDependency don't exist yet

    const mark = startMeasure();
    if (userInstantiateWasm) {
        // user wasm instantiation doesn't support memory snapshots
        runtimeHelpers.memorySnapshotSkippedOrDone.promise_control.resolve();
        const exports = userInstantiateWasm(imports, (instance: WebAssembly.Instance, module: WebAssembly.Module | undefined) => {
            endMeasure(mark, MeasuredBlock.instantiateWasm);
            runtimeHelpers.afterInstantiateWasm.promise_control.resolve();
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
    await loaderHelpers.afterConfigLoaded.promise;

    replace_linker_placeholders(imports, export_linker());

    // Instantiate from the module posted from the main thread.
    // We can just use sync instantiation in the worker.
    const instance = new WebAssembly.Instance(Module.wasmModule!, imports);
    successCallback(instance, undefined);
    Module.wasmModule = null;
}

function preInit(userPreInit: (() => void)[]) {
    Module.addRunDependency("mono_pre_init");
    const mark = startMeasure();
    try {
        mono_wasm_pre_init_essential(false);
        mono_log_debug("preInit");
        runtimeHelpers.beforePreInit.promise_control.resolve();
        // all user Module.preInit callbacks
        userPreInit.forEach(fn => fn());
    } catch (err) {
        _print_error("user preInint() failed", err);
        loaderHelpers.abort_startup(err, true);
        throw err;
    }
    // this will start immediately but return on first await.
    // It will block our `preRun` by afterPreInit promise
    // It will block emscripten `userOnRuntimeInitialized` by pending addRunDependency("mono_pre_init")
    (async () => {
        try {
            // - init the rest of the polyfills
            // - download Module.config from configSrc
            await mono_wasm_pre_init_essential_async();

            // - start download assets like DLLs
            await mono_wasm_pre_init_full();

            endMeasure(mark, MeasuredBlock.preInit);
        } catch (err) {
            loaderHelpers.abort_startup(err, true);
            throw err;
        }
        // signal next stage
        runtimeHelpers.afterPreInit.promise_control.resolve();
        Module.removeRunDependency("mono_pre_init");
    })();
}

async function preInitWorkerAsync() {
    mono_log_debug("worker initializing essential C exports and APIs");
    const mark = startMeasure();
    try {
        mono_log_debug("preInitWorker");
        runtimeHelpers.beforePreInit.promise_control.resolve();
        mono_wasm_pre_init_essential(true);
        await init_polyfills_async();
        runtimeHelpers.afterPreInit.promise_control.resolve();
        endMeasure(mark, MeasuredBlock.preInitWorker);
    } catch (err) {
        _print_error("user preInitWorker() failed", err);
        loaderHelpers.abort_startup(err, true);
        throw err;
    }
}

export function preRunWorker() {
    const mark = startMeasure();
    try {
        bindings_init();
        endMeasure(mark, MeasuredBlock.preRunWorker);
    } catch (err) {
        loaderHelpers.abort_startup(err, true);
        throw err;
    }
    // signal next stage
    runtimeHelpers.afterPreRun.promise_control.resolve();
}

async function preRunAsync(userPreRun: (() => void)[]) {
    Module.addRunDependency("mono_pre_run_async");
    // wait for previous stages
    try {
        await runtimeHelpers.afterInstantiateWasm.promise;
        await runtimeHelpers.afterPreInit.promise;
        mono_log_debug("preRunAsync");
        const mark = startMeasure();
        // all user Module.preRun callbacks
        userPreRun.map(fn => fn());
        endMeasure(mark, MeasuredBlock.preRun);
    } catch (err) {
        _print_error("user callback preRun() failed", err);
        loaderHelpers.abort_startup(err, true);
        throw err;
    }
    // signal next stage
    runtimeHelpers.afterPreRun.promise_control.resolve();
    Module.removeRunDependency("mono_pre_run_async");
}

async function onRuntimeInitializedAsync(userOnRuntimeInitialized: () => void) {
    try {
        // wait for previous stage
        await runtimeHelpers.afterPreRun.promise;
        mono_log_debug("onRuntimeInitialized");
        const mark = startMeasure();
        // signal this stage, this will allow pending assets to allocate memory
        runtimeHelpers.beforeOnRuntimeInitialized.promise_control.resolve();

        await wait_for_all_assets();

        // Diagnostics early are not supported with memory snapshot. See below how we enable them later.
        // Please disable startupMemoryCache in order to be able to diagnose or pause runtime startup.
        if (MonoWasmThreads && !runtimeHelpers.config.startupMemoryCache) {
            await mono_wasm_init_diagnostics();
        }

        // load runtime and apply environment settings (if necessary)
        await mono_wasm_before_memory_snapshot();

        if (runtimeHelpers.config.exitAfterSnapshot) {
            const reason = runtimeHelpers.ExitStatus
                ? new runtimeHelpers.ExitStatus(0)
                : new Error("Snapshot taken, exiting because exitAfterSnapshot was set.");
            reason.silent = true;

            loaderHelpers.abort_startup(reason, false);
            return;
        }

        if (MonoWasmThreads) {
            if (runtimeHelpers.config.startupMemoryCache) {
                // we could enable diagnostics after the snapshot is taken
                await mono_wasm_init_diagnostics();
            }
            await instantiateWasmPThreadWorkerPool();
        }

        bindings_init();
        if (!runtimeHelpers.mono_wasm_runtime_is_ready) mono_wasm_runtime_ready();

        setTimeout(() => {
            // when there are free CPU cycles
            string_decoder.init_fields();
        });

        if (runtimeHelpers.config.startupOptions && INTERNAL.resourceLoader) {
            if (INTERNAL.resourceLoader.bootConfig.debugBuild && INTERNAL.resourceLoader.bootConfig.cacheBootResources) {
                INTERNAL.resourceLoader.logToConsole();
            }
            INTERNAL.resourceLoader.purgeUnusedCacheEntriesAsync(); // Don't await - it's fine to run in background
        }

        // call user code
        try {
            userOnRuntimeInitialized();
        }
        catch (err: any) {
            _print_error("user callback onRuntimeInitialized() failed", err);
            throw err;
        }
        // finish
        await mono_wasm_after_user_runtime_initialized();
        endMeasure(mark, MeasuredBlock.onRuntimeInitialized);
    } catch (err) {
        _print_error("onRuntimeInitializedAsync() failed", err);
        loaderHelpers.abort_startup(err, true);
        throw err;
    }
    // signal next stage
    runtimeHelpers.afterOnRuntimeInitialized.promise_control.resolve();
}

async function postRunAsync(userpostRun: (() => void)[]) {
    // wait for previous stage
    try {
        await runtimeHelpers.afterOnRuntimeInitialized.promise;
        mono_log_debug("postRunAsync");
        const mark = startMeasure();

        // create /usr/share folder which is SpecialFolder.CommonApplicationData
        Module["FS_createPath"]("/", "usr", true, true);
        Module["FS_createPath"]("/", "usr/share", true, true);

        // all user Module.postRun callbacks
        userpostRun.map(fn => fn());
        endMeasure(mark, MeasuredBlock.postRun);
    } catch (err) {
        _print_error("user callback posRun() failed", err);
        loaderHelpers.abort_startup(err, true);
        throw err;
    }
    // signal next stage
    runtimeHelpers.afterPostRun.promise_control.resolve();
}


function mono_wasm_pre_init_essential(isWorker: boolean): void {
    if (!isWorker)
        Module.addRunDependency("mono_wasm_pre_init_essential");

    mono_log_debug("mono_wasm_pre_init_essential");

    init_c_exports();
    cwraps_internal(INTERNAL);
    if (WasmEnableLegacyJsInterop && !disableLegacyJsInterop) {
        cwraps_mono_api(MONO);
        cwraps_binding_api(BINDING);
    }
    // removeRunDependency triggers the dependenciesFulfilled callback (runCaller) in
    // emscripten - on a worker since we don't have any other dependencies that causes run() to get
    // called too soon; and then it will get called a second time when dotnet.native.js calls it directly.
    // on a worker run() short-cirtcuits and just calls   readyPromiseResolve, initRuntime and postMessage.
    // sending postMessage twice will break instantiateWasmPThreadWorkerPool on the main thread.
    if (!isWorker)
        Module.removeRunDependency("mono_wasm_pre_init_essential");
}

async function mono_wasm_pre_init_essential_async(): Promise<void> {
    mono_log_debug("mono_wasm_pre_init_essential_async");
    Module.addRunDependency("mono_wasm_pre_init_essential_async");

    await init_polyfills_async();

    if (MonoWasmThreads) {
        preAllocatePThreadWorkerPool(MONO_PTHREAD_POOL_SIZE, runtimeHelpers.config);
    }

    Module.removeRunDependency("mono_wasm_pre_init_essential_async");
}

async function mono_wasm_pre_init_full(): Promise<void> {
    mono_log_debug("mono_wasm_pre_init_full");
    Module.addRunDependency("mono_wasm_pre_init_full");

    await loaderHelpers.mono_download_assets();

    Module.removeRunDependency("mono_wasm_pre_init_full");
}

async function mono_wasm_after_user_runtime_initialized(): Promise<void> {
    mono_log_debug("mono_wasm_after_user_runtime_initialized");
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
                    mono_log_warn(`The exported symbol ${exportName} could not be found in the emscripten module`);
                }
            }
        }

        mono_log_debug("Initializing mono runtime");

        if (Module.onDotnetReady) {
            try {
                await Module.onDotnetReady();
            }
            catch (err: any) {
                _print_error("onDotnetReady () failed", err);
                throw err;
            }
        }
    } catch (err: any) {
        _print_error("Error in mono_wasm_after_user_runtime_initialized", err);
        throw err;
    }
}


function _print_error(message: string, err: any): void {
    if (typeof err === "object" && err.silent) {
        return;
    }
    Module.err(`${message}: ${JSON.stringify(err)}`);
    if (typeof err === "object" && err.stack) {
        Module.err("Stacktrace: \n");
        Module.err(err.stack);
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
        let memorySize: number | undefined = undefined;
        await loaderHelpers.afterConfigLoaded;
        mono_log_debug("instantiate_wasm_module");

        if (runtimeHelpers.config.startupMemoryCache) {
            memorySize = await getMemorySnapshotSize();
            runtimeHelpers.loadedMemorySnapshot = !!memorySize;
            runtimeHelpers.storeMemorySnapshotPending = !runtimeHelpers.loadedMemorySnapshot;
        }
        if (!runtimeHelpers.loadedMemorySnapshot) {
            // we should start downloading DLLs etc as they are not in the snapshot
            runtimeHelpers.memorySnapshotSkippedOrDone.promise_control.resolve();
        }

        await runtimeHelpers.beforePreInit.promise;
        Module.addRunDependency("instantiate_wasm_module");

        replace_linker_placeholders(imports, export_linker());
        const assetToLoad = await loaderHelpers.wasmDownloadPromise.promise;
        await instantiate_wasm_asset(assetToLoad, imports, successCallback);
        assetToLoad.pendingDownloadInternal = null as any; // GC
        assetToLoad.pendingDownload = null as any; // GC
        assetToLoad.buffer = null as any; // GC

        mono_log_debug("instantiate_wasm_module done");

        if (runtimeHelpers.loadedMemorySnapshot) {
            try {
                const wasmMemory = (Module.asm?.memory || Module.wasmMemory)!;

                // .grow() takes a delta compared to the previous size
                wasmMemory.grow((memorySize! - wasmMemory.buffer.byteLength + 65535) >>> 16);
                runtimeHelpers.updateMemoryViews();
            } catch (err) {
                mono_log_warn("failed to resize memory for the snapshot", err);
                runtimeHelpers.loadedMemorySnapshot = false;
            }
            // now we know if the loading of memory succeeded or not, we can start loading the rest of the assets
            runtimeHelpers.memorySnapshotSkippedOrDone.promise_control.resolve();
        }
        runtimeHelpers.afterInstantiateWasm.promise_control.resolve();
    } catch (err) {
        _print_error("instantiate_wasm_module() failed", err);
        loaderHelpers.abort_startup(err, true);
        throw err;
    }
    Module.removeRunDependency("instantiate_wasm_module");
}

async function mono_wasm_before_memory_snapshot() {
    const mark = startMeasure();
    if (runtimeHelpers.loadedMemorySnapshot) {
        // get the bytes after we re-sized the memory, so that we don't have too much memory in use at the same time
        const memoryBytes = await getMemorySnapshot();
        mono_assert(memoryBytes!.byteLength === Module.HEAP8.byteLength, "Loaded memory is not the expected size");
        Module.HEAP8.set(new Int8Array(memoryBytes!), 0);
        mono_log_debug("Loaded WASM linear memory from browser cache");

        // all things below are loaded from the snapshot
        return;
    }

    for (const k in runtimeHelpers.config.environmentVariables) {
        const v = runtimeHelpers.config.environmentVariables![k];
        if (typeof (v) === "string")
            mono_wasm_setenv(k, v);
        else
            throw new Error(`Expected environment variable '${k}' to be a string but it was ${typeof v}: '${v}'`);
    }
    if (runtimeHelpers.config.startupMemoryCache) {
        // disable the trampoline for now, we will re-enable it after we stored the snapshot
        cwraps.mono_jiterp_update_jit_call_dispatcher(0);
    }
    if (runtimeHelpers.config.runtimeOptions)
        mono_wasm_set_runtime_options(runtimeHelpers.config.runtimeOptions);

    if (runtimeHelpers.config.aotProfilerOptions)
        mono_wasm_init_aot_profiler(runtimeHelpers.config.aotProfilerOptions);

    if (runtimeHelpers.config.browserProfilerOptions)
        mono_wasm_init_browser_profiler(runtimeHelpers.config.browserProfilerOptions);

    mono_wasm_load_runtime("unused", runtimeHelpers.config.debugLevel);

    // we didn't have snapshot yet and the feature is enabled. Take snapshot now.
    if (runtimeHelpers.config.startupMemoryCache) {
        // this would install the mono_jiterp_do_jit_call_indirect
        cwraps.mono_jiterp_update_jit_call_dispatcher(-1);
        await storeMemorySnapshot(Module.HEAP8.buffer);
        runtimeHelpers.storeMemorySnapshotPending = false;
    }

    endMeasure(mark, MeasuredBlock.memorySnapshot);
}

export function mono_wasm_load_runtime(unused?: string, debugLevel?: number): void {
    mono_log_debug("mono_wasm_load_runtime");
    try {
        const mark = startMeasure();
        if (debugLevel == undefined) {
            debugLevel = 0;
            if (runtimeHelpers.config.debugLevel) {
                debugLevel = 0 + debugLevel;
            }
        }
        cwraps.mono_wasm_load_runtime(unused || "unused", debugLevel);
        endMeasure(mark, MeasuredBlock.loadRuntime);

    } catch (err: any) {
        _print_error("mono_wasm_load_runtime () failed", err);
        loaderHelpers.abort_startup(err, false);
    }
}

export function bindings_init(): void {
    if (runtimeHelpers.mono_wasm_bindings_is_ready) {
        return;
    }
    mono_log_debug("bindings_init");
    runtimeHelpers.mono_wasm_bindings_is_ready = true;
    try {
        const mark = startMeasure();
        init_managed_exports();
        if (WasmEnableLegacyJsInterop && !disableLegacyJsInterop) {
            init_legacy_exports();
        }
        initialize_marshalers_to_js();
        initialize_marshalers_to_cs();
        runtimeHelpers._i52_error_scratch_buffer = <any>Module._malloc(4);
        endMeasure(mark, MeasuredBlock.bindingsInit);
    } catch (err) {
        _print_error("Error in bindings_init", err);
        throw err;
    }
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
export async function configureWorkerStartup(module: DotnetModuleInternal): Promise<void> {
    // This is a good place for subsystems to attach listeners for pthreads_worker.currentWorkerThreadEvents
    pthreads_worker.currentWorkerThreadEvents.addEventListener(pthreads_worker.dotnetPthreadCreated, (ev) => {
        mono_log_debug("pthread created 0x" + ev.pthread_self.pthread_id.toString(16));
    });

    // these are the only events which are called on worker
    module.preInit = [() => preInitWorkerAsync()];
    module.instantiateWasm = instantiateWasmWorker;
    await runtimeHelpers.afterPreInit.promise;
}
