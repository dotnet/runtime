// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { VoidPtr } from "./types/emscripten";


let icuDataOffset: VoidPtr | null = null;
// @offset must be the address of an ICU data archive in the native heap.
// returns true on success.
export function mono_wasm_prepare_icu_data (offset: VoidPtr) {
    icuDataOffset = offset;
}

export function mono_wasm_load_icu_data () {
    if (icuDataOffset === null) {
        return;
    }
    if (!cwraps.mono_wasm_load_icu_data(icuDataOffset!)) {
        throw new Error("Failed to load ICU data");
    }
}
