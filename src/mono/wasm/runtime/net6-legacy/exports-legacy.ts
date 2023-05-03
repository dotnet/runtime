// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { legacy_c_functions as cwraps } from "../cwraps";
import { mono_wasm_runtime_ready } from "../debug";
import { mono_wasm_load_icu_data } from "../icu";
import { runtimeHelpers } from "../globals";
import { mono_wasm_load_bytes_into_heap, setB32, setI8, setI16, setI32, setI52, setU52, setI64Big, setU8, setU16, setU32, setF32, setF64, getB32, getI8, getI16, getI32, getI52, getU52, getI64Big, getU8, getU16, getU32, getF32, getF64 } from "../memory";
import { mono_wasm_new_root_buffer, mono_wasm_new_root, mono_wasm_new_external_root, mono_wasm_release_roots } from "../roots";
import { mono_run_main, mono_run_main_and_exit } from "../run";
import { mono_wasm_setenv, mono_wasm_load_config } from "../startup";
import { js_string_to_mono_string, conv_string, js_string_to_mono_string_root, conv_string_root } from "../strings";
import { mono_array_to_js_array, unbox_mono_obj, unbox_mono_obj_root, mono_array_root_to_js_array } from "./cs-to-js";
import { js_typed_array_to_array, js_to_mono_obj, js_typed_array_to_array_root, js_to_mono_obj_root } from "./js-to-cs";
import { mono_bind_static_method, mono_call_assembly_entry_point } from "./method-calls";
import { mono_wasm_load_runtime } from "../startup";
import { BINDINGType, MONOType } from "./export-types";
import { mono_wasm_load_data_archive } from "../assets";
import { mono_method_resolve } from "./method-binding";

export function export_mono_api(): MONOType {
    return {
        // legacy MONO API
        mono_wasm_setenv,
        mono_wasm_load_bytes_into_heap,
        mono_wasm_load_icu_data,
        mono_wasm_runtime_ready,
        mono_wasm_load_data_archive,
        mono_wasm_load_config,
        mono_wasm_new_root_buffer,
        mono_wasm_new_root,
        mono_wasm_new_external_root,
        mono_wasm_release_roots,
        mono_run_main,
        mono_run_main_and_exit,

        // for Blazor's future!
        mono_wasm_add_assembly: <any>null,
        mono_wasm_load_runtime,

        config: runtimeHelpers.config,
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
    };
}

export function cwraps_mono_api(mono: MONOType): void {
    Object.assign(mono, {
        mono_wasm_add_assembly: cwraps.mono_wasm_add_assembly,
    });
}

export function export_internal_api(): any {
    return {
        mono_method_resolve,//MarshalTests.cs
    };
}

export function export_binding_api(): BINDINGType {
    return {
        // legacy BINDING API
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
