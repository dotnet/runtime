// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { VoidPtr } from "./types/emscripten";

// @offset must be the address of an ICU data archive in the native heap.
// returns true on success.
export function mono_wasm_load_icu_data(offset: VoidPtr): boolean {
    return (cwraps.mono_wasm_load_icu_data(offset)) === 1;
}
