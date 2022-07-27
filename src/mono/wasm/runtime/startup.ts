// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import { mono_assert, CharPtrNull, DotnetModule, MonoConfig, wasm_type_symbol, MonoObject, MonoConfigError, LoadingResource, AssetEntry, ResourceRequest } from "./types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_PTHREAD, ENVIRONMENT_IS_SHELL, INTERNAL, Module, MONO, runtimeHelpers } from "./imports";
import cwraps from "./cwraps";
import { mono_wasm_raise_debug_event, mono_wasm_runtime_ready } from "./debug";
import { mono_wasm_globalization_init, mono_wasm_load_icu_data } from "./icu";
import { toBase64StringImpl } from "./base64";
import { mono_wasm_init_aot_profiler, mono_wasm_init_coverage_profiler } from "./profiler";
import { mono_wasm_load_bytes_into_heap } from "./buffers";
import { bind_runtime_method, get_method, _create_primitive_converters } from "./method-binding";
import { find_corlib_class } from "./class-loader";
import { VoidPtr, CharPtr } from "./types/emscripten";
import { DotnetPublicAPI } from "./exports";
import { mono_on_abort, set_exit_code } from "./run";
import { initialize_marshalers_to_cs } from "./marshal-to-cs";
import { initialize_marshalers_to_js } from "./marshal-to-js";
import { mono_wasm_new_root } from "./roots";
import { init_crypto } from "./crypto-worker";
import { init_polyfills_async } from "./polyfills";
import * as pthreads_worker from "./pthreads/worker";
import { createPromiseController } from "./promise-controller";
import { string_decoder } from "./strings";
import { mono_wasm_init_diagnostics } from "./diagnostics/index";

let all_assets_loaded_in_memory: Promise<void> | null = null;
const loaded_files: { url?: string, file: string }[] = [];
const loaded_assets: { [id: string]: [VoidPtr, number] } = Object.create(null);
let instantiated_assets_count = 0;
let downloded_assets_count = 0;
const max_parallel_downloads = 100;
// in order to prevent net::ERR_INSUFFICIENT_RESOURCES if we start downloading too many files at same time
let parallel_count = 0;
let throttling_promise: Promise<void> | undefined = undefined;
let throttling_promise_resolve: Function | undefined = undefined;
let config: MonoConfig = undefined as any;

const afterInstantiateWasm = createPromiseController();
const beforePreInit = createPromiseController();
const afterPreInit = createPromiseController();
const afterPreRun = createPromiseController();
const beforeOnRuntimeInitialized = createPromiseController();
const afterOnRuntimeInitialized = createPromiseController();
const afterPostRun = createPromiseController();

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
    const isCustomStartup = !module.configSrc && !module.config; // like blazor

    // execution order == [0] ==
    // - default or user Module.instantiateWasm (will start downloading dotnet.wasm)
    module.instantiateWasm = (imports, callback) => instantiateWasm(imports, callback, userInstantiateWasm);
    // execution order == [1] ==
    module.preInit = [() => preInit(isCustomStartup, userPreInit)];
    // execution order == [2] ==
    module.preRun = [() => preRunAsync(userPreRun)];
    // execution order == [4] ==
    module.onRuntimeInitialized = () => onRuntimeInitializedAsync(isCustomStartup, userOnRuntimeInitialized);
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

let wasm_module_imports: WebAssembly.Imports | null = null;
let wasm_success_callback: null | ((instance: WebAssembly.Instance, module: WebAssembly.Module) => void) = null;

function instantiateWasm(
    imports: WebAssembly.Imports,
    successCallback: (instance: WebAssembly.Instance, module: WebAssembly.Module) => void,
    userInstantiateWasm?: (imports: WebAssembly.Imports, successCallback: (instance: WebAssembly.Instance, module: WebAssembly.Module) => void) => any): any[] {
    // this is called so early that even Module exports like addRunDependency don't exist yet

    if (!Module.configSrc && !Module.config && !userInstantiateWasm) {
        Module.print("MONO_WASM: configSrc nor config was specified");
    }
    if (Module.config) {
        config = runtimeHelpers.config = Module.config as MonoConfig;
    } else {
        config = runtimeHelpers.config = Module.config = {} as any;
    }
    runtimeHelpers.diagnostic_tracing = !!config.diagnostic_tracing;
    runtimeHelpers.enable_debugging = config.enable_debugging ? config.enable_debugging : 0;
    if (!config.assets) {
        config.assets = [];
    }

    if (userInstantiateWasm) {
        const exports = userInstantiateWasm(imports, (instance: WebAssembly.Instance, module: WebAssembly.Module) => {
            afterInstantiateWasm.promise_control.resolve(null);
            successCallback(instance, module);
        });
        return exports;
    }

    wasm_module_imports = imports;
    wasm_success_callback = successCallback;
    _instantiate_wasm_module();
    return []; // No exports
}

function preInit(isCustomStartup: boolean, userPreInit: (() => void)[]) {
    Module.addRunDependency("mono_pre_init");
    try {
        mono_wasm_pre_init_essential();
        if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: preInit");
        beforePreInit.promise_control.resolve(null);
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
        afterPreInit.promise_control.resolve(null);
        Module.removeRunDependency("mono_pre_init");
    })();
}

async function preRunAsync(userPreRun: (() => void)[]) {
    Module.addRunDependency("mono_pre_run_async");
    // wait for previous stages
    await afterInstantiateWasm.promise;
    await afterPreInit.promise;
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: preRunAsync");
    try {
        // all user Module.preRun callbacks
        userPreRun.map(fn => fn());
    } catch (err) {
        _print_error("MONO_WASM: user callback preRun() failed", err);
        abort_startup(err, true);
        throw err;
    }
    // signal next stage
    afterPreRun.promise_control.resolve(null);
    Module.removeRunDependency("mono_pre_run_async");
}

async function onRuntimeInitializedAsync(isCustomStartup: boolean, userOnRuntimeInitialized: () => void) {
    // wait for previous stage
    await afterPreRun.promise;
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: onRuntimeInitialized");
    // signal this stage, this will allow pending assets to allocate memory
    beforeOnRuntimeInitialized.promise_control.resolve(null);
    try {
        if (!isCustomStartup) {
            // wait for all assets in memory
            await all_assets_loaded_in_memory;
            const expected_asset_count = config.assets ? config.assets.length : 0;
            mono_assert(downloded_assets_count == expected_asset_count, "Expected assets to be downloaded");
            mono_assert(instantiated_assets_count == expected_asset_count, "Expected assets to be in memory");
            if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: all assets are loaded in wasm memory");

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
    afterOnRuntimeInitialized.promise_control.resolve(null);
}

async function postRunAsync(userpostRun: (() => void)[]) {
    // wait for previous stage
    await afterOnRuntimeInitialized.promise;
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: postRunAsync");
    try {
        // all user Module.postRun callbacks
        userpostRun.map(fn => fn());
    } catch (err) {
        _print_error("MONO_WASM: user callback posRun() failed", err);
        abort_startup(err, true);
        throw err;
    }
    // signal next stage
    afterPostRun.promise_control.resolve(null);
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function abort_startup(reason: any, should_exit: boolean): void {
    if (runtimeHelpers.diagnostic_tracing) console.trace("MONO_WASM: abort_startup");
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
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_pre_init_essential");

    // init_polyfills() is already called from export.ts
    init_crypto();

    Module.removeRunDependency("mono_wasm_pre_init_essential");
}

// runs in both blazor and non-blazor
async function mono_wasm_pre_init_essential_async(): Promise<void> {
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_pre_init_essential_async");
    Module.addRunDependency("mono_wasm_pre_init_essential_async");

    await init_polyfills_async();
    if (MonoWasmThreads && ENVIRONMENT_IS_PTHREAD) {
        await mono_wasm_pthread_worker_init();
    }

    Module.removeRunDependency("mono_wasm_pre_init_essential_async");
}

// runs just in non-blazor
async function mono_wasm_pre_init_full(): Promise<void> {
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_pre_init_full");
    Module.addRunDependency("mono_wasm_pre_init_full");

    if (Module.configSrc) {
        await mono_wasm_load_config(Module.configSrc);
    }
    await mono_download_assets();

    Module.removeRunDependency("mono_wasm_pre_init_full");
}

// runs just in non-blazor
async function mono_wasm_before_user_runtime_initialized(): Promise<void> {
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_before_user_runtime_initialized");

    if (!Module.config || Module.config.isError) {
        return;
    }

    try {
        loaded_files.forEach(value => MONO.loaded_files.push(value.url));
        if (!loaded_files || loaded_files.length == 0) {
            Module.print("MONO_WASM: no files were loaded into runtime");
        }

        await _apply_configuration_from_args();
        mono_wasm_globalization_init();

        if (!runtimeHelpers.mono_wasm_load_runtime_done) mono_wasm_load_runtime("unused", config.debug_level || 0);
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
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_after_user_runtime_initialized");
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

        if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: Initializing mono runtime");

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
        throw new Error("Expected runtime_options to be an array of strings");

    const argv = Module._malloc(options.length * 4);
    let aindex = 0;
    for (let i = 0; i < options.length; ++i) {
        const option = options[i];
        if (typeof (option) !== "string")
            throw new Error("Expected runtime_options to be an array of strings");
        Module.setValue(<any>argv + (aindex * 4), cwraps.mono_wasm_strdup(option), "i32");
        aindex += 1;
    }
    cwraps.mono_wasm_parse_runtime_options(options.length, argv);
}


async function _instantiate_wasm_module(): Promise<void> {
    // this is called so early that even Module exports like addRunDependency don't exist yet
    try {
        if (!config.assets && Module.configSrc) {
            // when we are starting with mono-config,json, it could have dotnet.wasm location in it, we have to wait for it
            await mono_wasm_load_config(Module.configSrc);
        }
        if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: instantiateWasm");
        let assetToLoad: AssetEntry = {
            name: "dotnet.wasm",
            behavior: "dotnetwasm"
        };
        const assetfromConfig = config.assets!.find(a => a.behavior === "dotnetwasm");
        if (assetfromConfig) {
            assetToLoad = assetfromConfig;
        } else {
            config.assets!.push(assetToLoad);
        }

        const pendingAsset = await start_asset_download(assetToLoad);
        await beforePreInit.promise;
        Module.addRunDependency("_instantiate_wasm_module");
        mono_assert(pendingAsset && pendingAsset.pending, () => `Can't load ${assetToLoad.name}`);

        const response = await pendingAsset.pending.response;
        const contentType = response.headers ? response.headers.get("Content-Type") : undefined;
        let compiledInstance: WebAssembly.Instance;
        let compiledModule: WebAssembly.Module;
        if (typeof WebAssembly.instantiateStreaming === "function" && contentType === "application/wasm") {
            if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_after_user_runtime_initialized streaming");
            const streamingResult = await WebAssembly.instantiateStreaming(response, wasm_module_imports!);
            compiledInstance = streamingResult.instance;
            compiledModule = streamingResult.module;
        } else {
            const arrayBuffer = await response.arrayBuffer();
            if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_after_user_runtime_initialized streaming");
            const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, wasm_module_imports!);
            compiledInstance = arrayBufferResult.instance;
            compiledModule = arrayBufferResult.module;
        }
        ++instantiated_assets_count;
        wasm_success_callback!(compiledInstance, compiledModule);
        if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: instantiateWasm done");
        afterInstantiateWasm.promise_control.resolve(null);
        wasm_success_callback = null;
        wasm_module_imports = null;
    } catch (err) {
        _print_error("MONO_WASM: _instantiate_wasm_module() failed", err);
        abort_startup(err, true);
        throw err;
    }
    Module.removeRunDependency("_instantiate_wasm_module");
}

// this need to be run only after onRuntimeInitialized event, when the memory is ready
function _instantiate_asset(asset: AssetEntry, url: string, bytes: Uint8Array) {
    if (runtimeHelpers.diagnostic_tracing)
        console.debug(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);

    const virtualName: string = typeof (asset.virtual_path) === "string"
        ? asset.virtual_path
        : asset.name;
    let offset: VoidPtr | null = null;

    switch (asset.behavior) {
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
                if (runtimeHelpers.diagnostic_tracing)
                    console.debug(`MONO_WASM: Creating directory '${parentDirectory}'`);

                Module.FS_createPath(
                    "/", parentDirectory, true, true // fixme: should canWrite be false?
                );
            } else {
                parentDirectory = "/";
            }

            if (runtimeHelpers.diagnostic_tracing)
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

    for (const k in config.environment_variables) {
        const v = config.environment_variables![k];
        if (typeof (v) === "string")
            mono_wasm_setenv(k, v);
        else
            throw new Error(`Expected environment variable '${k}' to be a string but it was ${typeof v}: '${v}'`);
    }

    if (config.runtime_options)
        mono_wasm_set_runtime_options(config.runtime_options);

    if (config.aot_profiler_options)
        mono_wasm_init_aot_profiler(config.aot_profiler_options);

    if (config.coverage_profiler_options)
        mono_wasm_init_coverage_profiler(config.coverage_profiler_options);

    if (config.diagnostic_options) {
        await mono_wasm_init_diagnostics(config.diagnostic_options);
    }
}

export function mono_wasm_load_runtime(unused?: string, debug_level?: number): void {
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_load_runtime");
    if (runtimeHelpers.mono_wasm_load_runtime_done) {
        return;
    }
    runtimeHelpers.mono_wasm_load_runtime_done = true;
    try {
        if (debug_level == undefined) {
            debug_level = 0;
            if (config && config.debug_level) {
                debug_level = 0 + debug_level;
            }
        }
        cwraps.mono_wasm_load_runtime(unused || "unused", debug_level);
        runtimeHelpers.wait_for_debugger = config.wait_for_debugger;

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
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: bindings_init");
    if (runtimeHelpers.mono_wasm_bindings_is_ready) {
        return;
    }
    runtimeHelpers.mono_wasm_bindings_is_ready = true;
    try {

        // please keep System.Runtime.InteropServices.JavaScript.JSHostImplementation.MappedType in sync
        (<any>Object.prototype)[wasm_type_symbol] = 0;
        (<any>Array.prototype)[wasm_type_symbol] = 1;
        (<any>ArrayBuffer.prototype)[wasm_type_symbol] = 2;
        (<any>DataView.prototype)[wasm_type_symbol] = 3;
        (<any>Function.prototype)[wasm_type_symbol] = 4;
        (<any>Uint8Array.prototype)[wasm_type_symbol] = 11;

        runtimeHelpers._box_buffer_size = 65536;
        runtimeHelpers._unbox_buffer_size = 65536;
        runtimeHelpers._box_buffer = Module._malloc(runtimeHelpers._box_buffer_size);
        runtimeHelpers._unbox_buffer = Module._malloc(runtimeHelpers._unbox_buffer_size);
        runtimeHelpers._i52_error_scratch_buffer = <any>Module._malloc(4);
        runtimeHelpers._class_int32 = find_corlib_class("System", "Int32");
        runtimeHelpers._class_uint32 = find_corlib_class("System", "UInt32");
        runtimeHelpers._class_double = find_corlib_class("System", "Double");
        runtimeHelpers._class_boolean = find_corlib_class("System", "Boolean");
        runtimeHelpers.bind_runtime_method = bind_runtime_method;

        const bindingAssembly = INTERNAL.BINDING_ASM;
        const binding_fqn_asm = bindingAssembly.substring(bindingAssembly.indexOf("[") + 1, bindingAssembly.indexOf("]")).trim();
        const binding_fqn_class = bindingAssembly.substring(bindingAssembly.indexOf("]") + 1).trim();

        const binding_module = cwraps.mono_wasm_assembly_load(binding_fqn_asm);
        if (!binding_module)
            throw "Can't find bindings module assembly: " + binding_fqn_asm;

        if (binding_fqn_class && binding_fqn_class.length) {
            runtimeHelpers.runtime_interop_exports_classname = binding_fqn_class;
            if (binding_fqn_class.indexOf(".") != -1) {
                const idx = binding_fqn_class.lastIndexOf(".");
                runtimeHelpers.runtime_interop_namespace = binding_fqn_class.substring(0, idx);
                runtimeHelpers.runtime_interop_exports_classname = binding_fqn_class.substring(idx + 1);
            }
        }

        runtimeHelpers.runtime_interop_exports_class = cwraps.mono_wasm_assembly_find_class(binding_module, runtimeHelpers.runtime_interop_namespace, runtimeHelpers.runtime_interop_exports_classname);
        if (!runtimeHelpers.runtime_interop_exports_class)
            throw "Can't find " + binding_fqn_class + " class";

        runtimeHelpers.get_call_sig_ref = get_method("GetCallSignatureRef");
        if (!runtimeHelpers.get_call_sig_ref)
            throw "Can't find GetCallSignatureRef method";

        runtimeHelpers.complete_task_method = get_method("CompleteTask");
        if (!runtimeHelpers.complete_task_method)
            throw "Can't find CompleteTask method";

        runtimeHelpers.create_task_method = get_method("CreateTaskCallback");
        if (!runtimeHelpers.create_task_method)
            throw "Can't find CreateTaskCallback method";

        runtimeHelpers.call_delegate = get_method("CallDelegate");
        if (!runtimeHelpers.call_delegate)
            throw "Can't find CallDelegate method";

        initialize_marshalers_to_js();
        initialize_marshalers_to_cs();

        _create_primitive_converters();

        runtimeHelpers._box_root = mono_wasm_new_root<MonoObject>();
        runtimeHelpers._null_root = mono_wasm_new_root<MonoObject>();
    } catch (err) {
        _print_error("MONO_WASM: Error in bindings_init", err);
        throw err;
    }
}

function downloadResource(request: ResourceRequest): LoadingResource {
    if (typeof Module.downloadResource === "function") {
        return Module.downloadResource(request);
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

async function start_asset_download(asset: AssetEntry): Promise<AssetEntry | undefined> {
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

    while (throttling_promise) {
        await throttling_promise;
    }
    ++parallel_count;
    if (parallel_count == max_parallel_downloads) {
        if (runtimeHelpers.diagnostic_tracing)
            console.debug("MONO_WASM: Throttling further parallel downloads");

        throttling_promise = new Promise((resolve) => {
            throttling_promise_resolve = resolve;
        });
    }

    const sourcesList = asset.load_remote && config.remote_sources ? config.remote_sources : [""];

    let error = undefined;
    let result: AssetEntry | undefined = undefined;
    for (let sourcePrefix of sourcesList) {
        sourcePrefix = sourcePrefix.trim();
        // HACK: Special-case because MSBuild doesn't allow "" as an attribute
        if (sourcePrefix === "./")
            sourcePrefix = "";

        let attemptUrl;
        if (!asset.resolvedUrl) {
            if (sourcePrefix === "") {
                if (asset.behavior === "assembly" || asset.behavior === "pdb")
                    attemptUrl = config.assembly_root + "/" + asset.name;
                else if (asset.behavior === "resource") {
                    const path = asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                    attemptUrl = config.assembly_root + "/" + path;
                }
                else
                    attemptUrl = asset.name;
            } else {
                attemptUrl = sourcePrefix + asset.name;
            }
            attemptUrl = runtimeHelpers.locateFile(attemptUrl);
        }
        else {
            attemptUrl = asset.resolvedUrl;
        }
        if (asset.name === attemptUrl) {
            if (runtimeHelpers.diagnostic_tracing)
                console.debug(`MONO_WASM: Attempting to download '${attemptUrl}'`);
        } else {
            if (runtimeHelpers.diagnostic_tracing)
                console.debug(`MONO_WASM: Attempting to download '${attemptUrl}' for ${asset.name}`);
        }
        try {
            const loadingResource = downloadResource({
                name: asset.name,
                resolvedUrl: attemptUrl,
                hash: asset.hash,
                behavior: asset.behavior
            });
            const response = await loadingResource.response;
            if (!response.ok) {
                error = new Error(`MONO_WASM: download '${attemptUrl}' for ${asset.name} failed ${response.status} ${response.statusText}`);
                continue;// next source
            }
            asset.pending = loadingResource;
            result = asset;
            ++downloded_assets_count;
            error = undefined;
        }
        catch (err) {
            error = new Error(`MONO_WASM: download '${attemptUrl}' for ${asset.name} failed ${err}`);
            continue; //next source
        }

        if (!error) {
            break; // this source worked, stop searching
        }
    }

    --parallel_count;
    if (throttling_promise && parallel_count == ((max_parallel_downloads / 2) | 0)) {
        if (runtimeHelpers.diagnostic_tracing)
            console.debug("MONO_WASM: Resuming more parallel downloads");
        throttling_promise_resolve!();
        throttling_promise = undefined;
    }

    if (error) {
        const isOkToFail = asset.is_optional || (asset.name.match(/\.pdb$/) && config.ignore_pdb_load_errors);
        if (!isOkToFail)
            throw error;
    }

    return result;
}

async function mono_download_assets(): Promise<void> {
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_download_assets");
    try {
        const asset_promises: Promise<void>[] = [];

        // start fetching and instantiating all assets in parallel
        for (const asset of config.assets || []) {
            if (asset.behavior != "dotnetwasm") {
                const downloadedAsset = await start_asset_download(asset);
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
        }

        // this await will get past the onRuntimeInitialized because we are not blocking via addRunDependency
        // and we are not awating it here
        all_assets_loaded_in_memory = Promise.all(asset_promises) as any;
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

let configLoaded = false;
/**
 * Loads the mono config file (typically called mono-config.json) asynchroniously
 * Note: the run dependencies are so emsdk actually awaits it in order.
 *
 * @param {string} configFilePath - relative path to the config file
 * @throws Will throw an error if the config file loading fails
 */
export async function mono_wasm_load_config(configFilePath: string): Promise<void> {
    if (configLoaded) {
        return;
    }
    if (runtimeHelpers.diagnostic_tracing) console.debug("MONO_WASM: mono_wasm_load_config");
    try {
        const resolveSrc = runtimeHelpers.locateFile(configFilePath);
        const configResponse = await runtimeHelpers.fetch_like(resolveSrc);
        const configData: MonoConfig = (await configResponse.json()) || {};
        // merge
        configData.assets = [...(config.assets || []), ...(configData.assets || [])];
        config = runtimeHelpers.config = Module.config = Object.assign(Module.config as any, configData);

        // normalize
        config.environment_variables = config.environment_variables || {};
        config.assets = config.assets || [];
        config.runtime_options = config.runtime_options || [];
        config.globalization_mode = config.globalization_mode || "auto";
        if (config.enable_debugging)
            config.debug_level = config.enable_debugging;

        if (typeof (config.environment_variables) !== "object")
            throw new Error("Expected config.environment_variables to be unset or a dictionary-style object");

        if (Module.onConfigLoaded) {
            try {
                await Module.onConfigLoaded(<MonoConfig>runtimeHelpers.config);
            }
            catch (err: any) {
                _print_error("MONO_WASM: onConfigLoaded() failed", err);
                throw err;
            }
        }
        runtimeHelpers.diagnostic_tracing = !!runtimeHelpers.config.diagnostic_tracing;
        runtimeHelpers.enable_debugging = runtimeHelpers.config.enable_debugging ? runtimeHelpers.config.enable_debugging : 0;
        configLoaded = true;
    } catch (err) {
        const errMessage = `Failed to load config file ${configFilePath} ${err}`;
        abort_startup(errMessage, true);
        config = runtimeHelpers.config = Module.config = <any>{ message: errMessage, error: err, isError: true };
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
///
/// Notes:
/// 1. Emscripten skips a lot of initialization on the pthread workers, Module may not have everything you expect.
/// 2. Emscripten does not run the preInit or preRun functions in the workers.
/// 3. At the point when this executes there is no pthread assigned to the worker yet.
async function mono_wasm_pthread_worker_init(): Promise<void> {
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
    await all_assets_loaded_in_memory;
}
