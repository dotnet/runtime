// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString } from "../strings";
import { Int32Ptr } from "../types/emscripten";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { normalizeLocale } from "./helpers";

export function mono_wasm_get_first_day_of_week(culture: MonoStringRef, isException: Int32Ptr, exAddress: MonoObjectRef): number{

    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const canonicalLocale = normalizeLocale(cultureName);
        wrap_no_error_root(isException, exceptionRoot);
        return getFirstDayOfWeek(canonicalLocale);
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_get_first_week_of_year(culture: MonoStringRef, isException: Int32Ptr, exAddress: MonoObjectRef): number{

    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const canonicalLocale = normalizeLocale(cultureName);
        wrap_no_error_root(isException, exceptionRoot);
        return getFirstWeekOfYear(canonicalLocale);
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        exceptionRoot.release();
    }
}

function getFirstDayOfWeek(locale: string)
{
    const weekInfo = getWeekInfo(locale);
    if (weekInfo)
    {
        // JS's Sunday == 7 while dotnet's Sunday == 0
        return weekInfo.firstDay == 7 ? 0 : weekInfo.firstDay;
    }
    // Firefox does not support it rn but we can make a temporary workaround for it,
    // that should be removed when it starts being supported:
    const saturdayLocales = [ "en-AE", "en-SD", "fa-IR" ];
    if (saturdayLocales.includes(locale))
    {
        return 6;
    }
    const sundayLanguages = [ "zh", "th", "pt", "mr", "ml", "ko", "kn", "ja", "id", "hi", "he", "gu", "fil", "bn", "am", "ar" ];
    const sundayLocales = [ "ta-SG", "ta-IN", "sw-KE", "ms-SG", "fr-CA", "es-MX", "en-US", "en-ZW", "en-ZA", "en-WS", "en-VI", "en-UM", "en-TT", "en-SG", "en-PR", "en-PK", "en-PH", "en-MT", "en-MO", "en-MH", "en-KE", "en-JM", "en-IN", "en-IL", "en-HK", "en-GU", "en-DM", "en-CA", "en-BZ", "en-BW", "en-BS", "en-AU", "en-AS", "en-AG" ];
    const localeLang = locale.split("-")[0];
    if (sundayLanguages.includes(localeLang) || sundayLocales.includes(locale))
    {
        return 0;
    }
    return 1;
}

function getFirstWeekOfYear(locale: string)
{
    const weekInfo = getWeekInfo(locale);
    if (weekInfo)
    {
        // enum CalendarWeekRule
        // FirstDay = 0,           // when minimalDays < 4
        // FirstFullWeek = 1,      // when miminalDays == 7
        // FirstFourDayWeek = 2    // when miminalDays >= 4
        return weekInfo.minimalDays == 7 ? 1 :
            weekInfo.minimalDays < 4 ? 0 : 2;
    }
    // Firefox does not support it rn but we can make a temporary workaround for it,
    // that should be removed when it starts being supported:
    const firstFourDayWeekLocales = [ "pt-PT", "fr-CH", "fr-FR", "fr-BE", "es-ES", "en-SE", "en-NL", "en-JE", "en-IM", "en-IE", "en-GI", "en-GG", "en-GB", "en-FJ", "en-FI", "en-DK", "en-DE", "en-CH", "en-BE", "en-AT", "el-GR" ];
    const firstFourDayWeekLanguages = [ "sv", "sk", "ru", "pl", "nl", "no", "lt", "it", "hu", "fi", "et", "de", "da", "cs", "ca", "bg" ];
    const localeLang = locale.split("-")[0];
    if (firstFourDayWeekLocales.includes(locale) || firstFourDayWeekLanguages.includes(localeLang))
    {
        return 2;
    }
    return 0;
}

function getWeekInfo(locale: string)
{
    try {
        // most tools have it implemented as property
        return (new Intl.Locale(locale) as any).weekInfo;
    }
    catch {
        try {
            // but a few use methods, which is the preferred way
            return (new Intl.Locale(locale) as any).getWeekInfo();
        }
        catch
        {
            return undefined;
        }
    }
}