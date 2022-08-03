import { mono_wasm_runtime_ready } from "../debug";
import { mono_wasm_load_icu_data } from "../icu";
import { mono_wasm_get_assembly_exports } from "../invoke-cs";
import { mono_wasm_load_bytes_into_heap, setB32, setI8, setI16, setI32, setI52, setU52, setI64Big, setU8, setU16, setU32, setF32, setF64, getB32, getI8, getI16, getI32, getI52, getU52, getI64Big, getU8, getU16, getU32, getF32, getF64 } from "../memory";
import { mono_wasm_new_root_buffer, mono_wasm_new_root, mono_wasm_new_external_root, mono_wasm_release_roots } from "../roots";
import { mono_run_main, mono_run_main_and_exit } from "../run";
import { mono_wasm_setenv, mono_wasm_load_data_archive, mono_wasm_load_config } from "../startup";
import { js_string_to_mono_string, conv_string, js_string_to_mono_string_root, conv_string_root } from "../strings";
import { MonoArray, MonoObject, MonoObjectRef } from "../types";
import { VoidPtr } from "../types/emscripten";
import { mono_array_to_js_array, unbox_mono_obj, unbox_mono_obj_root, mono_array_root_to_js_array } from "./cs-to-js";
import { js_typed_array_to_array, js_to_mono_obj, js_typed_array_to_array_root, js_to_mono_obj_root } from "./js-to-cs";
import { mono_bind_static_method, mono_call_assembly_entry_point } from "./method-calls";

/**
 * @deprecated Please use methods in top level API object instead
 */
export type BINDINGType = {
    /**
     * @deprecated Please use [JSExportAttribute] instead
     */
    bind_static_method: typeof mono_bind_static_method;
    /**
     * @deprecated Please use runMain() instead
     */
    call_assembly_entry_point: typeof mono_call_assembly_entry_point;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_new: (size: number) => MonoArray;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_obj_array_set: (array: MonoArray, idx: number, obj: MonoObject) => void;
    /**
     * @deprecated Not GC or thread safe
     */
    js_string_to_mono_string: typeof js_string_to_mono_string;
    /**
     * @deprecated Not GC or thread safe
     */
    js_typed_array_to_array: typeof js_typed_array_to_array;
    /**
     * @deprecated Not GC or thread safe
     */
    mono_array_to_js_array: typeof mono_array_to_js_array;
    /**
     * @deprecated Not GC or thread safe
     */
    js_to_mono_obj: typeof js_to_mono_obj;
    /**
     * @deprecated Not GC or thread safe
     */
    conv_string: typeof conv_string;
    /**
     * @deprecated Not GC or thread safe
     */
    unbox_mono_obj: typeof unbox_mono_obj;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_obj_array_new_ref: (size: number, result: MonoObjectRef) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_obj_array_set_ref: (array: MonoObjectRef, idx: number, obj: MonoObjectRef) => void;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_string_to_mono_string_root: typeof js_string_to_mono_string_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_typed_array_to_array_root: typeof js_typed_array_to_array_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    js_to_mono_obj_root: typeof js_to_mono_obj_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    conv_string_root: typeof conv_string_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    unbox_mono_obj_root: typeof unbox_mono_obj_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_array_root_to_js_array: typeof mono_array_root_to_js_array;
}

/**
 * @deprecated Please use methods in top level API object instead
 */
export type MONOType = {
    /**
     * @deprecated Please use setEnvironmentVariable() instead
     */
    mono_wasm_setenv: typeof mono_wasm_setenv;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_runtime_ready: typeof mono_wasm_runtime_ready;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_data_archive: typeof mono_wasm_load_data_archive;
    /**
     * @deprecated Please use configSrc instead
     */
    mono_wasm_load_config: typeof mono_wasm_load_config;
    /**
     * @deprecated Please use runMain instead
     */
    mono_load_runtime_and_bcl_args: Function;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_root_buffer: typeof mono_wasm_new_root_buffer;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_root: typeof mono_wasm_new_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_new_external_root: typeof mono_wasm_new_external_root;
    /**
     * @deprecated Please use [JSImportAttribute] or [JSExportAttribute] for interop instead.
     */
    mono_wasm_release_roots: typeof mono_wasm_release_roots;
    /**
     * @deprecated Please use runMain instead
     */
    mono_run_main: typeof mono_run_main;
    /**
     * @deprecated Please use runMainAndExit instead
     */
    mono_run_main_and_exit: typeof mono_run_main_and_exit;
    /**
     * @deprecated Please use getAssemblyExports instead
     */
    mono_wasm_get_assembly_exports: typeof mono_wasm_get_assembly_exports;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_add_assembly: (name: string, data: VoidPtr, size: number) => number;
    /**
     * @deprecated Please use config.assets instead
     */
    mono_wasm_load_runtime: (unused: string, debugLevel: number) => void;
    /**
     * @deprecated Please use getConfig() instead
     */
    config: any;
    /**
     * @deprecated Please use config.assets instead
     */
    loaded_files: string[];
    /**
     * @deprecated Please use setHeapB32
     */
    setB32: typeof setB32;
    /**
     * @deprecated Please use setHeapI8
     */
    setI8: typeof setI8;
    /**
     * @deprecated Please use setHeapI16
     */
    setI16: typeof setI16;
    /**
     * @deprecated Please use setHeapI32
     */
    setI32: typeof setI32;
    /**
     * @deprecated Please use setHeapI52
     */
    setI52: typeof setI52;
    /**
     * @deprecated Please use setHeapU52
     */
    setU52: typeof setU52;
    /**
     * @deprecated Please use setHeapI64Big
     */
    setI64Big: typeof setI64Big;
    /**
     * @deprecated Please use setHeapU8
     */
    setU8: typeof setU8;
    /**
     * @deprecated Please use setHeapU16
     */
    setU16: typeof setU16;
    /**
     * @deprecated Please use setHeapU32
     */
    setU32: typeof setU32;
    /**
     * @deprecated Please use setHeapF32
     */
    setF32: typeof setF32;
    /**
     * @deprecated Please use setHeapF64
     */
    setF64: typeof setF64;
    /**
     * @deprecated Please use getHeapB32
     */
    getB32: typeof getB32;
    /**
     * @deprecated Please use getHeapI8
     */
    getI8: typeof getI8;
    /**
     * @deprecated Please use getHeapI16
     */
    getI16: typeof getI16;
    /**
     * @deprecated Please use getHeapI32
     */
    getI32: typeof getI32;
    /**
     * @deprecated Please use getHeapI52
     */
    getI52: typeof getI52;
    /**
     * @deprecated Please use getHeapU52
     */
    getU52: typeof getU52;
    /**
     * @deprecated Please use getHeapI64Big
     */
    getI64Big: typeof getI64Big;
    /**
     * @deprecated Please use getHeapU8
     */
    getU8: typeof getU8;
    /**
     * @deprecated Please use getHeapU16
     */
    getU16: typeof getU16;
    /**
     * @deprecated Please use getHeapU32
     */
    getU32: typeof getU32;
    /**
     * @deprecated Please use getHeapF32
     */
    getF32: typeof getF32;
    /**
     * @deprecated Please use getHeapF64
     */
    getF64: typeof getF64;
}