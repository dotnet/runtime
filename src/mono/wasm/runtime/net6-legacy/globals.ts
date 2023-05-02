// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { GlobalObjects, MonoClass } from "../types";
import type { VoidPtr } from "../types/emscripten";
import type { BINDINGType, MONOType } from "./export-types";

export let MONO: MONOType;
export let BINDING: BINDINGType;

export const legacyHelpers: LegacyHelpers = <any>{
};

export function initializeLegacyExports(
    globals: GlobalObjects,
): void {
    MONO = globals.mono;
    BINDING = globals.binding;
}

export type LegacyHelpers = {
    runtime_legacy_exports_classname: string;
    runtime_legacy_exports_class: MonoClass;

    // A WasmRoot that is guaranteed to contain 0
    _null_root: any;
    _class_int32: MonoClass;
    _class_uint32: MonoClass;
    _class_double: MonoClass;
    _class_boolean: MonoClass;
    _unbox_buffer_size: number;
    _box_buffer: VoidPtr;
    _unbox_buffer: VoidPtr;
    _box_root: any;
}

export const wasm_type_symbol = Symbol.for("wasm type");
