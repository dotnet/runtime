import cwraps from "../cwraps";
import { mono_wasm_runtime_ready } from "../debug";
import diagnostics, { Diagnostics } from "../diagnostics";
import { mono_wasm_load_icu_data } from "../icu";
import { runtimeHelpers } from "../imports";
import { mono_wasm_get_assembly_exports } from "../invoke-cs";
import { mono_wasm_load_bytes_into_heap, setB32, setI8, setI16, setI32, setI52, setU52, setI64Big, setU8, setU16, setU32, setF32, setF64, getB32, getI8, getI16, getI32, getI52, getU52, getI64Big, getU8, getU16, getU32, getF32, getF64 } from "../memory";
import { mono_wasm_new_root_buffer, mono_wasm_new_root, mono_wasm_new_external_root, mono_wasm_release_roots } from "../roots";
import { mono_run_main, mono_run_main_and_exit } from "../run";
import { mono_wasm_setenv, mono_wasm_load_data_archive, mono_wasm_load_config, mono_load_runtime_and_bcl_args } from "../startup";
import { js_string_to_mono_string, conv_string, js_string_to_mono_string_root, conv_string_root } from "../strings";
import { MonoArray, MonoConfig, MonoConfigError, MonoObject, MonoObjectRef } from "../types";
import { VoidPtr } from "../types/emscripten";
import { mono_array_to_js_array, unbox_mono_obj, unbox_mono_obj_root, mono_array_root_to_js_array } from "./cs-to-js";
import { js_typed_array_to_array, js_to_mono_obj, js_typed_array_to_array_root, js_to_mono_obj_root } from "./js-to-cs";
import { mono_bind_static_method, mono_call_assembly_entry_point } from "./method-calls";

export function export_mono_api(): MONOType {
    return {
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
        mono_wasm_get_assembly_exports,

        // for Blazor's future!
        mono_wasm_add_assembly: <any>null,
        mono_wasm_load_runtime: <any>null,

        config: <MonoConfig | MonoConfigError>runtimeHelpers.config,
        loaded_files: <string[]>[],

        // memory accessors
        setB32,
        setI8,
        setI16,
        setI32,
        setI52,
        setU52,
        setI64Big,
        setU8,
        setU16,
        setU32,
        setF32,
        setF64,
        getB32,
        getI8,
        getI16,
        getI32,
        getI52,
        getU52,
        getI64Big,
        getU8,
        getU16,
        getU32,
        getF32,
        getF64,

        // Diagnostics
        diagnostics
    };
}

export function cwraps_mono_api(mono: MONOType): void {
    Object.assign(mono, {
        mono_wasm_add_assembly: cwraps.mono_wasm_add_assembly,
        mono_wasm_load_runtime: cwraps.mono_wasm_load_runtime,
    });
}

export function export_binding_api(): BINDINGType {
    return {
        //current "public" BINDING API
        bind_static_method: mono_bind_static_method,
        call_assembly_entry_point: mono_call_assembly_entry_point,
        mono_obj_array_new: <any>null,
        mono_obj_array_set: <any>null,
        js_string_to_mono_string,
        js_typed_array_to_array,
        mono_array_to_js_array,
        js_to_mono_obj,
        conv_string,
        unbox_mono_obj,

        mono_obj_array_new_ref: <any>null,
        mono_obj_array_set_ref: <any>null,
        js_string_to_mono_string_root,
        js_typed_array_to_array_root,
        js_to_mono_obj_root,
        conv_string_root,
        unbox_mono_obj_root,
        mono_array_root_to_js_array,
    };
}

export function cwraps_binding_api(binding: BINDINGType): void {
    Object.assign(binding, {
        mono_obj_array_new: cwraps.mono_wasm_obj_array_new,
        mono_obj_array_set: cwraps.mono_wasm_obj_array_set,
        mono_obj_array_new_ref: cwraps.mono_wasm_obj_array_new_ref,
        mono_obj_array_set_ref: cwraps.mono_wasm_obj_array_set_ref,
    });
}

export type BINDINGType = {
    bind_static_method: typeof mono_bind_static_method;
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

    // do we really want to advertize add these below ?
    mono_obj_array_new_ref: (size: number, result: MonoObjectRef) => void;
    mono_obj_array_set_ref: (array: MonoObjectRef, idx: number, obj: MonoObjectRef) => void;
    js_string_to_mono_string_root: typeof js_string_to_mono_string_root;
    js_typed_array_to_array_root: typeof js_typed_array_to_array_root;
    js_to_mono_obj_root: typeof js_to_mono_obj_root;
    conv_string_root: typeof conv_string_root;
    unbox_mono_obj_root: typeof unbox_mono_obj_root;
    mono_array_root_to_js_array: typeof mono_array_root_to_js_array;
}

export type MONOType = {
    mono_wasm_setenv: typeof mono_wasm_setenv;
    mono_wasm_load_bytes_into_heap: typeof mono_wasm_load_bytes_into_heap;
    mono_wasm_load_icu_data: typeof mono_wasm_load_icu_data;
    mono_wasm_runtime_ready: typeof mono_wasm_runtime_ready;
    mono_wasm_load_data_archive: typeof mono_wasm_load_data_archive;
    mono_wasm_load_config: typeof mono_wasm_load_config;
    mono_load_runtime_and_bcl_args: typeof mono_load_runtime_and_bcl_args;
    mono_wasm_new_root_buffer: typeof mono_wasm_new_root_buffer;
    mono_wasm_new_root: typeof mono_wasm_new_root;
    mono_wasm_new_external_root: typeof mono_wasm_new_external_root;
    mono_wasm_release_roots: typeof mono_wasm_release_roots;
    mono_run_main: typeof mono_run_main;
    mono_run_main_and_exit: typeof mono_run_main_and_exit;
    mono_wasm_get_assembly_exports: typeof mono_wasm_get_assembly_exports;
    mono_wasm_add_assembly: (name: string, data: VoidPtr, size: number) => number;
    mono_wasm_load_runtime: (unused: string, debug_level: number) => void;
    config: MonoConfig | MonoConfigError;
    loaded_files: string[];
    setB32: typeof setB32;
    setI8: typeof setI8;
    setI16: typeof setI16;
    setI32: typeof setI32;
    setI52: typeof setI52;
    setU52: typeof setU52;
    setI64Big: typeof setI64Big;
    setU8: typeof setU8;
    setU16: typeof setU16;
    setU32: typeof setU32;
    setF32: typeof setF32;
    setF64: typeof setF64;
    getB32: typeof getB32;
    getI8: typeof getI8;
    getI16: typeof getI16;
    getI32: typeof getI32;
    getI52: typeof getI52;
    getU52: typeof getU52;
    getI64Big: typeof getI64Big;
    getU8: typeof getU8;
    getU16: typeof getU16;
    getU32: typeof getU32;
    getF32: typeof getF32;
    getF64: typeof getF64;
    diagnostics: Diagnostics;

}