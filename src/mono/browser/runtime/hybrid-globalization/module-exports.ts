import { RuntimeHelpers } from "../types/internal";
import { mono_wasm_get_calendar_info } from "./calendar";
import { mono_wasm_change_case, mono_wasm_change_case_invariant } from "./change-case";
import { mono_wasm_compare_string, mono_wasm_starts_with, mono_wasm_ends_with, mono_wasm_index_of } from "./collations";
import { mono_wasm_get_culture_info } from "./culture-info";
import { setSegmentationRulesFromJson } from "./grapheme-segmenter";
import { mono_wasm_get_first_day_of_week, mono_wasm_get_first_week_of_year } from "./locales";

export let runtimeHelpers: RuntimeHelpers;

export function initHybrid (rh: RuntimeHelpers) {
    rh.mono_wasm_change_case_invariant = mono_wasm_change_case_invariant;
    rh.mono_wasm_change_case = mono_wasm_change_case;
    rh.mono_wasm_compare_string = mono_wasm_compare_string;
    rh.mono_wasm_starts_with = mono_wasm_starts_with;
    rh.mono_wasm_ends_with = mono_wasm_ends_with;
    rh.mono_wasm_index_of = mono_wasm_index_of;
    rh.mono_wasm_get_calendar_info = mono_wasm_get_calendar_info;
    rh.mono_wasm_get_culture_info = mono_wasm_get_culture_info;
    rh.mono_wasm_get_first_day_of_week = mono_wasm_get_first_day_of_week;
    rh.mono_wasm_get_first_week_of_year = mono_wasm_get_first_week_of_year;
    rh.setSegmentationRulesFromJson = setSegmentationRulesFromJson;
    runtimeHelpers = rh;
}

