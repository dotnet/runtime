// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { VoidPtr } from "./types/emscripten";

export function wasm_load_icu_data (offset: VoidPtr) {
    if (!cwraps.wasm_load_icu_data(offset)) {
        throw new Error("Failed to load ICU data");
    }
}
