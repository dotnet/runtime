// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_change_case, mono_wasm_compare_string, mono_wasm_ends_with, mono_wasm_get_calendar_info, mono_wasm_get_culture_info, mono_wasm_get_first_day_of_week, mono_wasm_get_first_week_of_year, mono_wasm_index_of, mono_wasm_starts_with } from "./globalization-stubs";
import { mono_wasm_get_locale_info } from "./locales-common";

export const mono_wasm_hybrid_globalization_imports = [
    mono_wasm_change_case,
    mono_wasm_compare_string,
    mono_wasm_starts_with,
    mono_wasm_ends_with,
    mono_wasm_index_of,
    mono_wasm_get_calendar_info,
    mono_wasm_get_culture_info,
    mono_wasm_get_first_day_of_week,
    mono_wasm_get_first_week_of_year,
    // used by both: non-HG and HG:
    mono_wasm_get_locale_info,
];
