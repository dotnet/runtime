// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module, MONO } from '../runtime'
import { AssetEntry, MonoConfig } from './types'
import cwraps from './cwraps'
import { mono_wasm_runtime_ready } from './debug';
import { mono_wasm_globalization_init, mono_wasm_load_icu_data } from './icu';
import { toBase64StringImpl } from './base64';
import { mono_wasm_init_aot_profiler, mono_wasm_init_coverage_profiler } from './profiler';

// Set environment variable NAME to VALUE
// Should be called before mono_load_runtime_and_bcl () in most cases
export function mono_wasm_setenv(name: string, value: string) {
    cwraps.mono_wasm_setenv(name, value);
}

export function mono_wasm_set_runtime_options(options: string[]) {
    var argv = Module._malloc(options.length * 4);
    let aindex = 0;
    for (var i = 0; i < options.length; ++i) {
        Module.setValue(argv + (aindex * 4), cwraps.mono_wasm_strdup(options[i]), "i32");
        aindex += 1;
    }
    cwraps.mono_wasm_parse_runtime_options(options.length, argv);
}

function _handle_loaded_asset(ctx: MonoInitContext, asset: AssetEntry, url: string, blob: ArrayBuffer) {
    var bytes = new Uint8Array(blob);
    if (ctx.tracing)
        console.log(`MONO_WASM: Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);

    var virtualName: string = asset.virtual_path || asset.name;
    var offset: VoidPtr | null = null;

    switch (asset.behavior) {
        case "resource":
        case "assembly":
            ctx.loaded_files.push({ url: url, file: virtualName });
        case "heap":
        case "icu":
            offset = mono_wasm_load_bytes_into_heap(bytes);
            ctx.loaded_assets[virtualName] = [offset, bytes.length];
            break;

        case "vfs":
            // FIXME
            var lastSlash = virtualName.lastIndexOf("/");
            var parentDirectory = (lastSlash > 0)
                ? virtualName.substr(0, lastSlash)
                : null;
            var fileName = (lastSlash > 0)
                ? virtualName.substr(lastSlash + 1)
                : virtualName;
            if (fileName.startsWith("/"))
                fileName = fileName.substr(1);
            if (parentDirectory) {
                if (ctx.tracing)
                    console.log("MONO_WASM: Creating directory '" + parentDirectory + "'");

                var pathRet = ctx.createPath(
                    "/", parentDirectory, true, true // fixme: should canWrite be false?
                );
            } else {
                parentDirectory = "/";
            }

            if (ctx.tracing)
                console.log("MONO_WASM: Creating file '" + fileName + "' in directory '" + parentDirectory + "'");

            if (!mono_wasm_load_data_archive(bytes, parentDirectory)) {
                var fileRet = ctx.createDataFile(
                    parentDirectory, fileName,
                    bytes, true /* canRead */, true /* canWrite */, true /* canOwn */
                );
            }
            break;

        default:
            throw new Error(`Unrecognized asset behavior:${asset.behavior}, for asset ${asset.name}`);
    }

    if (asset.behavior === "assembly") {
        var hasPpdb = cwraps.mono_wasm_add_assembly(virtualName, offset!, bytes.length);

        if (!hasPpdb) {
            var index = ctx.loaded_files.findIndex(element => element.file == virtualName);
            ctx.loaded_files.splice(index, 1);
        }
    }
    else if (asset.behavior === "icu") {
        if (!mono_wasm_load_icu_data(offset!))
            console.error("Error loading ICU asset", asset.name);
    }
    else if (asset.behavior === "resource") {
        cwraps.mono_wasm_add_satellite_assembly(virtualName, asset.culture!, offset!, bytes.length);
    }
}

// Initializes the runtime and loads assemblies, debug information, and other files.
// @args is a dictionary-style Object with the following properties:
//    assembly_root: (required) the subfolder containing managed assemblies and pdbs
//    debug_level or enable_debugging: (required)
//    assets: (required) a list of assets to load along with the runtime. each asset
//     is a dictionary-style Object with the following properties:
//        name: (required) the name of the asset, including extension.
//        behavior: (required) determines how the asset will be handled once loaded:
//          "heap": store asset into the native heap
//          "assembly": load asset as a managed assembly (or debugging information)
//          "resource": load asset as a managed resource assembly
//          "icu": load asset as an ICU data archive
//          "vfs": load asset into the virtual filesystem (for fopen, File.Open, etc)
//        load_remote: (optional) if true, an attempt will be made to load the asset
//          from each location in @args.remote_sources.
//        virtual_path: (optional) if specified, overrides the path of the asset in
//          the virtual filesystem and similar data structures once loaded.
//        is_optional: (optional) if true, any failure to load this asset will be ignored.
//    loaded_cb: (required) a function () invoked when loading has completed.
//    fetch_file_cb: (optional) a function (string) invoked to fetch a given file.
//      If no callback is provided a default implementation appropriate for the current
//      environment will be selected (readFileSync in node, fetch elsewhere).
//      If no default implementation is available this call will fail.
//    remote_sources: (optional) additional search locations for assets.
//      sources will be checked in sequential order until the asset is found.
//      the string "./" indicates to load from the application directory (as with the
//      files in assembly_list), and a fully-qualified URL like "https://example.com/" indicates
//      that asset loads can be attempted from a remote server. Sources must end with a "/".
//    environment_variables: (optional) dictionary-style Object containing environment variables
//    runtime_options: (optional) array of runtime options as strings
//    aot_profiler_options: (optional) dictionary-style Object. see the comments for
//      mono_wasm_init_aot_profiler. If omitted, aot profiler will not be initialized.
//    coverage_profiler_options: (optional) dictionary-style Object. see the comments for
//      mono_wasm_init_coverage_profiler. If omitted, coverage profiler will not be initialized.
//    globalization_mode: (optional) configures the runtime's globalization mode:
//      "icu": load ICU globalization data from any runtime assets with behavior "icu".
//      "invariant": operate in invariant globalization mode.
//      "auto" (default): if "icu" behavior assets are present, use ICU, otherwise invariant.
//    diagnostic_tracing: (optional) enables diagnostic log messages during startup
export function mono_load_runtime_and_bcl_args(args: MonoConfig) {
    try {
        return _load_assets_and_runtime(args);
    } catch (exc: any) {
        console.error("error in mono_load_runtime_and_bcl_args:", exc.toString());
        throw exc;
    }
}

function _apply_configuration_from_args(args: MonoConfig) {
    for (var k in (args.environment_variables || {}))
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

    if (ENVIRONMENT_IS_NODE) {
        var fs = require('fs');
        return function (asset) {
            console.debug("MONO_WASM: Loading... " + asset);
            var binary = fs.readFileSync(asset);
            var resolve_func2 = function (resolve: Function, reject: Function) {
                resolve(new Uint8Array(binary));
            };

            var resolve_func1 = function (resolve: Function, reject: Function) {
                var response = {
                    ok: true,
                    url: asset,
                    arrayBuffer: function () {
                        return new Promise(resolve_func2);
                    }
                };
                resolve(response);
            };

            return new Promise(resolve_func1);
        };
    } else if (typeof (fetch) === "function") {
        return function (asset) {
            return fetch(asset, { credentials: 'same-origin' });
        };
    } else {
        throw new Error("No fetch_file_cb was provided and this environment does not expose 'fetch'.");
    }
}

function _finalize_startup(args: MonoConfig, ctx: MonoInitContext) {
    var loaded_files_with_debug_info: string[] = [];
    ctx.loaded_files.forEach(value => loaded_files_with_debug_info.push(value.url));
    MONO.loaded_assets = ctx.loaded_assets;
    MONO.loaded_files = loaded_files_with_debug_info;
    if (ctx.tracing) {
        console.log("MONO_WASM: loaded_assets: " + JSON.stringify(ctx.loaded_assets));
        console.log("MONO_WASM: loaded_files: " + JSON.stringify(ctx.loaded_files));
    }

    var load_runtime = cwraps.mono_wasm_load_runtime;

    console.debug("MONO_WASM: Initializing mono runtime");

    mono_wasm_globalization_init(args.globalization_mode!);

    if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
        try {
            load_runtime("unused", args.debug_level || 0);
        } catch (ex: any) {
            Module.print("MONO_WASM: load_runtime () failed: " + ex);
            Module.print("MONO_WASM: Stacktrace: \n");
            Module.print(ex.stack);

            var wasm_exit = cwraps.mono_wasm_exit;
            wasm_exit(1);
        }
    } else {
        load_runtime("unused", args.debug_level || 0);
    }

    let tz;
    try {
        tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
    } catch { }
    mono_wasm_setenv("TZ", tz || "UTC");
    mono_wasm_runtime_ready();
    args.loaded_cb();
}

function _load_assets_and_runtime(args: MonoConfig) {
    if (args.enable_debugging)
        args.debug_level = args.enable_debugging;
    if (args.assembly_list)
        throw new Error("Invalid args (assembly_list was replaced by assets)");
    if (args.runtime_assets)
        throw new Error("Invalid args (runtime_assets was replaced by assets)");
    if (args.runtime_asset_sources)
        throw new Error("Invalid args (runtime_asset_sources was replaced by remote_sources)");
    if (!args.loaded_cb)
        throw new Error("loaded_cb not provided");

    var ctx: MonoInitContext = {
        tracing: args.diagnostic_tracing || false,
        pending_count: args.assets.length,
        loaded_assets: Object.create(null),
        // dlls and pdbs, used by blazor and the debugger
        loaded_files: [],
        createPath: Module.FS_createPath,
        createDataFile: Module.FS_createDataFile
    };

    if (ctx.tracing)
        console.log("mono_wasm_load_runtime_with_args", JSON.stringify(args));

    _apply_configuration_from_args(args);

    var fetch_file_cb = _get_fetch_file_cb_from_args(args);

    var onPendingRequestComplete = function () {
        --ctx.pending_count;

        if (ctx.pending_count === 0) {
            try {
                _finalize_startup(args, ctx);
            } catch (exc) {
                console.error("Unhandled exception in _finalize_startup", exc);
                throw exc;
            }
        }
    };

    var processFetchResponseBuffer = function (asset: AssetEntry, url: string, buffer: ArrayBuffer) {
        try {
            _handle_loaded_asset(ctx, asset, url, buffer);
        } catch (exc) {
            console.error(`Unhandled exception in processFetchResponseBuffer ${url}`, exc);
            throw exc;
        } finally {
            onPendingRequestComplete();
        }
    };

    args.assets.forEach(function (asset: AssetEntry) {
        var sourceIndex = 0;
        var sourcesList = asset.load_remote ? args.remote_sources! : [""];

        var handleFetchResponse = function (response: Response) {
            if (!response.ok) {
                try {
                    attemptNextSource();
                    return;
                } catch (exc) {
                    console.error("MONO_WASM: Unhandled exception in handleFetchResponse attemptNextSource for asset", asset.name, exc);
                    throw exc;
                }
            }

            try {
                var bufferPromise = response.arrayBuffer();
                bufferPromise.then((data) => processFetchResponseBuffer(asset, response.url, data));
            } catch (exc) {
                console.error("MONO_WASM: Unhandled exception in handleFetchResponse for asset", asset.name, exc);
                attemptNextSource();
            }
        };

        const attemptNextSource = function () {
            if (sourceIndex >= sourcesList.length) {
                var msg = "MONO_WASM: Failed to load " + asset.name;
                try {
                    var isOk = asset.is_optional ||
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

            var sourcePrefix = sourcesList[sourceIndex];
            sourceIndex++;

            // HACK: Special-case because MSBuild doesn't allow "" as an attribute
            if (sourcePrefix === "./")
                sourcePrefix = "";

            var attemptUrl;
            if (sourcePrefix.trim() === "") {
                if (asset.behavior === "assembly")
                    attemptUrl = locateFile(args.assembly_root + "/" + asset.name);
                else if (asset.behavior === "resource") {
                    var path = asset.culture !== '' ? `${asset.culture}/${asset.name}` : asset.name;
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
                        console.log("Attempting to fetch '" + attemptUrl + "'");
                } else {
                    if (ctx.tracing)
                        console.log("Attempting to fetch '" + attemptUrl + "' for", asset.name);
                }
                var fetch_promise = fetch_file_cb(attemptUrl);
                fetch_promise.then(handleFetchResponse);
            } catch (exc) {
                console.error("MONO_WASM: Error fetching " + attemptUrl, exc);
                attemptNextSource();
            }
        };

        attemptNextSource();
    });
}

// used from ASP.NET
export function mono_wasm_load_data_archive(data: TypedArray, prefix: string) {
    if (data.length < 8)
        return false;

    var dataview = new DataView(data.buffer);
    var magic = dataview.getUint32(0, true);
    //    get magic number
    if (magic != 0x626c6174) {
        return false;
    }
    var manifestSize = dataview.getUint32(4, true);
    if (manifestSize == 0 || data.length < manifestSize + 8)
        return false;

    var manifest;
    try {
        var manifestContent = Module.UTF8ArrayToString(data, 8, manifestSize);
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

    var folders = new Set<string>()
    manifest.filter(m => {
        var file = m[0];
        var last = file.lastIndexOf("/");
        var directory = file.slice(0, last + 1);
        folders.add(directory);
    });
    folders.forEach(folder => {
        Module['FS_createPath'](prefix, folder, true, true);
    });

    for (var row of manifest) {
        var name = row[0];
        var length = row[1];
        var bytes = data.slice(0, length);
        Module['FS_createDataFile'](prefix, name, bytes, true, true);
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
export async function mono_wasm_load_config(configFilePath: string) {
    Module.addRunDependency(configFilePath);
    try {
        let config = null;
        // NOTE: when we add nodejs make sure to include the nodejs fetch package
        if (ENVIRONMENT_IS_WEB) {
            const configRaw = await fetch(configFilePath);
            config = await configRaw.json();
        } else if (ENVIRONMENT_IS_NODE) {
            config = require(configFilePath);
        } else { // shell or worker
            config = JSON.parse(read(configFilePath)); // read is a v8 debugger command
        }
        MONO.config = config;
        Module.config = MONO.config;
    } catch (e) {
        const errMessage = "failed to load config file " + configFilePath;
        console.error(errMessage)
        MONO.config = <any>{ message: errMessage, error: e };
        Module.config = MONO.config;
    } finally {
        Module.removeRunDependency(configFilePath);
    }
}

export function mono_wasm_asm_loaded(assembly_name: number, assembly_ptr: number, assembly_len: number, pdb_ptr: number, pdb_len: number) {
    // Only trigger this codepath for assemblies loaded after app is ready
    if (MONO.mono_wasm_runtime_is_ready !== true)
        return;

    const assembly_name_str = assembly_name !== 0 ? Module.UTF8ToString(assembly_name).concat('.dll') : '';
    const assembly_data = new Uint8Array(Module.HEAPU8.buffer, assembly_ptr, assembly_len);
    const assembly_b64 = toBase64StringImpl(assembly_data);

    let pdb_b64;
    if (pdb_ptr) {
        const pdb_data = new Uint8Array(Module.HEAPU8.buffer, pdb_ptr, pdb_len);
        pdb_b64 = toBase64StringImpl(pdb_data);
    }

    MONO.mono_wasm_raise_debug_event({
        eventName: 'AssemblyLoaded',
        assembly_name: assembly_name_str,
        assembly_b64,
        pdb_b64
    });
}

// @bytes must be a typed array. space is allocated for it in the native heap
//  and it is copied to that location. returns the address of the allocation.
export function mono_wasm_load_bytes_into_heap(bytes: Uint8Array): VoidPtr {
    var memoryOffset = Module._malloc(bytes.length);
    var heapBytes = new Uint8Array(Module.HEAPU8.buffer, memoryOffset, bytes.length);
    heapBytes.set(bytes);
    return memoryOffset;
}

type MonoInitContext = {
    tracing: boolean,
    pending_count: number,
    loaded_files: { url: string, file: string }[],
    loaded_assets: { [id: string]: [VoidPtr, number] },
    createPath: Function,
    createDataFile: Function
}