// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-console */
/* eslint-disable no-inner-declarations */
import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, stringToUTF16 } from "../strings";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";

const OUTER_SEPARATOR = "|||";
const INNER_SEPARATOR = "||";

// this function joing all calendar info with OUTER_SEPARATOR into one string and returns back to managed code
export function mono_wasm_get_calendar_info(culture: MonoStringRef, dst: number, dstLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number
{
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const calendarInfo = {
            YearMonth: "",
            MonthDay: "",
            LongDates: "",
            DayNames: "",
            AbbreviatedDayNames: "",
            ShortestDayNames: "",
            MonthNames: "",
            AbbreviatedMonthNames: "",
        };
        const year = 999;
        const month = 11;
        const day = 22;
        const date = new Date(year, month - 1, day);
        const dayNames = getDayNames(locale);
        calendarInfo.DayNames = dayNames.long.join(INNER_SEPARATOR);
        calendarInfo.AbbreviatedDayNames = dayNames.abbreviated.join(INNER_SEPARATOR);
        calendarInfo.ShortestDayNames = dayNames.shortest.join(INNER_SEPARATOR);
        const monthNames = getMonthNames(locale);
        calendarInfo.MonthNames = monthNames.long.join(INNER_SEPARATOR);
        calendarInfo.AbbreviatedMonthNames = monthNames.abbreviated.join(INNER_SEPARATOR);
        calendarInfo.YearMonth = getMonthYearPattern(locale, date);
        calendarInfo.MonthDay = getMonthDayPattern(locale, date);
        calendarInfo.LongDates = getLongDatePattern(locale, date);
       
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
    pattern = pattern.replace(monthName, "MMMM");
    pattern = pattern.replace("999", "yyyy");
    // sometimes the number is localized and the above does not have an effect
    const yearStr = date.toLocaleDateString(locale, { year: "numeric" });
    return pattern.replace(yearStr, "yyyy");
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
    pattern = pattern.replace(replacedMonthName, "MMMM");
    pattern = pattern.replace("22", "d");
    const dayStr = formatWithoutMonthName.format(date);
    return pattern.replace(dayStr, "d");
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
        pattern = pattern.replace(replacedMonthName, "MMMM");            
    }
    pattern = pattern.replace("999", "yyyy");
    // sometimes the number is localized and the above does not have an effect,
    // so additionally, we need to do:
    const yearStr = date.toLocaleDateString(locale, { year: "numeric" });
    pattern = pattern.replace(yearStr, "yyyy");
    const weekday = date.toLocaleDateString(locale, { weekday: "long" }).toLowerCase();
    const replacedWeekday = getGenitiveForName(date, pattern, weekday, new Intl.DateTimeFormat(locale, { year: "numeric", month: "long", day: "numeric"}));
    pattern = pattern.replace(replacedWeekday, "dddd");
    pattern = pattern.replace("22", "d");
    const dayStr = date.toLocaleDateString(locale, { day: "numeric" }); // should we replace it for localized digits?
    return pattern.replace(dayStr, "d");
}

function getGenitiveForName(date: Date, pattern: string, name: string, formatWithoutName: Intl.DateTimeFormat)
{
    let replacedName = name;
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
        // replacedName = 'lapkričio'
        const patternWithoutName = formatWithoutName.format(date).toLowerCase();
        replacedName = pattern.split(/,| /).filter(x => !patternWithoutName.split(/,| /).includes(x) && x[0] == name[0])[0];
    }
    return replacedName;
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

function getMonthNames(locale: string | undefined) : { long: string[], abbreviated: string[] }
{
    const localeLang = locale?.split("-")[0];
    const firstMonthShift = localeLang == "ar" ? 8 : localeLang == "fa" ? 3 : 0; //first month: Muharram or Farwardin or January
    const month = new Date(2021, firstMonthShift, 1);
    const months = [ month.toLocaleDateString(locale, { month: "long" }) ];
    const monthsAbb = [ month.toLocaleDateString(locale, { month: "short" }) ];
    for(let i = 1 + firstMonthShift; i < 13 + firstMonthShift; i++)
    {
        const monthCnt = i % 12;
        month.setMonth(monthCnt);
        const shortMonth = month.toLocaleDateString(locale, { month: "short" });
        if (shortMonth == monthsAbb[0])
        {
            break;
        }
        monthsAbb[i - firstMonthShift] = shortMonth;
        months[i - firstMonthShift] = month.toLocaleDateString(locale, { month: "long" });
    }
    return {long: months, abbreviated: monthsAbb };
}

// const date = new Date(year, month - 1, 22, 2, 3, 4)
// let pattern = new Intl.DateTimeFormat(locale, { weekday: "long", year: "numeric", month: "long", day: "numeric", hour: "numeric", minute: "numeric", second: "numeric"}).format(date);
// pattern = pattern.replace("02", "HH");
// pattern = pattern.replace("2", "h"); // try localized version as well
// pattern = pattern.replace("03", "mm"); // try localized version as well
// pattern = pattern.replace("04", "ss"); // try localized version as well
// // tt AM/PM - how to catch it?