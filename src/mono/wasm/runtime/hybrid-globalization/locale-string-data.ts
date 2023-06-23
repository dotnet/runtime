// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_external_root } from "../roots";
import { monoStringToString, stringToUTF16 } from "../strings";
import { MonoObject, MonoObjectRef, MonoString, MonoStringRef } from "../types/internal";
import { Int32Ptr } from "../types/emscripten";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";

export function mono_wasm_get_monetary_symbol(culture: MonoStringRef, iosSymbol: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        isoSymbolRoot = mono_wasm_new_external_root<MonoString>(iosSymbol),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const isoSymbolName = monoStringToString(isoSymbolRoot);
        if (!isoSymbolName)
            throw new Error("ISO symbol name has to have a value.");
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const getCurrencySymbol = (locale: Intl.LocalesArgument, currency: string) => 
            (0).toLocaleString(locale, { style: "currency", currency, minimumFractionDigits: 0, maximumFractionDigits: 0 }).replace(/\d/g, "").trim();
        const result = getCurrencySymbol(locale, isoSymbolName);
        if (result.length > bufferLength) // quite impossible
        {
            throw new Error(`Monetary symbol exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result.length, result);
        wrap_no_error_root(isException, exceptionRoot);
        return result.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        isoSymbolRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_get_monetary_decimal_separator(culture: MonoStringRef, iosSymbol: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        isoSymbolRoot = mono_wasm_new_external_root<MonoString>(iosSymbol),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const isoSymbolName = monoStringToString(isoSymbolRoot);
        if (!isoSymbolName)
            throw new Error("ISO symbol name has to have a value.");
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const getCurrencySeparator = (locale: Intl.LocalesArgument, currency: any) => (0.2).toLocaleString(locale, { style: "currency", currency, minimumFractionDigits: 0, maximumFractionDigits: 1 }).split(decimalSeparatorRegex(locale, 0, 2))[1];
        const result = getCurrencySeparator(locale, isoSymbolName);
        if (result.length > bufferLength) // quite impossible
        {
            throw new Error(`Monetary decimal separator exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result.length, result);
        wrap_no_error_root(isException, exceptionRoot);
        return result.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        isoSymbolRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_get_number_decimal_separator(culture: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const result = (0.2).toLocaleString(locale).split(decimalSeparatorRegex(locale, 0, 2))[1];
        if (result.length > bufferLength) // quite impossible
        {
            throw new Error(`Number decimal separator exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result.length, result);
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

export function mono_wasm_get_monetary_thousand_separator(culture: MonoStringRef, iosSymbol: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        isoSymbolRoot = mono_wasm_new_external_root<MonoString>(iosSymbol),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const isoSymbolName = monoStringToString(isoSymbolRoot);
        if (!isoSymbolName)
            throw new Error("ISO symbol name has to have a value.");
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const getCurrencyThousandSeparator = (locale: Intl.LocalesArgument, currency: any) => (123456).toLocaleString(locale, { style: "currency", currency }).split(decimalSeparatorRegex(locale, 3, 4))[1];
        const result = getCurrencyThousandSeparator(locale, isoSymbolName);
        if (result.length > bufferLength) // quite impossible
        {
            throw new Error(`Monetary thousand separator exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result.length, result);
        wrap_no_error_root(isException, exceptionRoot);
        return result.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        isoSymbolRoot.release();
        exceptionRoot.release();
    }
}

export function mono_wasm_get_number_thousand_separator(culture: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const result = (123456).toLocaleString(locale).split(decimalSeparatorRegex(locale, 3, 4))[1];
        if (result.length > bufferLength) // quite impossible
        {
            throw new Error(`Thousand separator exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result.length, result);
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

const numberToLocaleString = (locale: Intl.LocalesArgument, num: number) => (num).toLocaleString(locale);
const decimalSeparatorRegex = (locale: Intl.LocalesArgument, num1: number, num2: number) => new RegExp(`[${numberToLocaleString(locale, num1)} ${numberToLocaleString(locale, num2)}]`);

export function mono_wasm_get_digits(culture: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        let result = "";
        for (let i=0; i<10; i++)
            result += `${numberToLocaleString(locale, i)}\uFFFF`;
        if (result.length > bufferLength) // quite impossible
        {
            throw new Error(`Digits exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result.length, result);
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

export function mono_wasm_get_currency_name(culture: MonoStringRef, iosSymbol: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        isoSymbolRoot = mono_wasm_new_external_root<MonoString>(iosSymbol),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const isoSymbolName = monoStringToString(isoSymbolRoot);
        if (!isoSymbolName)
            throw new Error("ISO symbol name has to have a value.");
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        const getCurrencyFullName = (locale: Intl.LocalesArgument, currency: any) => (1).toLocaleString(locale, { style: "currency", currency, currencyDisplay: "name", }).replace(/\d.*\d/g, "").trim();
        const result = getCurrencyFullName(locale, isoSymbolName);
        if (result.length > bufferLength) // quite impossible
        {
            throw new Error(`Currency name exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result.length, result);
        wrap_no_error_root(isException, exceptionRoot);
        return result.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        isoSymbolRoot.release();
        exceptionRoot.release();
    }
}

// for AM/PM designators:
// const month = 3;
// const event = new Date(2008, month-1, 1, 7, 0, 6);
// const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };

// console.log(event.toLocaleTimeString('en-US', options));
// //"Saturday, March 1, 2008 at 7:00:06 AM"

// console.log(event.toLocaleTimeString('pl-PL', options));
// // "sobota, 1 marca 2008 07:00:06"

// console.log(event.toLocaleTimeString('ar-EG', options));
// // "السبت، ١ مارس ٢٠٠٨ في ٧:٠٠:٠٦ ص"

export function mono_wasm_get_country_name(culture: MonoStringRef, region: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        regionRoot = mono_wasm_new_external_root<MonoString>(region),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const regionName = monoStringToString(regionRoot);
        if (!regionName)
            throw new Error("Region name has to have a value.");
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        if (locale == undefined)
        {
            throw new Error("Locale name is undefined.");
        }
        const result = getCountryName(locale, regionName);
        if (result!.length > bufferLength) // quite impossible
        {
            throw new Error(`Country name exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result!.length, result!);
        wrap_no_error_root(isException, exceptionRoot);
        return result!.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        regionRoot.release();
        exceptionRoot.release();
    }
}

const getCountryName = (locale: string, regionName: string) => new Intl.DisplayNames([locale], {type: "region"}).of(regionName);

export function mono_wasm_get_language_name(culture: MonoStringRef, region: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        regionRoot = mono_wasm_new_external_root<MonoString>(region),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const regionName = monoStringToString(regionRoot);
        if (!regionName)
            throw new Error("Region name has to have a value.");
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        if (locale == undefined)
        {
            throw new Error("Locale name is undefined.");
        }
        const result = getLanguageName(locale, regionName);
        if (result!.length > bufferLength) // quite impossible
        {
            throw new Error(`Language name exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result!.length, result!);
        wrap_no_error_root(isException, exceptionRoot);
        return result!.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        regionRoot.release();
        exceptionRoot.release();
    }
}

const getLanguageName = (locale: string, regionName: string) => new Intl.DisplayNames([locale], {type: "language"}).of(regionName);

export function mono_wasm_get_display_name(culture: MonoStringRef, displayCulture: MonoStringRef, buffer: number, bufferLength: number, isException: Int32Ptr, exAddress: MonoObjectRef): number {
    const cultureRoot = mono_wasm_new_external_root<MonoString>(culture),
        displayCultureRoot = mono_wasm_new_external_root<MonoString>(displayCulture),
        exceptionRoot = mono_wasm_new_external_root<MonoObject>(exAddress);
    try {
        const displayCultureName = monoStringToString(displayCultureRoot);
        if (!displayCultureName)
            throw new Error("Display culture name has to have a value.");
        const cultureName = monoStringToString(cultureRoot);
        const locale = cultureName ? cultureName : undefined;
        if (locale == undefined)
        {
            throw new Error("Locale name is undefined.");
        }
        const result = getLanguageName(locale, displayCultureName);
        console.log(`displayCultureName=${displayCultureName}, cultureName=${cultureName}, result=${result}`);
        if (result!.length > bufferLength) // quite impossible
        {
            throw new Error(`Display name exceeds length of ${bufferLength}.`);
        }
        stringToUTF16(buffer, buffer + 2 * result!.length, result!);
        wrap_no_error_root(isException, exceptionRoot);
        return result!.length;
    }
    catch (ex: any) {
        wrap_error_root(isException, ex, exceptionRoot);
        return -1;
    }
    finally {
        cultureRoot.release();
        displayCultureRoot.release();
        exceptionRoot.release();
    }
}
