// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { INTERNAL, Module, MONO, runtimeHelpers } from "./modules";
import { AssetEntry, CharPtr, CharPtrNull, EmscriptenModuleMono, GlobalizationMode, MonoConfig, TypedArray, VoidPtr, wasm_type_symbol } from "./types";
import cwraps from "./cwraps";
import { mono_wasm_raise_debug_event, mono_wasm_runtime_ready } from "./debug";
import { mono_wasm_globalization_init, mono_wasm_load_icu_data } from "./icu";
import { toBase64StringImpl } from "./base64";
import { mono_wasm_init_aot_profiler, mono_wasm_init_coverage_profiler } from "./profiler";
import { mono_wasm_load_bytes_into_heap } from "./buffers";
import { bind_runtime_method, get_method, _create_primitive_converters } from "./method-binding";
import { find_corlib_class } from "./class-loader";

export async function mono_wasm_pre_init(): Promise<void> {
    const moduleExt = Module as EmscriptenModuleMono;
    if (moduleExt.configSrc) {
        // sets MONO.config implicitly
        await mono_wasm_load_config(moduleExt.configSrc);

        if (moduleExt.onConfigLoaded) {
            try {
                moduleExt.onConfigLoaded();
            }
            catch (err: any) {
                Module.printErr("MONO_WASM: onConfigLoaded () failed: " + err);
                Module.printErr("MONO_WASM: Stacktrace: \n");
                Module.printErr(err.stack);
                throw err;
            }
        }
    }
}

export function mono_wasm_on_runtime_initialized(): void {
    const moduleExt = Module as EmscriptenModuleMono;
    if (!moduleExt.config || moduleExt.config.isError) {
        return;
    }
    mono_load_runtime_and_bcl_args(moduleExt.config);
}

// Set environment variable NAME to VALUE
// Should be called before mono_load_runtime_and_bcl () in most cases
export function mono_wasm_setenv(name: string, value: string): void {
    cwraps.mono_wasm_setenv(name, value);
}

export function mono_wasm_set_runtime_options(options: string[]): void {
    const argv = Module._malloc(options.length * 4);
    let aindex = 0;
    for (let i = 0; i < options.length; ++i) {
        Module.setValue(<any>argv + (aindex * 4), cwraps.mono_wasm_strdup(options[i]), "i32");
        aindex += 1;
    }
    cwraps.mono_wasm_parse_runtime_options(options.length, argv);
}

function _handle_loaded_asset(ctx: MonoInitContext, asset: AssetEntry, url: string, blob: ArrayBuffer) {
    const bytes = new Uint8Array(blob);
    if (ctx.tracing)
        console.log(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);

    const virtualName: string = asset.virtual_path || asset.name;
    let offset: VoidPtr | null = null;

    switch (asset.behavior) {
        case "resource":
        case "assembly":
            ctx.loaded_files.push({ url: url, file: virtualName });
        // falls through
        case "heap":
        case "icu":
            offset = mono_wasm_load_bytes_into_heap(bytes);
            ctx.loaded_assets[virtualName] = [offset, bytes.length];
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
                if (ctx.tracing)
                    console.log(`MONO_WASM: Creating directory '${parentDirectory}'`);

                ctx.createPath(
                    "/", parentDirectory, true, true // fixme: should canWrite be false?
                );
            } else {
                parentDirectory = "/";
            }

            if (ctx.tracing)
                console.log(`MONO_WASM: Creating file '${fileName}' in directory '${parentDirectory}'`);

            if (!mono_wasm_load_data_archive(bytes, parentDirectory)) {
                ctx.createDataFile(
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
            const index = ctx.loaded_files.findIndex(element => element.file == virtualName);
            ctx.loaded_files.splice(index, 1);
        }
    }
    else if (asset.behavior === "icu") {
        if (!mono_wasm_load_icu_data(offset!))
            console.error(`MONO_WASM: Error loading ICU asset ${asset.name}`);
    }
    else if (asset.behavior === "resource") {
        cwraps.mono_wasm_add_satellite_assembly(virtualName, asset.culture!, offset!, bytes.length);
    }
}

// Initializes the runtime and loads assemblies, debug information, and other files.
export function mono_load_runtime_and_bcl_args(args: MonoConfig): void {
    try {
        return _load_assets_and_runtime(args);
    } catch (exc: any) {
        console.error("MONO_WASM: Error in mono_load_runtime_and_bcl_args:", exc);
        throw exc;
    }
}

function _apply_configuration_from_args(args: MonoConfig) {
    for (const k in (args.environment_variables || {}))
        mono_wasm_setenv(k, args.environment_variables![k]);

    if (args.runtime_options)
        mono_wasm_set_runtime_options(args.runtime_options);

    if (args.aot_profiler_options)
        mono_wasm_init_aot_profiler(args.aot_profiler_options);

    if (args.coverage_profiler_options)
        mono_wasm_init_coverage_profiler(args.coverage_profiler_options);
}

function _get_fetch_file_cb_from_args(args: MonoConfig): (asset: string) => Promise<Response> {
    if (typeof (args.fetch_file_cb) === "function")
        return args.fetch_file_cb;

    if (typeof (fetch) === "function") {
        return function (asset) {
            return fetch(asset, { credentials: "same-origin" });
        };
    } else if (ENVIRONMENT_IS_NODE || typeof (read) === "function") {
        return async function (asset) {
            let data: any = null;
            let err: any = null;
            try {
                if (ENVIRONMENT_IS_NODE) {
                    // eslint-disable-next-line @typescript-eslint/no-var-requires
                    const fs = require("fs");
                    data = await fs.promises.readFile(asset);
                }
                else {
                    data = read(asset, "binary");
                }
            }
            catch (exc) {
                data = null;
                err = exc;
            }
            const res: any = {
                ok: !!data,
                url: asset,
                arrayBuffer: async function () {
                    if (err) throw err;
                    return new Uint8Array(data);
                }
            };
            return <Response>res;
        };
    } else {
        throw new Error("No fetch_file_cb was provided and this environment does not expose 'fetch'.");
    }
}

function _finalize_startup(args: MonoConfig, ctx: MonoInitContext) {
    const moduleExt = Module as EmscriptenModuleMono;

    ctx.loaded_files.forEach(value => MONO.loaded_files.push(value.url));
    if (ctx.tracing) {
        console.log("MONO_WASM: loaded_assets: " + JSON.stringify(ctx.loaded_assets));
        console.log("MONO_WASM: loaded_files: " + JSON.stringify(ctx.loaded_files));
    }

    console.debug("MONO_WASM: Initializing mono runtime");

    mono_wasm_globalization_init(args.globalization_mode!);

    if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
        try {
            cwraps.mono_wasm_load_runtime("unused", args.debug_level || 0);
        } catch (ex: any) {
            Module.printErr("MONO_WASM: mono_wasm_load_runtime () failed: " + ex);
            Module.printErr("MONO_WASM: Stacktrace: \n");
            Module.printErr(ex.stack);

            const wasm_exit = cwraps.mono_wasm_exit;
            wasm_exit(1);
        }
    } else {
        cwraps.mono_wasm_load_runtime("unused", args.debug_level || 0);
    }

    bindings_lazy_init();

    let tz;
    try {
        tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
    } catch {
        //swallow
    }
    mono_wasm_setenv("TZ", tz || "UTC");
    mono_wasm_runtime_ready();

    if (moduleExt.onDotNetReady) {
        try {
            moduleExt.onDotNetReady();
        }
        catch (err: any) {
            Module.printErr("MONO_WASM: onDotNetReady () failed: " + err);
            Module.printErr("MONO_WASM: Stacktrace: \n");
            Module.printErr(err.stack);
            throw err;
        }
    }
}

export function bindings_lazy_init(): void {
    if (runtimeHelpers.mono_wasm_bindings_is_ready)
        return;
    runtimeHelpers.mono_wasm_bindings_is_ready = true;

    // please keep System.Runtime.InteropServices.JavaScript.Runtime.MappedType in sync
    (<any>Object.prototype)[wasm_type_symbol] = 0;
    (<any>Array.prototype)[wasm_type_symbol] = 1;
    (<any>ArrayBuffer.prototype)[wasm_type_symbol] = 2;
    (<any>DataView.prototype)[wasm_type_symbol] = 3;
    (<any>Function.prototype)[wasm_type_symbol] = 4;
    (<any>Map.prototype)[wasm_type_symbol] = 5;
    if (typeof SharedArrayBuffer !== "undefined")
        (<any>SharedArrayBuffer.prototype)[wasm_type_symbol] = 6;
    (<any>Int8Array.prototype)[wasm_type_symbol] = 10;
    (<any>Uint8Array.prototype)[wasm_type_symbol] = 11;
    (<any>Uint8ClampedArray.prototype)[wasm_type_symbol] = 12;
    (<any>Int16Array.prototype)[wasm_type_symbol] = 13;
    (<any>Uint16Array.prototype)[wasm_type_symbol] = 14;
    (<any>Int32Array.prototype)[wasm_type_symbol] = 15;
    (<any>Uint32Array.prototype)[wasm_type_symbol] = 16;
    (<any>Float32Array.prototype)[wasm_type_symbol] = 17;
    (<any>Float64Array.prototype)[wasm_type_symbol] = 18;

    runtimeHelpers._box_buffer_size = 65536;
    runtimeHelpers._unbox_buffer_size = 65536;
    runtimeHelpers._box_buffer = Module._malloc(runtimeHelpers._box_buffer_size);
    runtimeHelpers._unbox_buffer = Module._malloc(runtimeHelpers._unbox_buffer_size);
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
        runtimeHelpers.runtime_classname = binding_fqn_class;
        if (binding_fqn_class.indexOf(".") != -1) {
            const idx = binding_fqn_class.lastIndexOf(".");
            runtimeHelpers.runtime_namespace = binding_fqn_class.substring(0, idx);
            runtimeHelpers.runtime_classname = binding_fqn_class.substring(idx + 1);
        }
    }

    runtimeHelpers.wasm_runtime_class = cwraps.mono_wasm_assembly_find_class(binding_module, runtimeHelpers.runtime_namespace, runtimeHelpers.runtime_classname);
    if (!runtimeHelpers.wasm_runtime_class)
        throw "Can't find " + binding_fqn_class + " class";

    runtimeHelpers.get_call_sig = get_method("GetCallSignature");
    if (!runtimeHelpers.get_call_sig)
        throw "Can't find GetCallSignature method";

    _create_primitive_converters();
}
function _load_assets_and_runtime(args: MonoConfig) {
    if (args.enable_debugging)
        args.debug_level = args.enable_debugging;

    const ctx: MonoInitContext = {
        tracing: args.diagnostic_tracing || false,
        pending_count: args.assets.length,
        loaded_assets: Object.create(null),
        // dlls and pdbs, used by blazor and the debugger
        loaded_files: [],
        createPath: Module.FS_createPath,
        createDataFile: Module.FS_createDataFile
    };

    if (ctx.tracing)
        console.log("MONO_WASM: mono_wasm_load_runtime_with_args", JSON.stringify(args));

    _apply_configuration_from_args(args);

    const fetch_file_cb = _get_fetch_file_cb_from_args(args);

    const onPendingRequestComplete = function () {
        --ctx.pending_count;

        if (ctx.pending_count === 0) {
            try {
                _finalize_startup(args, ctx);
            } catch (exc: any) {
                console.error("MONO_WASM: Unhandled exception in _finalize_startup", exc);
                console.error(exc.stack);
                throw exc;
            }
        }
    };

    const processFetchResponseBuffer = function (asset: AssetEntry, url: string, buffer: ArrayBuffer) {
        try {
            _handle_loaded_asset(ctx, asset, url, buffer);
        } catch (exc) {
            console.error(`MONO_WASM: Unhandled exception in processFetchResponseBuffer ${url} ${exc}`);
            throw exc;
        } finally {
            onPendingRequestComplete();
        }
    };

    args.assets.forEach(function (asset: AssetEntry) {
        let sourceIndex = 0;
        const sourcesList = asset.load_remote ? args.remote_sources! : [""];

        const handleFetchResponse = function (response: Response) {
            if (!response.ok) {
                try {
                    attemptNextSource();
                    return;
                } catch (exc) {
                    console.error(`MONO_WASM: Unhandled exception in handleFetchResponse attemptNextSource for asset ${asset.name} ${exc}`);
                    throw exc;
                }
            }

            try {
                const bufferPromise = response.arrayBuffer();
                bufferPromise.then((data) => processFetchResponseBuffer(asset, response.url, data));
            } catch (exc) {
                console.error(`MONO_WASM: Unhandled exception in handleFetchResponse for asset ${asset.name} ${exc}`);
                attemptNextSource();
            }
        };

        const attemptNextSource = function () {
            if (sourceIndex >= sourcesList.length) {
                const msg = `MONO_WASM: Failed to load ${asset.name}`;
                try {
                    const isOk = asset.is_optional ||
                        (asset.name.match(/\.pdb$/) && args.ignore_pdb_load_errors);

                    if (isOk)
                        console.debug(msg);
                    else {
                        console.error(msg);
                        throw new Error(msg);
                    }
                } finally {
                    onPendingRequestComplete();
                }
            }

            let sourcePrefix = sourcesList[sourceIndex];
            sourceIndex++;

            // HACK: Special-case because MSBuild doesn't allow "" as an attribute
            if (sourcePrefix === "./")
                sourcePrefix = "";

            let attemptUrl;
            if (sourcePrefix.trim() === "") {
                if (asset.behavior === "assembly")
                    attemptUrl = locateFile(args.assembly_root + "/" + asset.name);
                else if (asset.behavior === "resource") {
                    const path = asset.culture !== "" ? `${asset.culture}/${asset.name}` : asset.name;
                    attemptUrl = locateFile(args.assembly_root + "/" + path);
                }
                else
                    attemptUrl = asset.name;
            } else {
                attemptUrl = sourcePrefix + asset.name;
            }

            try {
                if (asset.name === attemptUrl) {
                    if (ctx.tracing)
                        console.log(`MONO_WASM: Attempting to fetch '${attemptUrl}'`);
                } else {
                    if (ctx.tracing)
                        console.log(`MONO_WASM: Attempting to fetch '${attemptUrl}' for ${asset.name}`);
                }
                const fetch_promise = fetch_file_cb(attemptUrl);
                fetch_promise.then(handleFetchResponse);
            } catch (exc) {
                console.error(`MONO_WASM: Error fetching ${attemptUrl} ${exc}`);
                attemptNextSource();
            }
        };

        attemptNextSource();
    });
}

// used from ASP.NET
export function mono_wasm_load_data_archive(data: TypedArray, prefix: string): boolean {
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
export async function mono_wasm_load_config(configFilePath: string): Promise<void> {
    const module = Module;
    module.addRunDependency(configFilePath);
    try {
        let config = null;
        // NOTE: when we add nodejs make sure to include the nodejs fetch package
        if (ENVIRONMENT_IS_WEB) {
            const configRaw = await fetch(configFilePath);
            config = await configRaw.json();
        } else if (ENVIRONMENT_IS_NODE) {
            // eslint-disable-next-line @typescript-eslint/no-var-requires
            const fs = require("fs");
            const json = await fs.promises.readFile(configFilePath);
            config = JSON.parse(json);
        } else { // shell or worker
            const json = read(configFilePath);// read is a v8 debugger command
            config = JSON.parse(json);
        }
        runtimeHelpers.config = config;
        config.environment_variables = config.environment_variables || {};
        config.assets = config.assets || [];
        config.runtime_options = config.runtime_options || [];
        config.globalization_mode = config.globalization_mode || GlobalizationMode.AUTO;

    } catch (exc) {
        const errMessage = `Failed to load config file ${configFilePath} ${exc}`;
        console.error(errMessage);
        runtimeHelpers.config = { message: errMessage, error: exc, isError: true };
        throw exc;
    } finally {
        Module.removeRunDependency(configFilePath);
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
    Module.setValue(main_argv + (aindex * 4), INTERNAL.mono_wasm_strdup(name), "i32");
    aindex += 1;
    for (let i = 0; i < allRuntimeArguments.length; ++i) {
        Module.setValue(main_argv + (aindex * 4), INTERNAL.mono_wasm_strdup(allRuntimeArguments[i]), "i32");
        aindex += 1;
    }
    cwraps.mono_wasm_set_main_args(main_argc, main_argv);
}

type MonoInitContext = {
    tracing: boolean,
    pending_count: number,
    loaded_files: { url: string, file: string }[],
    loaded_assets: { [id: string]: [VoidPtr, number] },
    createPath: Function,
    createDataFile: Function
}