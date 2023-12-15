// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-inner-declarations */
import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, stringToUTF16 } from "../strings";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { INNER_SEPARATOR, OUTER_SEPARATOR, normalizeSpaces } from "./helpers";

const MONTH_CODE = "MMMM";
const YEAR_CODE = "yyyy";
const DAY_CODE = "d";

// this function joins all calendar info with OUTER_SEPARATOR into one string and returns it back to managed code
export function mono_wasm_get_calendar_info(culture: MonoStringRef, calendarId: number, dst: number, dstLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number
{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const calendarInfo = {
            EnglishName: "",
            YearMonth: "",
            MonthDay: "",
            LongDates: "",
            ShortDates: "",
            EraNames: "",
            AbbreviatedEraNames: "",
            DayNames: "",
            AbbreviatedDayNames: "",
            ShortestDayNames: "",
            MonthNames: "",
            AbbreviatedMonthNames: "",
            MonthGenitiveNames: "",
            AbbrevMonthGenitiveNames: "",
        };
        const date = new Date(999, 10, 22); // Fri Nov 22 0999 00:00:00 GMT+0124 (Central European Standard Time)
        calendarInfo.EnglishName = getCalendarName(locale);
        const dayNames = getDayNames(locale);
        calendarInfo.DayNames = dayNames.long.join(INNER_SEPARATOR);
        calendarInfo.AbbreviatedDayNames = dayNames.abbreviated.join(INNER_SEPARATOR);
        calendarInfo.ShortestDayNames = dayNames.shortest.join(INNER_SEPARATOR);
        const monthNames = getMonthNames(locale);
        calendarInfo.MonthNames = monthNames.long.join(INNER_SEPARATOR);
        calendarInfo.AbbreviatedMonthNames = monthNames.abbreviated.join(INNER_SEPARATOR);
        calendarInfo.MonthGenitiveNames = monthNames.longGenitive.join(INNER_SEPARATOR);
        calendarInfo.AbbrevMonthGenitiveNames = monthNames.abbreviatedGenitive.join(INNER_SEPARATOR);
        calendarInfo.YearMonth = getMonthYearPattern(locale, date);
        calendarInfo.MonthDay = getMonthDayPattern(locale, date);
        calendarInfo.ShortDates = getShortDatePattern(locale);
        calendarInfo.LongDates = getLongDatePattern(locale, date);
        const eraNames = getEraNames(date, locale, calendarId);
        calendarInfo.EraNames = eraNames.eraNames;
        calendarInfo.AbbreviatedEraNames = eraNames.abbreviatedEraNames;

        const result = Object.values(calendarInfo).join(OUTER_SEPARATOR);
        if (result.length > dstLength)
        {
            throw new Error(`Calendar info exceeds length of ${dstLength}.`);
        }
        stringToUTF16(dst, dst + 2 * result.length, result);
        wrap_no_error_root(isException, exceptionRoot);
        return result.length;
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

function getCalendarName(locale: any){
    const calendars = getCalendarInfo(locale);
    if (!calendars || calendars.length == 0)
        return "";
    return calendars[0];
}

function getCalendarInfo(locale: string)
{
    try {
        // most tools have it implemented as a property
        return (new Intl.Locale(locale) as any).calendars;
    }
    catch {
        try {
            // but a few use methods, which is the preferred way
            return (new Intl.Locale(locale) as any).getCalendars();
        }
        catch
        {
            return undefined;
        }
    }
}

function getMonthYearPattern(locale: string | undefined, date: Date): string
{
    let pattern = date.toLocaleDateString(locale, { year: "numeric", month: "long" }).toLowerCase();
    // pattern has month name as string or as number
    const monthName = date.toLocaleString(locale, { month: "long" }).toLowerCase().trim();
    if (monthName.charAt(monthName.length - 1) == "\u6708")
    {
        // Chineese-like patterns:
        return "yyyy\u5e74M\u6708";
    }
    pattern = pattern.replace(monthName, MONTH_CODE);
    pattern = pattern.replace("999", YEAR_CODE);
    // sometimes the number is localized and the above does not have an effect
    const yearStr = date.toLocaleDateString(locale, { year: "numeric" });
    pattern = normalizeSpaces(pattern);
    return pattern.replace(yearStr, YEAR_CODE);
}

function getMonthDayPattern(locale: string | undefined, date: Date): string
{
    let pattern = date.toLocaleDateString(locale, { month: "long", day: "numeric"}).toLowerCase();
    // pattern has month name as string or as number
    const monthName = date.toLocaleString(locale, { month: "long" }).toLowerCase().trim();
    if (monthName.charAt(monthName.length - 1) == "\u6708")
    {
        // Chineese-like patterns:
        return "M\u6708d\u65e5";
    }
    const formatWithoutMonthName = new Intl.DateTimeFormat(locale, { day: "numeric" });
    const replacedMonthName = getGenitiveForName(date, pattern, monthName, formatWithoutMonthName);
    pattern = pattern.replace(replacedMonthName, MONTH_CODE);
    pattern = pattern.replace("22", DAY_CODE);
    const dayStr = formatWithoutMonthName.format(date);
    return pattern.replace(dayStr, DAY_CODE);
}

function getShortDatePattern(locale: string | undefined): string
{
    if (locale?.substring(0, 2) == "fa")
    {
        // persian calendar is shifted and it has no lapping dates with
        // arabic and gregorian calendars, so that both day and month would be < 10
        return "yyyy/M/d";
    }
    const year = 2014;
    const month = 1;
    const day = 2;
    const date = new Date(year, month - 1, day); // arabic: 1/3/1435
    const longYearStr = "2014";
    const shortYearStr = "14";
    const longMonthStr = "01";
    const shortMonthStr = "1";
    const longDayStr = "02";
    const shortDayStr = "2";
    let pattern = date.toLocaleDateString(locale, {dateStyle: "short"});
    // each date part might be in localized numbers or standard arabic numbers
    // toLocaleDateString returns not compatible data,
    // e.g. { dateStyle: "short" } sometimes contains localized year number
    // while { year: "numeric" } contains non-localized year number and vice versa
    if (pattern.includes(shortYearStr))
    {
        pattern = pattern.replace(longYearStr, YEAR_CODE);
        pattern = pattern.replace(shortYearStr, YEAR_CODE);
    }
    else
    {
        const yearStr = date.toLocaleDateString(locale, { year: "numeric" });
        const yearStrShort = yearStr.substring(yearStr.length - 2, yearStr.length);
        pattern = pattern.replace(yearStr, YEAR_CODE);
        if (yearStrShort)
            pattern = pattern.replace(yearStrShort, YEAR_CODE);
    }

    if (pattern.includes(shortMonthStr))
    {
        pattern = pattern.replace(longMonthStr, "MM");
        pattern = pattern.replace(shortMonthStr, "M");
    }
    else
    {
        const monthStr = date.toLocaleDateString(locale, { month: "numeric" });
        const localizedMonthCode = monthStr.length == 1 ? "M" : "MM";
        pattern = pattern.replace(monthStr, localizedMonthCode);
    }

    if (pattern.includes(shortDayStr))
    {
        pattern = pattern.replace(longDayStr, "dd");
        pattern = pattern.replace(shortDayStr, "d");
    }
    else
    {
        const dayStr = date.toLocaleDateString(locale, { day: "numeric" });
        const localizedDayCode = dayStr.length == 1 ? "d" : "dd";
        pattern = pattern.replace(dayStr, localizedDayCode);
    }
    return normalizeSpaces(pattern);
}

function getLongDatePattern(locale: string | undefined, date: Date): string
{
    if (locale == "th-TH")
    {
        // cannot be caught with regexes
        return "ddddที่ d MMMM g yyyy";
    }
    let pattern = new Intl.DateTimeFormat(locale, { weekday: "long", year: "numeric", month: "long", day: "numeric"}).format(date).toLowerCase();
    const monthName = date.toLocaleString(locale, { month: "long" }).trim().toLowerCase();

    // pattern has month name as string or as number
    const monthSuffix = monthName.charAt(monthName.length - 1);
    if (monthSuffix == "\u6708" || monthSuffix == "\uc6d4")
    {
        // Asian-like patterns:
        const shortMonthName = date.toLocaleString(locale, { month: "short" });
        pattern = pattern.replace(shortMonthName, `M${monthSuffix}`);
    }
    else
    {
        const replacedMonthName = getGenitiveForName(date, pattern, monthName, new Intl.DateTimeFormat(locale, { weekday: "long", year: "numeric", day: "numeric"}));
        pattern = pattern.replace(replacedMonthName, MONTH_CODE);
    }
    pattern = pattern.replace("999", YEAR_CODE);
    // sometimes the number is localized and the above does not have an effect,
    // so additionally, we need to do:
    const yearStr = date.toLocaleDateString(locale, { year: "numeric" });
    pattern = pattern.replace(yearStr, YEAR_CODE);
    const weekday = date.toLocaleDateString(locale, { weekday: "long" }).toLowerCase();
    const replacedWeekday = getGenitiveForName(date, pattern, weekday, new Intl.DateTimeFormat(locale, { year: "numeric", month: "long", day: "numeric"}));
    pattern = pattern.replace(replacedWeekday, "dddd");
    pattern = pattern.replace("22", DAY_CODE);
    const dayStr = date.toLocaleDateString(locale, { day: "numeric" }); // should we replace it for localized digits?
    pattern = normalizeSpaces(pattern);
    return pattern.replace(dayStr, DAY_CODE);
}

function getGenitiveForName(date: Date, pattern: string, name: string, formatWithoutName: Intl.DateTimeFormat)
{
    let genitiveName = name;
    const nameStart = pattern.indexOf(name);
    if (nameStart == -1 ||
        // genitive month name can include monthName and monthName can include spaces, e.g. "tháng 11":, so we cannot use pattern.includes() or pattern.split(" ").includes()
        (nameStart != -1 && pattern.length > nameStart + name.length && pattern[nameStart + name.length] != " " && pattern[nameStart + name.length] != "," && pattern[nameStart + name.length] != "\u060c"))
    {
        // needs to be in Genitive form to be useful
        // e.g.
        // pattern = '999 m. lapkričio 22 d., šeštadienis',
        // patternWithoutName = '999 2, šeštadienis',
        // name = 'lapkritis'
        // genitiveName = 'lapkričio'
        const patternWithoutName = formatWithoutName.format(date).toLowerCase();
        genitiveName = pattern.split(/,| /).filter(x => !patternWithoutName.split(/,| /).includes(x) && x[0] == name[0])[0];
    }
    return genitiveName;
}

function getDayNames(locale: string | undefined) : { long: string[], abbreviated: string[], shortest: string[] }
{
    const weekDay = new Date(2023, 5, 25); // Sunday
    const dayNames = [];
    const dayNamesAbb = [];
    const dayNamesSS = [];
    for(let i=0; i<7; i++)
    {
        dayNames[i] = weekDay.toLocaleDateString(locale, { weekday: "long" });
        dayNamesAbb[i] = weekDay.toLocaleDateString(locale, { weekday: "short" });
        dayNamesSS[i] = weekDay.toLocaleDateString(locale, { weekday: "narrow" });
        weekDay.setDate(weekDay.getDate() + 1);
    }
    return {long: dayNames, abbreviated: dayNamesAbb, shortest: dayNamesSS };
}

function getMonthNames(locale: string | undefined) : { long: string[], abbreviated: string[], longGenitive: string[], abbreviatedGenitive: string[] }
{
    // some calendars have the first month on non-0 index in JS
    // first month: Muharram ("ar") or Farwardin ("fa") or January
    const localeLang = locale ? locale.split("-")[0] : "";
    const firstMonthShift = localeLang == "ar" ? 8 : localeLang == "fa" ? 3 : 0;
    const date = new Date(2021, firstMonthShift, 1);
    const months: string[] = [];
    const monthsAbb: string[] = [];
    const monthsGen: string[] = [];
    const monthsAbbGen: string[] = [];
    let isChineeseStyle, isShortFormBroken;
    for(let i = firstMonthShift; i < 12 + firstMonthShift; i++)
    {
        const monthCnt = i % 12;
        date.setMonth(monthCnt);

        const monthNameLong = date.toLocaleDateString(locale, { month: "long" });
        const monthNameShort = date.toLocaleDateString(locale, { month: "short" });
        months[i - firstMonthShift] = monthNameLong;
        monthsAbb[i - firstMonthShift] = monthNameShort;
        // for Genitive forms:
        isChineeseStyle = isChineeseStyle ?? monthNameLong.charAt(monthNameLong.length - 1) == "\u6708";
        if (isChineeseStyle)
        {
            // for Chinese-like calendar's Genitive = Nominative
            monthsGen[i - firstMonthShift] = monthNameLong;
            monthsAbbGen[i - firstMonthShift] = monthNameShort;
            continue;
        }
        const formatWithoutMonthName = new Intl.DateTimeFormat(locale, { day: "numeric" });
        const monthWithDayLong = date.toLocaleDateString(locale, { month: "long", day: "numeric"});
        monthsGen[i - firstMonthShift] = getGenitiveForName(date, monthWithDayLong, monthNameLong, formatWithoutMonthName);
        isShortFormBroken = isShortFormBroken ?? /^\d+$/.test(monthNameShort);
        if (isShortFormBroken)
        {
            // for buggy locales e.g. lt-LT, short month contains only number instead of string
            // we leave Genitive = Nominative
            monthsAbbGen[i - firstMonthShift] = monthNameShort;
            continue;
        }
        const monthWithDayShort = date.toLocaleDateString(locale, { month: "short", day: "numeric"});
        monthsAbbGen[i - firstMonthShift] = getGenitiveForName(date, monthWithDayShort, monthNameShort, formatWithoutMonthName);
    }
    return {long: months, abbreviated: monthsAbb, longGenitive: monthsGen, abbreviatedGenitive: monthsAbbGen };
}

// .NET expects that only the Japanese calendars have more than 1 era.
// So for other calendars, only return the latest era.
function getEraNames(date: Date, locale: string | undefined, calendarId: number) : { eraNames: string, abbreviatedEraNames: string}
{
    if (shouldBePopulatedByManagedCode(calendarId))
    {
        // managed code already handles these calendars,
        // so empty strings will get overwritten in
        // InitializeEraNames/InitializeAbbreviatedEraNames
        return {
            eraNames: "",
            abbreviatedEraNames: ""
        };
    }
    const yearStr = date.toLocaleDateString(locale, { year: "numeric" });
    const dayStr = date.toLocaleDateString(locale, { day: "numeric" });
    const eraDate = date.toLocaleDateString(locale, { era: "short" });
    const shortEraDate = date.toLocaleDateString(locale, { era: "narrow" });

    const eraDateParts = eraDate.includes(yearStr) ?
        getEraDateParts(yearStr) :
        getEraDateParts(date.getFullYear().toString());

    return {
        eraNames: getEraFromDateParts(eraDateParts.eraDateParts, eraDateParts.ignoredPart),
        abbreviatedEraNames: getEraFromDateParts(eraDateParts.abbrEraDateParts, eraDateParts.ignoredPart)
    };

    function shouldBePopulatedByManagedCode(calendarId: number)
    {
        return (calendarId > 1 && calendarId < 15) || calendarId == 22 || calendarId == 23;
    }

    function getEraFromDateParts(dateParts: string[], ignoredPart: string) : string
    {
        const regex = new RegExp(`^((?!${ignoredPart}|[0-9]).)*$`);
        const filteredEra = dateParts.filter(part => regex.test(part));
        if (filteredEra.length == 0)
            throw new Error(`Internal error, era for locale ${locale} was in non-standard format.`);
        return filteredEra[0].trim();
    }

    function getEraDateParts(yearStr: string)
    {
        if (eraDate.startsWith(yearStr) || eraDate.endsWith(yearStr))
        {
            return {
                eraDateParts: eraDate.split(dayStr),
                abbrEraDateParts: shortEraDate.split(dayStr),
                ignoredPart: yearStr,
            };
        }
        return {
            eraDateParts: eraDate.split(yearStr),
            abbrEraDateParts: shortEraDate.split(yearStr),
            ignoredPart: dayStr,
        };
    }
}
