// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { globalizationHelpers } from "./globals";
import { Int32Ptr, VoidPtr } from "./types/emscripten";
import { VoidPtrNull } from "./types/internal";

export function mono_wasm_change_case (culture: number, cultureLength: number, src: number, srcLength: number, dst: number, dstLength: number, toUpper: number) : VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_change_case === "function") {
        return globalizationHelpers.mono_wasm_change_case(culture, cultureLength, src, srcLength, dst, dstLength, toUpper);
    }
    return VoidPtrNull;
}

export function mono_wasm_compare_string (culture: number, cultureLength: number, str1: number, str1Length: number, str2: number, str2Length: number, options: number, resultPtr: Int32Ptr) : VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_compare_string === "function") {
        return globalizationHelpers.mono_wasm_compare_string(culture, cultureLength, str1, str1Length, str2, str2Length, options, resultPtr);
    }
    return VoidPtrNull;
}

export function mono_wasm_starts_with (culture: number, cultureLength: number, str1: number, str1Length: number, str2: number, str2Length: number, options: number, resultPtr: Int32Ptr): VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_starts_with === "function") {
        return globalizationHelpers.mono_wasm_starts_with(culture, cultureLength, str1, str1Length, str2, str2Length, options, resultPtr);
    }
    return VoidPtrNull;
}

export function mono_wasm_ends_with (culture: number, cultureLength: number, str1: number, str1Length: number, str2: number, str2Length: number, options: number, resultPtr: Int32Ptr): VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_ends_with === "function") {
        return globalizationHelpers.mono_wasm_ends_with(culture, cultureLength, str1, str1Length, str2, str2Length, options, resultPtr);
    }
    return VoidPtrNull;
}

export function mono_wasm_index_of (culture: number, cultureLength: number, needlePtr: number, needleLength: number, srcPtr: number, srcLength: number, options: number, fromBeginning: number, resultPtr: Int32Ptr): VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_index_of === "function") {
        return globalizationHelpers.mono_wasm_index_of(culture, cultureLength, needlePtr, needleLength, srcPtr, srcLength, options, fromBeginning, resultPtr);
    }
    return VoidPtrNull;
}

export function mono_wasm_get_calendar_info (culture: number, cultureLength: number, calendarId: number, dst: number, dstMaxLength: number, dstLength: Int32Ptr): VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_get_calendar_info === "function") {
        return globalizationHelpers.mono_wasm_get_calendar_info(culture, cultureLength, calendarId, dst, dstMaxLength, dstLength);
    }
    return VoidPtrNull;
}

export function mono_wasm_get_culture_info (culture: number, cultureLength: number, dst: number, dstMaxLength: number, dstLength: Int32Ptr): VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_get_culture_info === "function") {
        return globalizationHelpers.mono_wasm_get_culture_info(culture, cultureLength, dst, dstMaxLength, dstLength);
    }
    return VoidPtrNull;
}

export function mono_wasm_get_first_day_of_week (culture: number, cultureLength: number, resultPtr: Int32Ptr): VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_get_first_day_of_week === "function") {
        return globalizationHelpers.mono_wasm_get_first_day_of_week(culture, cultureLength, resultPtr);
    }
    return VoidPtrNull;
}

export function mono_wasm_get_first_week_of_year (culture: number, cultureLength: number, resultPtr: Int32Ptr): VoidPtr {
    if (typeof globalizationHelpers.mono_wasm_get_first_week_of_year === "function") {
        return globalizationHelpers.mono_wasm_get_first_week_of_year(culture, cultureLength, resultPtr);
    }
    return VoidPtrNull;
}
