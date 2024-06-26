import { GlobalizationHelpers, RuntimeHelpers } from "../types/internal";
import { mono_wasm_get_calendar_info } from "./calendar";
import { mono_wasm_change_case } from "./change-case";
import { mono_wasm_compare_string, mono_wasm_starts_with, mono_wasm_ends_with, mono_wasm_index_of } from "./collations";
import { mono_wasm_get_culture_info } from "./culture-info";
import { setSegmentationRulesFromJson } from "./grapheme-segmenter";
import { mono_wasm_get_first_day_of_week, mono_wasm_get_first_week_of_year } from "./locales";

export let globalizationHelpers: GlobalizationHelpers;
export let runtimeHelpers: RuntimeHelpers;

export function initHybrid (gh: GlobalizationHelpers, rh: RuntimeHelpers) {
    gh.mono_wasm_change_case = mono_wasm_change_case;
    gh.mono_wasm_compare_string = mono_wasm_compare_string;
    gh.mono_wasm_starts_with = mono_wasm_starts_with;
    gh.mono_wasm_ends_with = mono_wasm_ends_with;
    gh.mono_wasm_index_of = mono_wasm_index_of;
    gh.mono_wasm_get_calendar_info = mono_wasm_get_calendar_info;
    gh.mono_wasm_get_culture_info = mono_wasm_get_culture_info;
    gh.mono_wasm_get_first_day_of_week = mono_wasm_get_first_day_of_week;
    gh.mono_wasm_get_first_week_of_year = mono_wasm_get_first_week_of_year;
    gh.setSegmentationRulesFromJson = setSegmentationRulesFromJson;
    globalizationHelpers = gh;
    runtimeHelpers = rh;
}

