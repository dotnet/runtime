// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_get_locale_info } from "./globalization-locale";

// JS-based globalization support for WebAssembly

export const mono_wasm_js_globalization_imports = [
    mono_wasm_get_locale_info,
];
