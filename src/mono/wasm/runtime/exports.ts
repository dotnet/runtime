// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import Configuration from "consts:configuration";

import {
    mono_wasm_new_root, mono_wasm_release_roots, mono_wasm_new_external_root,
    mono_wasm_new_root_buffer
} from "./roots";
import {
    mono_wasm_send_dbg_command_with_parms,
    mono_wasm_send_dbg_command,
    mono_wasm_get_dbg_command_info,
    mono_wasm_get_details,
    mono_wasm_release_object,
    mono_wasm_call_function_on,
    mono_wasm_debugger_resume,
    mono_wasm_detach_debugger,
    mono_wasm_runtime_ready,
    mono_wasm_get_loaded_files,
    mono_wasm_raise_debug_event,
    mono_wasm_fire_debugger_agent_message,
    mono_wasm_debugger_log,
    mono_wasm_trace_logger,
    mono_wasm_add_dbg_command_received,
    mono_wasm_change_debugger_log_level,
    mono_wasm_symbolicate_string,
    mono_wasm_stringify_as_error_with_stack,
} from "./debug";
import { ENVIRONMENT_IS_WEB, ExitStatusError, runtimeHelpers, setImportsAndExports } from "./imports";
import { DotnetModuleConfigImports, DotnetModule } from "./types";
import {
    mono_load_runtime_and_bcl_args, mono_wasm_load_config,
    mono_wasm_setenv, mono_wasm_set_runtime_options,
    mono_wasm_load_data_archive, mono_wasm_asm_loaded,
    configure_emscripten_startup
} from "./startup";
import { mono_set_timeout, schedule_background_exec } from "./scheduling";
import { mono_wasm_load_icu_data, mono_wasm_get_icudt_name } from "./icu";
import { conv_string, conv_string_root, js_string_to_mono_string, js_string_to_mono_string_root, mono_intern_string } from "./strings";
import { js_to_mono_obj, js_typed_array_to_array, mono_wasm_typed_array_to_array_ref, js_to_mono_obj_root, js_typed_array_to_array_root } from "./js-to-cs";
import {
    mono_array_to_js_array, mono_wasm_create_cs_owned_object_ref, unbox_mono_obj, unbox_mono_obj_root, mono_array_root_to_js_array
} from "./cs-to-js";
import {
    call_static_method, mono_bind_static_method, mono_call_assembly_entry_point,
    mono_method_resolve,
    mono_wasm_compile_function,
    mono_wasm_get_by_index_ref, mono_wasm_get_global_object_ref, mono_wasm_get_object_property_ref,
    mono_wasm_invoke_js,
    mono_wasm_invoke_js_blazor,
    mono_wasm_invoke_js_with_args, mono_wasm_set_by_index_ref, mono_wasm_set_object_property_ref
} from "./method-calls";
import { mono_wasm_typed_array_copy_to_ref, mono_wasm_typed_array_from_ref, mono_wasm_typed_array_copy_from_ref, mono_wasm_load_bytes_into_heap } from "./buffers";
import { mono_wasm_cancel_promise } from "./cancelable-promise";
import { mono_wasm_release_cs_owned_object } from "./gc-handles";
import { mono_wasm_web_socket_open_ref, mono_wasm_web_socket_send, mono_wasm_web_socket_receive, mono_wasm_web_socket_close_ref, mono_wasm_web_socket_abort } from "./web-socket";
import cwraps from "./cwraps";
import {
    setI8, setI16, setI32, setI64,
    setU8, setU16, setU32, setF32, setF64,
    getI8, getI16, getI32, getI64,
    getU8, getU16, getU32, getF32, getF64,
} from "./memory";
import { create_weak_ref } from "./weak-ref";
import { fetch_like, readAsync_like } from "./polyfills";
import { EmscriptenModule } from "./types/emscripten";
import { mono_run_main, mono_run_main_and_exit } from "./run";

const MONO = {
    // current "public" MONO API
    mono_wasm_setenv,
    mono_wasm_load_bytes_into_heap,
    mono_wasm_load_icu_data,
    mono_wasm_runtime_ready,
    mono_wasm_load_data_archive,
    mono_wasm_load_config,
    mono_load_runtime_and_bcl_args,
    mono_wasm_new_root_buffer,
    mono_wasm_new_root,
    mono_wasm_new_external_root,
    mono_wasm_release_roots,
    mono_run_main,
    mono_run_main_and_exit,

    // for Blazor's future!
    mono_wasm_add_assembly: cwraps.mono_wasm_add_assembly,
    mono_wasm_load_runtime: cwraps.mono_wasm_load_runtime,

    config: runtimeHelpers.config,
    loaded_files: <string[]>[],

    // memory accessors
    setI8,
    setI16,
    setI32,
    setI64,
    setU8,
    setU16,
    setU32,
    setF32,
    setF64,
    getI8,
    getI16,
    getI32,
    getI64,
    getU8,
    getU16,
    getU32,
    getF32,
    getF64,
};
export type MONOType = typeof MONO;

const BINDING = {
    //current "public" BINDING API
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_new: cwraps.mono_wasm_obj_array_new,
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_set: cwraps.mono_wasm_obj_array_set,
    /**
     * @deprecated Not GC or thread safe
     */
    js_string_to_mono_string,
    /**
     * @deprecated Not GC or thread safe
     */
    js_typed_array_to_array,
    /**
     * @deprecated Not GC or thread safe
     */
    mono_array_to_js_array,
    /**
     * @deprecated Not GC or thread safe
     */
    js_to_mono_obj,
    /**
     * @deprecated Not GC or thread safe
     */
    conv_string,
    /**
     * @deprecated Not GC or thread safe
     */
    unbox_mono_obj,
    /**
     * @deprecated Renamed to conv_string_root
     */
    conv_string_rooted: conv_string_root,

    mono_obj_array_new_ref: cwraps.mono_wasm_obj_array_new_ref,
    mono_obj_array_set_ref: cwraps.mono_wasm_obj_array_set_ref,
    js_string_to_mono_string_root,
    js_typed_array_to_array_root,
    js_to_mono_obj_root,
    conv_string_root,
    unbox_mono_obj_root,
    mono_array_root_to_js_array,

    bind_static_method: mono_bind_static_method,
    call_assembly_entry_point: mono_call_assembly_entry_point,
};
export type BINDINGType = typeof BINDING;

let exportedAPI: DotnetPublicAPI;

// this is executed early during load of emscripten runtime
// it exports methods to global objects MONO, BINDING and Module in backward compatible way
// At runtime this will be referred to as 'createDotnetRuntime'
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
function initializeImportsAndExports(
    imports: { isESM: boolean, isGlobal: boolean, isNode: boolean, isShell: boolean, isWeb: boolean, locateFile: Function, quit_: Function, ExitStatus: ExitStatusError, requirePromise: Promise<Function> },
    exports: { mono: any, binding: any, internal: any, module: any },
    replacements: { fetch: any, readAsync: any, require: any, requireOut: any, noExitRuntime: boolean },
): DotnetPublicAPI {
    const module = exports.module as DotnetModule;
    const globalThisAny = globalThis as any;

    // we want to have same instance of MONO, BINDING and Module in dotnet iffe
    setImportsAndExports(imports, exports);

    // here we merge methods from the local objects into exported objects
    Object.assign(exports.mono, MONO);
    Object.assign(exports.binding, BINDING);
    Object.assign(exports.internal, INTERNAL);

    exportedAPI = <any>{
        MONO: exports.mono,
        BINDING: exports.binding,
        INTERNAL: exports.internal,
        Module: module,
        RuntimeBuildInfo: {
            ProductVersion,
            Configuration
        }
    };
    if (exports.module.__undefinedConfig) {
        module.disableDotnet6Compatibility = true;
        module.configSrc = "./mono-config.json";
    }

    if (!module.print) {
        module.print = console.log.bind(console);
    }
    if (!module.printErr) {
        module.printErr = console.error.bind(console);
    }
    module.imports = module.imports || <DotnetModuleConfigImports>{};
    if (!module.imports.require) {
        module.imports.require = (name) => {
            const resolved = (<any>module.imports)[name];
            if (resolved) {
                return resolved;
            }
            if (replacements.require) {
                return replacements.require(name);
            }
            throw new Error(`Please provide Module.imports.${name} or Module.imports.require`);
        };
    }

    if (module.imports.fetch) {
        runtimeHelpers.fetch = module.imports.fetch;
    }
    else {
        runtimeHelpers.fetch = fetch_like;
    }
    replacements.fetch = runtimeHelpers.fetch;
    replacements.readAsync = readAsync_like;
    replacements.requireOut = module.imports.require;

    replacements.noExitRuntime = ENVIRONMENT_IS_WEB;

    if (typeof module.disableDotnet6Compatibility === "undefined") {
        module.disableDotnet6Compatibility = imports.isESM;
    }
    // here we expose objects global namespace for tests and backward compatibility
    if (imports.isGlobal || !module.disableDotnet6Compatibility) {
        Object.assign(module, exportedAPI);

        // backward compatibility
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        module.mono_bind_static_method = (fqn: string, signature: string/*ArgsMarshalString*/): Function => {
            console.warn("Module.mono_bind_static_method is obsolete, please use BINDING.bind_static_method instead");
            return mono_bind_static_method(fqn, signature);
        };

        const warnWrap = (name: string, provider: () => any) => {
            if (typeof globalThisAny[name] !== "undefined") {
                // it already exists in the global namespace
                return;
            }
            let value: any = undefined;
            Object.defineProperty(globalThis, name, {
                get: () => {
                    if (!value) {
                        const stack = (new Error()).stack;
                        const nextLine = stack ? stack.substr(stack.indexOf("\n", 8) + 1) : "";
                        console.warn(`global ${name} is obsolete, please use Module.${name} instead ${nextLine}`);
                        value = provider();
                    }
                    return value;
                }
            });
        };
        globalThisAny.MONO = exports.mono;
        globalThisAny.BINDING = exports.binding;
        globalThisAny.INTERNAL = exports.internal;
        if (!imports.isGlobal) {
            globalThisAny.Module = module;
        }

        // Blazor back compat
        warnWrap("cwrap", () => module.cwrap);
        warnWrap("addRunDependency", () => module.addRunDependency);
        warnWrap("removeRunDependency", () => module.removeRunDependency);
    }

    // this code makes it possible to find dotnet runtime on a page via global namespace, even when there are multiple runtimes at the same time
    let list: RuntimeList;
    if (!globalThisAny.getDotnetRuntime) {
        globalThisAny.getDotnetRuntime = (runtimeId: string) => globalThisAny.getDotnetRuntime.__list.getRuntime(runtimeId);
        globalThisAny.getDotnetRuntime.__list = list = new RuntimeList();
    }
    else {
        list = globalThisAny.getDotnetRuntime.__list;
    }
    list.registerRuntime(exportedAPI);

    configure_emscripten_startup(module, exportedAPI);

    // HACK: Emscripten expects the return value of this function to always be the Module object,
    // but we changed ours to return a set of exported namespaces. In order for the emscripten
    // generated worker code to keep working, we detect that we're running in a worker (via the
    // presence of globalThis.importScripts) and emulate the old behavior. Note that this will
    // impact anyone trying to load us in a web worker directly, not just emscripten!
    if (typeof ((<any>globalThis)["importScripts"]) === "function")
        return <any>exportedAPI.Module;

    return exportedAPI;
}

export const __initializeImportsAndExports: any = initializeImportsAndExports; // don't want to export the type

// the methods would be visible to EMCC linker
// --- keep in sync with dotnet.cjs.lib.js ---
export const __linker_exports: any = {
    // mini-wasm.c
    mono_set_timeout,

    // mini-wasm-debugger.c
    mono_wasm_asm_loaded,
    mono_wasm_fire_debugger_agent_message,
    mono_wasm_debugger_log,
    mono_wasm_add_dbg_command_received,

    // mono-threads-wasm.c
    schedule_background_exec,

    // also keep in sync with driver.c
    mono_wasm_invoke_js,
    mono_wasm_invoke_js_blazor,
    mono_wasm_trace_logger,

    // also keep in sync with corebindings.c
    mono_wasm_invoke_js_with_args,
    mono_wasm_get_object_property_ref,
    mono_wasm_set_object_property_ref,
    mono_wasm_get_by_index_ref,
    mono_wasm_set_by_index_ref,
    mono_wasm_get_global_object_ref,
    mono_wasm_create_cs_owned_object_ref,
    mono_wasm_release_cs_owned_object,
    mono_wasm_typed_array_to_array_ref,
    mono_wasm_typed_array_copy_to_ref,
    mono_wasm_typed_array_from_ref,
    mono_wasm_typed_array_copy_from_ref,
    mono_wasm_cancel_promise,
    mono_wasm_web_socket_open_ref,
    mono_wasm_web_socket_send,
    mono_wasm_web_socket_receive,
    mono_wasm_web_socket_close_ref,
    mono_wasm_web_socket_abort,
    mono_wasm_compile_function,

    //  also keep in sync with pal_icushim_static.c
    mono_wasm_load_icu_data,
    mono_wasm_get_icudt_name,
};

const INTERNAL: any = {
    // startup
    BINDING_ASM: "[System.Private.Runtime.InteropServices.JavaScript]System.Runtime.InteropServices.JavaScript.Runtime",

    // tests
    call_static_method,
    mono_wasm_exit: cwraps.mono_wasm_exit,
    mono_wasm_enable_on_demand_gc: cwraps.mono_wasm_enable_on_demand_gc,
    mono_profiler_init_aot: cwraps.mono_profiler_init_aot,
    mono_wasm_set_runtime_options,
    mono_wasm_exec_regression: cwraps.mono_wasm_exec_regression,
    mono_method_resolve,//MarshalTests.cs
    mono_bind_static_method,// MarshalTests.cs
    mono_intern_string,// MarshalTests.cs

    // with mono_wasm_debugger_log and mono_wasm_trace_logger
    logging: undefined,

    //
    mono_wasm_symbolicate_string,
    mono_wasm_stringify_as_error_with_stack,

    // used in debugger DevToolsHelper.cs
    mono_wasm_get_loaded_files,
    mono_wasm_send_dbg_command_with_parms,
    mono_wasm_send_dbg_command,
    mono_wasm_get_dbg_command_info,
    mono_wasm_get_details,
    mono_wasm_release_object,
    mono_wasm_call_function_on,
    mono_wasm_debugger_resume,
    mono_wasm_detach_debugger,
    mono_wasm_raise_debug_event,
    mono_wasm_change_debugger_log_level,
    mono_wasm_runtime_is_ready: runtimeHelpers.mono_wasm_runtime_is_ready,
};


// this represents visibility in the javascript
// like https://github.com/dotnet/aspnetcore/blob/main/src/Components/Web.JS/src/Platform/Mono/MonoTypes.ts
export interface DotnetPublicAPI {
    MONO: typeof MONO,
    BINDING: typeof BINDING,
    INTERNAL: any,
    Module: EmscriptenModule,
    RuntimeId: number,
    RuntimeBuildInfo: {
        ProductVersion: string,
        Configuration: string,
    }
}

class RuntimeList {
    private list: { [runtimeId: number]: WeakRef<DotnetPublicAPI> } = {};

    public registerRuntime(api: DotnetPublicAPI): number {
        api.RuntimeId = Object.keys(this.list).length;
        this.list[api.RuntimeId] = create_weak_ref(api);
        return api.RuntimeId;
    }

    public getRuntime(runtimeId: number): DotnetPublicAPI | undefined {
        const wr = this.list[runtimeId];
        return wr ? wr.deref() : undefined;
    }
}