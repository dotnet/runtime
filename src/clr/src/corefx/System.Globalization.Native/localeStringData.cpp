//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <assert.h>
#include <string.h>

#include "locale.hpp"
#include "holders.h"

#include "unicode/dcfmtsym.h" //decimal symbols
#include "unicode/dtfmtsym.h" //date symbols
#include "unicode/smpdtfmt.h" //date format
#include "unicode/localpointer.h"

// Enum that corresponds to managed enum CultureData.LocaleStringData.
// The numeric values of the enum members match their Win32 counterparts.
enum LocaleStringData : int32_t
{
    LocalizedDisplayName = 0x00000002,
    EnglishDisplayName = 0x00000072,
    NativeDisplayName = 0x00000073,
    LocalizedLanguageName = 0x0000006f,
    EnglishLanguageName = 0x00001001,
    NativeLanguageName = 0x00000004,
    EnglishCountryName = 0x00001002,
    NativeCountryName = 0x00000008,
    ListSeparator = 0x0000000C,
    DecimalSeparator = 0x0000000E,
    ThousandSeparator = 0x0000000F,
    Digits = 0x00000013,
    MonetarySymbol = 0x00000014,
    Iso4217MonetarySymbol = 0x00000015,
    MonetaryDecimalSeparator = 0x00000016,
    MonetaryThousandSeparator = 0x00000017,
    AMDesignator = 0x00000028,
    PMDesignator = 0x00000029,
    PositiveSign = 0x00000050,
    NegativeSign = 0x00000051,
    Iso639LanguageName = 0x00000059,
    Iso3166CountryName = 0x0000005A,
    NaNSymbol = 0x00000069,
    PositiveInfinitySymbol = 0x0000006a,
    ParentName = 0x0000006d,
    PercentSymbol = 0x00000076,
    PerMilleSymbol = 0x00000077
};

/*
Function:
GetLocaleInfoDecimalFormatSymbol

Obtains the value of a DecimalFormatSymbols
*/
UErrorCode
GetLocaleInfoDecimalFormatSymbol(const Locale& locale, UNumberFormatSymbol symbol, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    UNumberFormat* pFormat = unum_open(UNUM_DECIMAL, nullptr, 0, locale.getName(), nullptr, &status);
    UNumberFormatHolder formatHolder(pFormat, status);

    if (U_FAILURE(status))
    {
        return status;
    }

    unum_getSymbol(pFormat, symbol, value, valueLength, &status);

    return status;
}

/*
Function:
GetDigitSymbol

Obtains the value of a Digit DecimalFormatSymbols
*/
UErrorCode GetDigitSymbol(const Locale& locale,
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
UErrorCode GetLocaleInfoAmPm(const Locale& locale, bool am, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    UDateFormat* pFormat = udat_open(UDAT_DEFAULT, UDAT_DEFAULT, locale.getName(), nullptr, 0, nullptr, 0, &status);
    UDateFormatHolder formatHolder(pFormat, status);

    if (U_FAILURE(status))
    {
        return status;
    }

    udat_getSymbols(pFormat, UDAT_AM_PMS, am ? 0 : 1, value, valueLength, &status);

    return status;
}

/*
PAL Function:
GetLocaleInfoString

Obtains string locale information.
Returns 1 for success, 0 otherwise
*/
extern "C" int32_t
GetLocaleInfoString(const UChar* localeName, LocaleStringData localeStringData, UChar* value, int32_t valueLength)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    UnicodeString str;
    UErrorCode status = U_ZERO_ERROR;
    switch (localeStringData)
    {
        case LocalizedDisplayName:
            locale.getDisplayName(str);
            str.extract(value, valueLength, status);
            break;
        case EnglishDisplayName:
            locale.getDisplayName(Locale::getEnglish(), str);
            str.extract(value, valueLength, status);
            break;
        case NativeDisplayName:
            locale.getDisplayName(locale, str);
            str.extract(value, valueLength, status);
            break;
        case LocalizedLanguageName:
            locale.getDisplayLanguage(str);
            str.extract(value, valueLength, status);
            break;
        case EnglishLanguageName:
            locale.getDisplayLanguage(Locale::getEnglish(), str);
            str.extract(value, valueLength, status);
            break;
        case NativeLanguageName:
            locale.getDisplayLanguage(locale, str);
            str.extract(value, valueLength, status);
            break;
        case EnglishCountryName:
            locale.getDisplayCountry(Locale::getEnglish(), str);
            str.extract(value, valueLength, status);
            break;
        case NativeCountryName:
            locale.getDisplayCountry(locale, str);
            str.extract(value, valueLength, status);
            break;
        case ListSeparator:
        // fall through
        case ThousandSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_GROUPING_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case DecimalSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_DECIMAL_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case Digits:
            status = GetDigitSymbol(locale, status, UNUM_ZERO_DIGIT_SYMBOL, 0, value, valueLength);
            // symbols UNUM_ONE_DIGIT to UNUM_NINE_DIGIT are contiguous
            for (int32_t symbol = UNUM_ONE_DIGIT_SYMBOL; symbol <= UNUM_NINE_DIGIT_SYMBOL; symbol++)
            {
                int charIndex = symbol - UNUM_ONE_DIGIT_SYMBOL + 1;
                status = GetDigitSymbol(
                    locale, status, static_cast<UNumberFormatSymbol>(symbol), charIndex, value, valueLength);
            }
            break;
        case MonetarySymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_CURRENCY_SYMBOL, value, valueLength);
            break;
        case Iso4217MonetarySymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_INTL_CURRENCY_SYMBOL, value, valueLength);
            break;
        case MonetaryDecimalSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case MonetaryThousandSeparator:
            status =
                GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_GROUPING_SEPARATOR_SYMBOL, value, valueLength);
            break;
        case AMDesignator:
            status = GetLocaleInfoAmPm(locale, true, value, valueLength);
            break;
        case PMDesignator:
            status = GetLocaleInfoAmPm(locale, false, value, valueLength);
            break;
        case PositiveSign:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PLUS_SIGN_SYMBOL, value, valueLength);
            break;
        case NegativeSign:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MINUS_SIGN_SYMBOL, value, valueLength);
            break;
        case Iso639LanguageName:
            status = u_charsToUChars_safe(locale.getLanguage(), value, valueLength);
            break;
        case Iso3166CountryName:
            // coreclr expects 2-character version, not 3 (3 would correspond to
            // LOCALE_SISO3166CTRYNAME2 and locale.getISO3Country)
            status = u_charsToUChars_safe(locale.getCountry(), value, valueLength);
            break;
        case NaNSymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_NAN_SYMBOL, value, valueLength);
            break;
        case PositiveInfinitySymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_INFINITY_SYMBOL, value, valueLength);
            break;
        case ParentName:
        {
            // ICU supports lang[-script][-region][-variant] so up to 4 parents
            // including invariant locale
            char localeNameTemp[ULOC_FULLNAME_CAPACITY];

            uloc_getParent(locale.getName(), localeNameTemp, ULOC_FULLNAME_CAPACITY, &status);
            if (U_SUCCESS(status))
            {
                status = u_charsToUChars_safe(localeNameTemp, value, valueLength);
                if (U_SUCCESS(status))
                {
                    FixupLocaleName(value, valueLength);
                }
            }
            break;
        }
        case PercentSymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PERCENT_SYMBOL, value, valueLength);
            break;
        case PerMilleSymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PERMILL_SYMBOL, value, valueLength);
            break;
        default:
            status = U_UNSUPPORTED_ERROR;
            break;
    };

    return UErrorCodeToBool(status);
}

/*
PAL Function:
GetLocaleTimeFormat

Obtains time format information (in ICU format, it needs to be coverted to .NET Format).
Returns 1 for success, 0 otherwise
*/
extern "C" int32_t GetLocaleTimeFormat(const UChar* localeName, int shortFormat, UChar* value, int32_t valueLength)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    UErrorCode err = U_ZERO_ERROR;
    UDateFormatStyle style = (shortFormat != 0) ? UDAT_SHORT : UDAT_MEDIUM;
    UDateFormat* pFormat = udat_open(style, UDAT_NONE, locale.getName(), nullptr, 0, nullptr, 0, &err);
    UDateFormatHolder formatHolder(pFormat, err);

    if (U_FAILURE(err))
        return UErrorCodeToBool(err);

    udat_toPattern(pFormat, false, value, valueLength, &err);

    return UErrorCodeToBool(err);
}
