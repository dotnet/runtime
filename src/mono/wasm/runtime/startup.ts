// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import BuildConfiguration from "consts:configuration";
import { mono_assert, CharPtrNull, DotnetModule, MonoConfig, MonoConfigError, LoadingResource, AssetEntry, ResourceRequest, AssetBehaviours } from "./types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WEB, INTERNAL, Module, runtimeHelpers } from "./imports";
import cwraps, { init_c_exports } from "./cwraps";
import { mono_wasm_raise_debug_event, mono_wasm_runtime_ready } from "./debug";
import { mono_wasm_globalization_init, mono_wasm_load_icu_data } from "./icu";
import { toBase64StringImpl } from "./base64";
import { mono_wasm_init_aot_profiler, mono_wasm_init_coverage_profiler } from "./profiler";
import { VoidPtr, CharPtr, InstantiateWasmSuccessCallback, InstantiateWasmCallBack } from "./types/emscripten";
import { mono_on_abort, set_exit_code } from "./run";
import { initialize_marshalers_to_cs } from "./marshal-to-cs";
import { initialize_marshalers_to_js } from "./marshal-to-js";
import { init_crypto } from "./crypto-worker";
import { init_polyfills_async } from "./polyfills";
import * as pthreads_worker from "./pthreads/worker";
import { createPromiseController, PromiseAndController } from "./promise-controller";
import { string_decoder } from "./strings";
import { delay } from "./promise-utils";
import { init_managed_exports } from "./managed-exports";
import { init_legacy_exports } from "./net6-legacy/corebindings";
import { mono_wasm_load_bytes_into_heap } from "./memory";
import { cwraps_internal } from "./exports-internal";
import { cwraps_binding_api, cwraps_mono_api } from "./net6-legacy/exports-legacy";
import { DotnetPublicAPI } from "./export-types";
import { BINDING, MONO } from "./net6-legacy/imports";
import { mono_wasm_init_diagnostics } from "./diagnostics";

const all_assets_loaded_in_memory = createPromiseController<void>();
const loaded_files: { url: string, file: string }[] = [];
const loaded_assets: { [id: string]: [VoidPtr, number] } = Object.create(null);
let instantiated_assets_count = 0;
let downloded_assets_count = 0;
// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
let parallel_count = 0;
const skipDownloadsAssetTypes: {
    [k: string]: boolean
} = {
    "js-module-crypto": true,
    "js-module-threads": true,
};

let config: MonoConfig = undefined as any;
let configLoaded = false;
let isCustomStartup = false;
const afterConfigLoaded = createPromiseController<void>();
const afterInstantiateWasm = createPromiseController<void>();
const beforePreInit = createPromiseController<void>();
const afterPreInit = createPromiseController<void>();
const afterPreRun = createPromiseController<void>();
const beforeOnRuntimeInitialized = createPromiseController<void>();
const afterOnRuntimeInitialized = createPromiseController<void>();
const afterPostRun = createPromiseController<void>();

// we are making emscripten startup async friendly
// emscripten is executing the events without awaiting it and so we need to block progress via PromiseControllers above
export function configure_emscripten_startup(module: DotnetModule, exportedAPI: DotnetPublicAPI): void {
    // these all could be overridden on DotnetModuleConfig, we are chaing them to async below, as opposed to emscripten
    // when user set configSrc or config, we are running our default startup sequence.
    const userInstantiateWasm: undefined | ((imports: WebAssembly.Imports, successCallback: (instance: WebAssembly.Instance, module: WebAssembly.Module) => void) => any) = module.instantiateWasm;
    const userPreInit: (() => void)[] = !module.preInit ? [] : typeof module.preInit === "function" ? [module.preInit] : module.preInit;
    const userPreRun: (() => void)[] = !module.preRun ? [] : typeof module.preRun === "function" ? [module.preRun] : module.preRun as any;
    const userpostRun: (() => void)[] = !module.postRun ? [] : typeof module.postRun === "function" ? [module.postRun] : module.postRun as any;
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    const userOnRuntimeInitialized: () => void = module.onRuntimeInitialized ? module.onRuntimeInitialized : () => { };
    // when assets don't contain DLLs it means this is Blazor or another custom startup
    isCustomStartup = !module.configSrc && (!module.config || !module.config.assets || module.config.assets.findIndex(a => a.behavior === "assembly") != -1); // like blazor

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
    module.ready = module.ready.then(async () => {
        // wait for previous stage
        await afterPostRun.promise;
        // - here we resolve the promise returned by createDotnetRuntime export
        return exportedAPI;
        // - any code after createDotnetRuntime is executed now
    });
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
    if (Module.config) {
        config = runtimeHelpers.config = Module.config as MonoConfig;
    } else {
        config = runtimeHelpers.config = Module.config = {} as any;
    }
    runtimeHelpers.diagnosticTracing = !!config.diagnosticTracing;

    if (userInstantiateWasm) {
        const exports = userInstantiateWasm(imports, (instance: WebAssembly.Instance, module: WebAssembly.Module) => {
            afterInstantiateWasm.promise_control.resolve();
            successCallback(instance, module);
        });
        return exports;
    }

    instantiate_wasm_module(imports, successCallback);
    return []; // No exports
}

function preInit(userPreInit: (() => void)[]) {
    Module.addRunDependency("mono_pre_init");
    try {
        mono_wasm_pre_init_essential();
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
            if (!isCustomStartup) {
                // - download Module.config from configSrc
                // - start download assets like DLLs
                await mono_wasm_pre_init_full();
            }
        } catch (err) {
            abort_startup(err, true);
            throw err;
        }
        // signal next stage
        afterPreInit.promise_control.resolve();
        Module.removeRunDependency("mono_pre_init");
    })();
}

async function preRunAsync(userPreRun: (() => void)[]) {
    Module.addRunDependency("mono_pre_run_async");
    // wait for previous stages
    await afterInstantiateWasm.promise;
    await afterPreInit.promise;
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: preRunAsync");
    try {
        // all user Module.preRun callbacks
        userPreRun.map(fn => fn());
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
    // signal this stage, this will allow pending assets to allocate memory
    beforeOnRuntimeInitialized.promise_control.resolve();
    try {
        if (!isCustomStartup) {
            await wait_for_all_assets();
            // load runtime
            await mono_wasm_before_user_runtime_initialized();
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
        // all user Module.postRun callbacks
        userpostRun.map(fn => fn());
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
    afterInstantiateWasm.promise_control.reject(reason);
    beforePreInit.promise_control.reject(reason);
    afterPreInit.promise_control.reject(reason);
    afterPreRun.promise_control.reject(reason);
    beforeOnRuntimeInitialized.promise_control.reject(reason);
    afterOnRuntimeInitialized.promise_control.reject(reason);
    afterPostRun.promise_control.reject(reason);
    if (should_exit) {
        set_exit_code(1, reason);
    }
}

// runs in both blazor and non-blazor
function mono_wasm_pre_init_essential(): void {
    Module.addRunDependency("mono_wasm_pre_init_essential");
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_pre_init_essential");

    // init_polyfills() is already called from export.ts
    init_c_exports();
    cwraps_internal(INTERNAL);
    cwraps_mono_api(MONO);
    cwraps_binding_api(BINDING);

    Module.removeRunDependency("mono_wasm_pre_init_essential");
}

// runs in both blazor and non-blazor
async function mono_wasm_pre_init_essential_async(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_pre_init_essential_async");
    Module.addRunDependency("mono_wasm_pre_init_essential_async");

    await init_polyfills_async();
    await mono_wasm_load_config(Module.configSrc);
    init_crypto();

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

        if (!runtimeHelpers.mono_wasm_load_runtime_done) mono_wasm_load_runtime("unused", config.debugLevel || 0);
        if (!runtimeHelpers.mono_wasm_runtime_is_ready) mono_wasm_runtime_ready();
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
            await mono_wasm_init_diagnostics("env");
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


export function resolve_asset_path(behavior: AssetBehaviours) {
    const asset: AssetEntry | undefined = runtimeHelpers.config.assets?.find(a => a.behavior == behavior);
    mono_assert(asset, () => `Can't find asset for ${behavior}`);
    if (!asset.resolvedUrl) {
        asset.resolvedUrl = resolve_path(asset, "");
    }
    return asset;
}


async function instantiate_wasm_module(
    imports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
): Promise<void> {
    // this is called so early that even Module exports like addRunDependency don't exist yet
    try {
        await mono_wasm_load_config(Module.configSrc);
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module");
        const assetToLoad = resolve_asset_path("dotnetwasm");
        const pendingAsset = await start_asset_download(assetToLoad);
        await beforePreInit.promise;
        Module.addRunDependency("instantiate_wasm_module");
        instantiate_wasm_asset(pendingAsset!, imports, successCallback);

        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module done");
        afterInstantiateWasm.promise_control.resolve();
    } catch (err) {
        _print_error("MONO_WASM: instantiate_wasm_module() failed", err);
        abort_startup(err, true);
        throw err;
    }
    Module.removeRunDependency("instantiate_wasm_module");
}

export async function instantiate_wasm_asset(
    pendingAsset: AssetEntry,
    wasmModuleImports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
) {
    mono_assert(pendingAsset && pendingAsset.pending, "Can't load dotnet.wasm");
    const response = await pendingAsset.pending.response;
    const contentType = response.headers ? response.headers.get("Content-Type") : undefined;
    let compiledInstance: WebAssembly.Instance;
    let compiledModule: WebAssembly.Module;
    if (typeof WebAssembly.instantiateStreaming === "function" && contentType === "application/wasm") {
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module streaming");
        const streamingResult = await WebAssembly.instantiateStreaming(response, wasmModuleImports!);
        compiledInstance = streamingResult.instance;
        compiledModule = streamingResult.module;
    } else {
        if (ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
            console.warn("MONO_WASM: WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
        }
        const arrayBuffer = await response.arrayBuffer();
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: instantiate_wasm_module buffered");
        const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, wasmModuleImports!);
        compiledInstance = arrayBufferResult.instance;
        compiledModule = arrayBufferResult.module;
    }
    ++instantiated_assets_count;
    successCallback(compiledInstance, compiledModule);
}

// this need to be run only after onRuntimeInitialized event, when the memory is ready
function _instantiate_asset(asset: AssetEntry, url: string, bytes: Uint8Array) {
    if (runtimeHelpers.diagnosticTracing)
        console.debug(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);

    const virtualName: string = typeof (asset.virtualPath) === "string"
        ? asset.virtualPath
        : asset.name;
    let offset: VoidPtr | null = null;

    switch (asset.behavior) {
        case "dotnetwasm":
        case "js-module-crypto":
        case "js-module-threads":
            // do nothing
            break;
        case "resource":
        case "assembly":
        case "pdb":
            loaded_files.push({ url: url, file: virtualName });
        // falls through
        case "heap":
        case "icu":
            offset = mono_wasm_load_bytes_into_heap(bytes);
            loaded_assets[virtualName] = [offset, bytes.length];
            break;

        case "vfs": {
            // FIXME
            const lastSlash = virtualName.lastIndexOf("/");
            let parentDirectory = (lastSlash > 0)
                ? virtualName.substr(0, lastSlash)
                : null;
            let fileName = (lastSlash > 0)
                ? virtualName.substr(lastSlash + 1)
                : virtualName;
            if (fileName.startsWith("/"))
                fileName = fileName.substr(1);
            if (parentDirectory) {
                if (runtimeHelpers.diagnosticTracing)
                    console.debug(`MONO_WASM: Creating directory '${parentDirectory}'`);

                Module.FS_createPath(
                    "/", parentDirectory, true, true // fixme: should canWrite be false?
                );
            } else {
                parentDirectory = "/";
            }

            if (runtimeHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: Creating file '${fileName}' in directory '${parentDirectory}'`);

            if (!mono_wasm_load_data_archive(bytes, parentDirectory)) {
                Module.FS_createDataFile(
                    parentDirectory, fileName,
                    bytes, true /* canRead */, true /* canWrite */, true /* canOwn */
                );
            }
            break;
        }
        default:
            throw new Error(`Unrecognized asset behavior:${asset.behavior}, for asset ${asset.name}`);
    }

    if (asset.behavior === "assembly") {
        // this is reading flag inside the DLL about the existence of PDB
        // it doesn't relate to whether the .pdb file is downloaded at all
        const hasPpdb = cwraps.mono_wasm_add_assembly(virtualName, offset!, bytes.length);

        if (!hasPpdb) {
            const index = loaded_files.findIndex(element => element.file == virtualName);
            loaded_files.splice(index, 1);
        }
    }
    else if (asset.behavior === "icu") {
        if (!mono_wasm_load_icu_data(offset!))
            Module.printErr(`MONO_WASM: Error loading ICU asset ${asset.name}`);
    }
    else if (asset.behavior === "resource") {
        cwraps.mono_wasm_add_satellite_assembly(virtualName, asset.culture!, offset!, bytes.length);
    }
    ++instantiated_assets_count;
}

// runs just in non-blazor
async function _apply_configuration_from_args() {
    try {
        const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
        mono_wasm_setenv("TZ", tz || "UTC");
    } catch {
        mono_wasm_setenv("TZ", "UTC");
    }

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

    if (config.coverageProfilerOptions)
        mono_wasm_init_coverage_profiler(config.coverageProfilerOptions);
    // for non-Blazor, init diagnostics after environment variables are set
    if (MonoWasmThreads) {
        await mono_wasm_init_diagnostics("env");
    }
}

export function mono_wasm_load_runtime(unused?: string, debugLevel?: number): void {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_load_runtime");
    if (runtimeHelpers.mono_wasm_load_runtime_done) {
        return;
    }
    runtimeHelpers.mono_wasm_load_runtime_done = true;
    try {
        if (debugLevel == undefined) {
            debugLevel = 0;
            if (config && config.debugLevel) {
                debugLevel = 0 + debugLevel;
            }
        }
        cwraps.mono_wasm_load_runtime(unused || "unused", debugLevel);
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
        init_managed_exports();
        init_legacy_exports();
        initialize_marshalers_to_js();
        initialize_marshalers_to_cs();
        runtimeHelpers._i52_error_scratch_buffer = <any>Module._malloc(4);

    } catch (err) {
        _print_error("MONO_WASM: Error in bindings_init", err);
        throw err;
    }
}

function downloadResource(request: ResourceRequest): LoadingResource {
    if (typeof Module.downloadResource === "function") {
        const loading = Module.downloadResource(request);
        if (loading) return loading;
    }
    const options: any = {};
    if (request.hash) {
        options.integrity = request.hash;
    }
    const response = runtimeHelpers.fetch_like(request.resolvedUrl!, options);
    return {
        name: request.name, url: request.resolvedUrl!, response
    };
}
async function start_asset_download_sources(asset: AssetEntry): Promise<AssetEntry | undefined> {
    // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
    if (asset.buffer) {
        ++downloded_assets_count;
        const buffer = asset.buffer;
        asset.buffer = undefined;//GC later
        asset.pending = {
            url: "undefined://" + asset.name,
            name: asset.name,
            response: Promise.resolve({
                arrayBuffer: () => buffer,
                headers: {
                    get: () => undefined,
                }
            }) as any
        };
        return Promise.resolve(asset);
    }
    if (asset.pending) {
        ++downloded_assets_count;
        return asset;
    }

    const sourcesList = asset.loadRemote && runtimeHelpers.config.remoteSources ? runtimeHelpers.config.remoteSources : [""];
    let response: Response | undefined = undefined;
    for (let sourcePrefix of sourcesList) {
        sourcePrefix = sourcePrefix.trim();
        // HACK: Special-case because MSBuild doesn't allow "" as an attribute
        if (sourcePrefix === "./")
            sourcePrefix = "";

        const attemptUrl = resolve_path(asset, sourcePrefix);
        if (asset.name === attemptUrl) {
            if (runtimeHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: Attempting to download '${attemptUrl}'`);
        } else {
            if (runtimeHelpers.diagnosticTracing)
                console.debug(`MONO_WASM: Attempting to download '${attemptUrl}' for ${asset.name}`);
        }
        try {
            const loadingResource = downloadResource({
                name: asset.name,
                resolvedUrl: attemptUrl,
                hash: asset.hash,
                behavior: asset.behavior
            });
            asset.pending = loadingResource;
            response = await loadingResource.response;
            if (!response.ok) {
                continue;// next source
            }
            ++downloded_assets_count;
            return asset;
        }
        catch (err) {
            continue; //next source
        }
    }
    throw response;
}

function resolve_path(asset: AssetEntry, sourcePrefix: string): string {
    let attemptUrl;
    const assemblyRootFolder = runtimeHelpers.config.assemblyRootFolder;
    if (!asset.resolvedUrl) {
        if (sourcePrefix === "") {
            if (asset.behavior === "assembly" || asset.behavior === "pdb")
                attemptUrl = assemblyRootFolder + "/" + asset.name;
            else if (asset.behavior === "resource") {
                const path = asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                attemptUrl = assemblyRootFolder + "/" + path;
            }
            else {
                attemptUrl = asset.name;
            }
        } else {
            attemptUrl = sourcePrefix + asset.name;
        }
        attemptUrl = runtimeHelpers.locateFile(attemptUrl);
    }
    else {
        attemptUrl = asset.resolvedUrl;
    }
    return attemptUrl;
}

let throttling: PromiseAndController<void> | undefined;
async function start_asset_download_throttle(asset: AssetEntry): Promise<AssetEntry | undefined> {
    // we don't addRunDependency to allow download in parallel with onRuntimeInitialized event!
    while (throttling) {
        await throttling.promise;
    }
    try {
        ++parallel_count;
        if (parallel_count == runtimeHelpers.maxParallelDownloads) {
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: Throttling further parallel downloads");
            throttling = createPromiseController<void>();
        }
        return await start_asset_download_sources(asset);
    }
    catch (response: any) {
        const isOkToFail = asset.isOptional || (asset.name.match(/\.pdb$/) && runtimeHelpers.config.ignorePdbLoadErrors);
        if (!isOkToFail) {
            const err: any = new Error(`MONO_WASM: download '${response.url}' for ${asset.name} failed ${response.status} ${response.statusText}`);
            err.status = response.status;
            throw err;
        }
    }
    finally {
        --parallel_count;
        if (throttling && parallel_count == runtimeHelpers.maxParallelDownloads - 1) {
            if (runtimeHelpers.diagnosticTracing)
                console.debug("MONO_WASM: Resuming more parallel downloads");
            const old_throttling = throttling;
            throttling = undefined;
            old_throttling.promise_control.resolve();
        }
    }
}

async function start_asset_download(asset: AssetEntry): Promise<AssetEntry | undefined> {
    try {
        return await start_asset_download_throttle(asset);
    } catch (err: any) {
        if (err && err.status == 404) {
            throw err;
        }
        // second attempt only after all first attempts are queued
        await allDownloadsQueued.promise;
        try {
            return await start_asset_download_throttle(asset);
        } catch (err) {
            // third attempt after small delay
            await delay(100);
            return await start_asset_download_throttle(asset);
        }
    }
}

const allDownloadsQueued = createPromiseController<void>();
async function mono_download_assets(): Promise<void> {
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_download_assets");
    runtimeHelpers.maxParallelDownloads = runtimeHelpers.config.maxParallelDownloads || runtimeHelpers.maxParallelDownloads;
    try {
        const download_promises: Promise<AssetEntry | undefined>[] = [];
        // start fetching and instantiating all assets in parallel
        for (const asset of runtimeHelpers.config.assets!) {
            if (!asset.pending && !skipDownloadsAssetTypes[asset.behavior]) {
                download_promises.push(start_asset_download(asset));
            }
        }
        allDownloadsQueued.promise_control.resolve();

        const asset_promises: Promise<void>[] = [];
        for (const downloadPromise of download_promises) {
            const downloadedAsset = await downloadPromise;
            if (downloadedAsset) {
                asset_promises.push((async () => {
                    const url = downloadedAsset.pending!.url;
                    const response = await downloadedAsset.pending!.response;
                    downloadedAsset.pending = undefined; //GC
                    const buffer = await response.arrayBuffer();
                    await beforeOnRuntimeInitialized.promise;
                    // this is after onRuntimeInitialized
                    _instantiate_asset(downloadedAsset, url, new Uint8Array(buffer));
                })());
            }
        }

        // this await will get past the onRuntimeInitialized because we are not blocking via addRunDependency
        // and we are not awating it here
        Promise.all(asset_promises).then(() => all_assets_loaded_in_memory.promise_control.resolve());
        // OPTIMIZATION explained:
        // we do it this way so that we could allocate memory immediately after asset is downloaded (and after onRuntimeInitialized which happened already)
        // spreading in time
        // rather than to block all downloads after onRuntimeInitialized or block onRuntimeInitialized after all downloads are done. That would create allocation burst.
    } catch (err: any) {
        Module.printErr("MONO_WASM: Error in mono_download_assets: " + err);
        throw err;
    }
}

// used from Blazor
export function mono_wasm_load_data_archive(data: Uint8Array, prefix: string): boolean {
    if (data.length < 8)
        return false;

    const dataview = new DataView(data.buffer);
    const magic = dataview.getUint32(0, true);
    //    get magic number
    if (magic != 0x626c6174) {
        return false;
    }
    const manifestSize = dataview.getUint32(4, true);
    if (manifestSize == 0 || data.length < manifestSize + 8)
        return false;

    let manifest;
    try {
        const manifestContent = Module.UTF8ArrayToString(data, 8, manifestSize);
        manifest = JSON.parse(manifestContent);
        if (!(manifest instanceof Array))
            return false;
    } catch (exc) {
        return false;
    }

    data = data.slice(manifestSize + 8);

    // Create the folder structure
    // /usr/share/zoneinfo
    // /usr/share/zoneinfo/Africa
    // /usr/share/zoneinfo/Asia
    // ..

    const folders = new Set<string>();
    manifest.filter(m => {
        const file = m[0];
        const last = file.lastIndexOf("/");
        const directory = file.slice(0, last + 1);
        folders.add(directory);
    });
    folders.forEach(folder => {
        Module["FS_createPath"](prefix, folder, true, true);
    });

    for (const row of manifest) {
        const name = row[0];
        const length = row[1];
        const bytes = data.slice(0, length);
        Module["FS_createDataFile"](prefix, name, bytes, true, true);
        data = data.slice(length);
    }
    return true;
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
        normalize();
        afterConfigLoaded.promise_control.resolve();
        return;
    }
    if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: mono_wasm_load_config");
    try {
        const resolveSrc = runtimeHelpers.locateFile(configFilePath);
        const configResponse = await runtimeHelpers.fetch_like(resolveSrc);
        const configData: MonoConfig = (await configResponse.json()) || {};
        if (configData.environmentVariables && typeof (configData.environmentVariables) !== "object")
            throw new Error("Expected config.environmentVariables to be unset or a dictionary-style object");

        // merge
        configData.assets = [...(config.assets || []), ...(configData.assets || [])];
        configData.environmentVariables = { ...(config.environmentVariables || {}), ...(configData.environmentVariables || {}) };
        config = runtimeHelpers.config = Module.config = Object.assign(Module.config as any, configData);

        normalize();

        if (Module.onConfigLoaded) {
            try {
                await Module.onConfigLoaded(<MonoConfig>runtimeHelpers.config);
                normalize();
            }
            catch (err: any) {
                _print_error("MONO_WASM: onConfigLoaded() failed", err);
                throw err;
            }
        }
        afterConfigLoaded.promise_control.resolve();
    } catch (err) {
        const errMessage = `Failed to load config file ${configFilePath} ${err}`;
        abort_startup(errMessage, true);
        config = runtimeHelpers.config = Module.config = <any>{ message: errMessage, error: err, isError: true };
        throw err;
    }

    function normalize() {
        // normalize
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
///
/// Notes:
/// 1. Emscripten skips a lot of initialization on the pthread workers, Module may not have everything you expect.
/// 2. Emscripten does not run the preInit or preRun functions in the workers.
/// 3. At the point when this executes there is no pthread assigned to the worker yet.
export async function mono_wasm_pthread_worker_init(): Promise<void> {
    console.debug("MONO_WASM: worker initializing essential C exports and APIs");
    // FIXME: copy/pasted from mono_wasm_pre_init_essential - can we share this code? Any other global state that needs initialization?
    init_c_exports();
    // not initializing INTERNAL, MONO, or BINDING C wrappers here - those legacy APIs are not likely to be needed on pthread workers.

    // This is a good place for subsystems to attach listeners for pthreads_worker.currentWorkerThreadEvents
    pthreads_worker.currentWorkerThreadEvents.addEventListener(pthreads_worker.dotnetPthreadCreated, (ev) => {
        console.debug("MONO_WASM: pthread created", ev.pthread_self.pthread_id);
    });

}

/**
* @deprecated
*/
export async function mono_load_runtime_and_bcl_args(cfg?: MonoConfig | MonoConfigError | undefined): Promise<void> {
    config = Module.config = runtimeHelpers.config = Object.assign(runtimeHelpers.config || {}, cfg || {}) as any;
    await mono_download_assets();
    if (!isCustomStartup) {
        await wait_for_all_assets();
    }
}

export async function wait_for_all_assets() {
    // wait for all assets in memory
    await all_assets_loaded_in_memory.promise;
    if (runtimeHelpers.config.assets) {
        const expected_asset_count = runtimeHelpers.config.assets.filter(a => !skipDownloadsAssetTypes[a.behavior]).length;
        mono_assert(downloded_assets_count == expected_asset_count, "Expected assets to be downloaded");
        mono_assert(instantiated_assets_count == expected_asset_count, "Expected assets to be in memory");
        loaded_files.forEach(value => MONO.loaded_files.push(value.url));
        if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: all assets are loaded in wasm memory");
    }
}
