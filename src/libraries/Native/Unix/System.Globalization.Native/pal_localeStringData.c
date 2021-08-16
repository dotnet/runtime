// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>
#include <string.h>

#include "pal_locale_internal.h"
#include "pal_localeStringData.h"

/*
Function:
GetLocaleInfoDecimalFormatSymbol

Obtains the value of a DecimalFormatSymbols
*/
static UErrorCode GetLocaleInfoDecimalFormatSymbol(const char* locale,
                                                   UNumberFormatSymbol symbol,
                                                   UChar* value,
                                                   int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    UNumberFormat* pFormat = unum_open(UNUM_DECIMAL, NULL, 0, locale, NULL, &status);
    unum_getSymbol(pFormat, symbol, value, valueLength, &status);
    unum_close(pFormat);
    return status;
}

/*
Function:
GetDigitSymbol

Obtains the value of a Digit DecimalFormatSymbols
*/
static UErrorCode GetDigitSymbol(const char* locale,
                                 UErrorCode previousStatus,
                                 UNumberFormatSymbol symbol,
                                 int digit,
                                 UChar* value,
                                 int32_t valueLength)
{
    if (U_FAILURE(previousStatus))
    {
        return previousStatus;
    }

    return GetLocaleInfoDecimalFormatSymbol(locale, symbol, value + digit, valueLength - digit);
}

/*
Function:
GetLocaleInfoAmPm

Obtains the value of the AM or PM string for a locale.
*/
static UErrorCode GetLocaleInfoAmPm(const char* locale,
                                    int am,
                                    UChar* value,
                                    int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    UDateFormat* pFormat = udat_open(UDAT_DEFAULT, UDAT_DEFAULT, locale, NULL, 0, NULL, 0, &status);
    udat_getSymbols(pFormat, UDAT_AM_PMS, am ? 0 : 1, value, valueLength, &status);
    udat_close(pFormat);
    return status;
}

/*
Function:
GetLocaleIso639LanguageTwoLetterName

Gets the language name for a locale (via uloc_getLanguage) and converts the result to UChars
*/
static UErrorCode GetLocaleIso639LanguageTwoLetterName(const char* locale, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR, ignore = U_ZERO_ERROR;
    int32_t length = uloc_getLanguage(locale, NULL, 0, &ignore) + 1;

    char* buf = (char*)calloc((size_t)length, sizeof(char));
    if (buf == NULL)
    {
        return U_MEMORY_ALLOCATION_ERROR;
    }

    uloc_getLanguage(locale, buf, length, &status);
    u_charsToUChars_safe(buf, value, valueLength, &status);
    free(buf);

    return status;
}

/*
Function:
GetLocaleIso639LanguageThreeLetterName

Gets the language name for a locale (via uloc_getISO3Language) and converts the result to UChars
*/
static UErrorCode GetLocaleIso639LanguageThreeLetterName(const char* locale, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    const char *isoLanguage = uloc_getISO3Language(locale);
    if (isoLanguage[0] == 0)
    {
        return U_ILLEGAL_ARGUMENT_ERROR;
    }

    u_charsToUChars_safe(isoLanguage, value, valueLength, &status);
    return status;
}

/*
Function:
GetLocaleIso3166CountryName

Gets the country name for a locale (via uloc_getCountry) and converts the result to UChars
*/
static UErrorCode GetLocaleIso3166CountryName(const char* locale, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR, ignore = U_ZERO_ERROR;
    int32_t length = uloc_getCountry(locale, NULL, 0, &ignore) + 1;

    char* buf = (char*)calloc((size_t)length, sizeof(char));
    if (buf == NULL)
    {
        return U_MEMORY_ALLOCATION_ERROR;
    }

    uloc_getCountry(locale, buf, length, &status);
    u_charsToUChars_safe(buf, value, valueLength, &status);
    free(buf);

    return status;
}

/*
Function:
GetLocaleIso3166CountryCode

Gets the 3 letter country code for a locale (via uloc_getISO3Country) and converts the result to UChars
*/
static UErrorCode GetLocaleIso3166CountryCode(const char* locale, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    const char *pIsoCountryName = uloc_getISO3Country(locale);
    size_t len = strlen(pIsoCountryName);

    if (len == 0)
    {
        return U_ILLEGAL_ARGUMENT_ERROR;
    }

    u_charsToUChars_safe(pIsoCountryName, value, valueLength, &status);
    return status;
}

/*
Function:
GetLocaleCurrencyName

Gets the locale currency English or native name and convert the result to UChars
*/
static UErrorCode GetLocaleCurrencyName(const char* locale, UBool nativeName, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;

    UChar currencyThreeLettersName[4]; // 3 letters currency iso name + NULL
    ucurr_forLocale(locale, currencyThreeLettersName, 4, &status);
    if (!U_SUCCESS(status))
    {
        return status;
    }

    int32_t len;
    UBool formatChoice;
    const UChar *pCurrencyLongName = ucurr_getName(
                                        currencyThreeLettersName,
                                        nativeName ? locale : ULOC_US,
                                        UCURR_LONG_NAME,
                                        &formatChoice,
                                        &len,
                                        &status);
    if (!U_SUCCESS(status))
    {
        return status;
    }

    if (len >= valueLength) // we need to have room for NULL too
    {
        return U_BUFFER_OVERFLOW_ERROR;
    }

    u_strncpy(value, pCurrencyLongName, len);
    value[len] = 0;

    return status;
}

/*
PAL Function:
GetLocaleInfoString

Obtains string locale information.
Returns 1 for success, 0 otherwise
*/
int32_t GlobalizationNative_GetLocaleInfoString(const UChar* localeName,
                                                LocaleStringData localeStringData,
                                                UChar* value,
                                                int32_t valueLength,
                                                const UChar* uiLocaleName)
{
    UErrorCode status = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY] = "";
    char uiLocale[ULOC_FULLNAME_CAPACITY] = "";

    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &status);

    if (U_FAILURE(status))
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    switch (localeStringData)
    {
        case LocaleString_LocalizedDisplayName:
            assert(uiLocaleName != NULL);
            GetLocale(uiLocaleName, uiLocale, ULOC_FULLNAME_CAPACITY, false, &status);
            uloc_getDisplayName(locale, uiLocale, value, valueLength, &status);
            if (status == U_USING_DEFAULT_WARNING)
            {
                // The default locale data was used. i.e. couldn't find suitable resources for the requested UI language. Fallback to English then.
                uloc_getDisplayName(locale, ULOC_ENGLISH, value, valueLength, &status);
            }
            break;
        case LocaleString_EnglishDisplayName:
            uloc_getDisplayName(locale, ULOC_ENGLISH, value, valueLength, &status);
            break;
        case LocaleString_NativeDisplayName:
            uloc_getDisplayName(locale, locale, value, valueLength, &status);
            if (status == U_USING_DEFAULT_WARNING)
            {
                // The default locale data was used. i.e. couldn't find suitable resources for the requested input locale. Fallback to English instead.
                uloc_getDisplayName(locale, ULOC_ENGLISH, value, valueLength, &status);
            }

            break;
        case LocaleString_LocalizedLanguageName:
            assert(uiLocaleName != NULL);
            GetLocale(uiLocaleName, uiLocale, ULOC_FULLNAME_CAPACITY, false, &status);
            uloc_getDisplayLanguage(locale, uiLocale, value, valueLength, &status);
            if (status == U_USING_DEFAULT_WARNING)
            {
                // No data was found from the locale resources and a case canonicalized language code is placed into language as fallback. Fallback to English instead.
                uloc_getDisplayLanguage(locale, ULOC_ENGLISH, value, valueLength, &status);
            }

            break;
        case LocaleString_EnglishLanguageName:
            uloc_getDisplayLanguage(locale, ULOC_ENGLISH, value, valueLength, &status);
            break;
        case LocaleString_NativeLanguageName:
            uloc_getDisplayLanguage(locale, locale, value, valueLength, &status);
            if (status == U_USING_DEFAULT_WARNING)
            {
                // No data was found from the locale resources and a case canonicalized language code is placed into language as fallback. Fallback to English instead.
                uloc_getDisplayLanguage(locale, ULOC_ENGLISH, value, valueLength, &status);
            }
            break;
        case LocaleString_EnglishCountryName:
            uloc_getDisplayCountry(locale, ULOC_ENGLISH, value, valueLength, &status);
            break;
        case LocaleString_NativeCountryName:
            uloc_getDisplayCountry(locale, locale, value, valueLength, &status);
            if (status == U_USING_DEFAULT_WARNING)
            {
                // No data was found from the locale resources and a case canonicalized language code is placed into language as fallback. Fallback to English instead.
                uloc_getDisplayCountry(locale, ULOC_ENGLISH, value, valueLength, &status);
            }
            break;
        case LocaleString_ThousandSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_GROUPING_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case LocaleString_DecimalSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_DECIMAL_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case LocaleString_Digits:
            status = GetDigitSymbol(locale, status, UNUM_ZERO_DIGIT_SYMBOL, 0, value, valueLength);
            // symbols UNUM_ONE_DIGIT to UNUM_NINE_DIGIT are contiguous
            for (int32_t symbol = UNUM_ONE_DIGIT_SYMBOL; symbol <= UNUM_NINE_DIGIT_SYMBOL; symbol++)
            {
                int charIndex = symbol - UNUM_ONE_DIGIT_SYMBOL + 1;
                status = GetDigitSymbol(
                    locale, status, (UNumberFormatSymbol)symbol, charIndex, value, valueLength);
            }
            break;
        case LocaleString_MonetarySymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_CURRENCY_SYMBOL, value, valueLength);
            break;
        case LocaleString_Iso4217MonetarySymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_INTL_CURRENCY_SYMBOL, value, valueLength);
            break;
        case LocaleString_CurrencyEnglishName:
            status = GetLocaleCurrencyName(locale, false, value, valueLength);
            break;
        case LocaleString_CurrencyNativeName:
            status = GetLocaleCurrencyName(locale, true, value, valueLength);
            break;
        case LocaleString_MonetaryDecimalSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case LocaleString_MonetaryThousandSeparator:
            status =
                GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_GROUPING_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case LocaleString_AMDesignator:
            status = GetLocaleInfoAmPm(locale, true, value, valueLength);
            break;
        case LocaleString_PMDesignator:
            status = GetLocaleInfoAmPm(locale, false, value, valueLength);
            break;
        case LocaleString_PositiveSign:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PLUS_SIGN_SYMBOL, value, valueLength);
            break;
        case LocaleString_NegativeSign:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MINUS_SIGN_SYMBOL, value, valueLength);
            break;
        case LocaleString_Iso639LanguageTwoLetterName:
            status = GetLocaleIso639LanguageTwoLetterName(locale, value, valueLength);
            break;
        case LocaleString_Iso639LanguageThreeLetterName:
            status = GetLocaleIso639LanguageThreeLetterName(locale, value, valueLength);
            break;
        case LocaleString_Iso3166CountryName:
            status = GetLocaleIso3166CountryName(locale, value, valueLength);
            break;
        case LocaleString_Iso3166CountryName2:
            status = GetLocaleIso3166CountryCode(locale, value, valueLength);
            break;
        case LocaleString_NaNSymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_NAN_SYMBOL, value, valueLength);
            break;
        case LocaleString_PositiveInfinitySymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_INFINITY_SYMBOL, value, valueLength);
            break;
        case LocaleString_ParentName:
        {
            // ICU supports lang[-script][-region][-variant] so up to 4 parents
            // including invariant locale
            char localeNameTemp[ULOC_FULLNAME_CAPACITY];

            uloc_getParent(locale, localeNameTemp, ULOC_FULLNAME_CAPACITY, &status);
            u_charsToUChars_safe(localeNameTemp, value, valueLength, &status);
            if (U_SUCCESS(status))
            {
                FixupLocaleName(value, valueLength);
            }
            break;
        }
        case LocaleString_PercentSymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PERCENT_SYMBOL, value, valueLength);
            break;
        case LocaleString_PerMilleSymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PERMILL_SYMBOL, value, valueLength);
            break;
        default:
            status = U_UNSUPPORTED_ERROR;
            break;
    }

    return UErrorCodeToBool(status);
}

/*
PAL Function:
GetLocaleTimeFormat

Obtains time format information (in ICU format, it needs to be coverted to .NET Format).
Returns 1 for success, 0 otherwise
*/
int32_t GlobalizationNative_GetLocaleTimeFormat(const UChar* localeName,
                                                int shortFormat,
                                                UChar* value,
                                                int32_t valueLength)
{
    UErrorCode err = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &err);
    UDateFormatStyle style = (shortFormat != 0) ? UDAT_SHORT : UDAT_MEDIUM;
    UDateFormat* pFormat = udat_open(style, UDAT_NONE, locale, NULL, 0, NULL, 0, &err);
    udat_toPattern(pFormat, false, value, valueLength, &err);
    udat_close(pFormat);
    return UErrorCodeToBool(err);
}
