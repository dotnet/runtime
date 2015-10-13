//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <assert.h>
#include <string.h>

#include "locale.hpp"

#include "unicode/dcfmtsym.h" //decimal symbols
#include "unicode/dtfmtsym.h" //date symbols
#include "unicode/smpdtfmt.h" //date format
#include "unicode/localpointer.h"

// invariant character definitions used by ICU
#define UCHAR_SPACE ((UChar)0x0020)   // space
#define UCHAR_NBSPACE ((UChar)0x00A0) // space

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
UErrorCode GetLocaleInfoDecimalFormatSymbol(const Locale& locale,
                                            DecimalFormatSymbols::ENumberFormatSymbol symbol,
                                            UChar* value,
                                            int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    LocalPointer<DecimalFormatSymbols> decimalsymbols(new DecimalFormatSymbols(locale, status));
    if (decimalsymbols == NULL)
    {
        status = U_MEMORY_ALLOCATION_ERROR;
    }

    if (U_FAILURE(status))
    {
        return status;
    }

    UnicodeString s = decimalsymbols->getSymbol(symbol);

    s.extract(value, valueLength, status);
    return status;
}

/*
Function:
GetDigitSymbol

Obtains the value of a Digit DecimalFormatSymbols
*/
UErrorCode GetDigitSymbol(const Locale& locale,
                          UErrorCode previousStatus,
                          DecimalFormatSymbols::ENumberFormatSymbol symbol,
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

Obtains the value of a DateFormatSymbols Am or Pm string
*/
UErrorCode GetLocaleInfoAmPm(const Locale& locale, bool am, UChar* value, int32_t valueLength)
{
    UErrorCode status = U_ZERO_ERROR;
    LocalPointer<DateFormatSymbols> dateFormatSymbols(new DateFormatSymbols(locale, status));
    if (dateFormatSymbols == NULL)
    {
        status = U_MEMORY_ALLOCATION_ERROR;
    }

    if (U_FAILURE(status))
    {
        return status;
    }

    int32_t count = 0;
    const UnicodeString* tempStr = dateFormatSymbols->getAmPmStrings(count);
    int offset = am ? 0 : 1;
    if (offset >= count)
    {
        return U_INTERNAL_PROGRAM_ERROR;
    }

    tempStr[offset].extract(value, valueLength, status);
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
            status = GetLocaleInfoDecimalFormatSymbol(
                locale, DecimalFormatSymbols::kGroupingSeparatorSymbol, value, valueLength);
            break;
        case DecimalSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(
                locale, DecimalFormatSymbols::kDecimalSeparatorSymbol, value, valueLength);
            break;
        case Digits:
            status = GetDigitSymbol(locale, status, DecimalFormatSymbols::kZeroDigitSymbol, 0, value, valueLength);
            // symbols kOneDigitSymbol to kNineDigitSymbol are contiguous
            for (int32_t symbol = DecimalFormatSymbols::kOneDigitSymbol;
                 symbol <= DecimalFormatSymbols::kNineDigitSymbol;
                 symbol++)
            {
                int charIndex = symbol - DecimalFormatSymbols::kOneDigitSymbol + 1;
                status = GetDigitSymbol(
                    locale, status, (DecimalFormatSymbols::ENumberFormatSymbol)symbol, charIndex, value, valueLength);
            }
            break;
        case MonetarySymbol:
            status =
                GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kCurrencySymbol, value, valueLength);
            break;
        case Iso4217MonetarySymbol:
            status =
                GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kIntlCurrencySymbol, value, valueLength);
            break;
        case MonetaryDecimalSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(
                locale, DecimalFormatSymbols::kMonetarySeparatorSymbol, value, valueLength);
            break;
        case MonetaryThousandSeparator:
            status = GetLocaleInfoDecimalFormatSymbol(
                locale, DecimalFormatSymbols::kMonetaryGroupingSeparatorSymbol, value, valueLength);
            break;
        case AMDesignator:
            status = GetLocaleInfoAmPm(locale, true, value, valueLength);
            break;
        case PMDesignator:
            status = GetLocaleInfoAmPm(locale, false, value, valueLength);
            break;
        case PositiveSign:
            status =
                GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kPlusSignSymbol, value, valueLength);
            break;
        case NegativeSign:
            status =
                GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kMinusSignSymbol, value, valueLength);
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
            status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kNaNSymbol, value, valueLength);
            break;
        case PositiveInfinitySymbol:
            status =
                GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kInfinitySymbol, value, valueLength);
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
            status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kPercentSymbol, value, valueLength);
            break;
        case PerMilleSymbol:
            status = GetLocaleInfoDecimalFormatSymbol(locale, DecimalFormatSymbols::kPerMillSymbol, value, valueLength);
            break;
        default:
            status = U_UNSUPPORTED_ERROR;
            break;
    };

    return UErrorCodeToBool(status);
}

/*
Function:
NormalizeTimePattern

Convert an ICU non-localized time pattern to .NET format
*/
void NormalizeTimePattern(const UnicodeString* srcPattern, UnicodeString* destPattern)
{
    // An srcPattern example: "h:mm:ss a"
    // A destPattern example: "h:mm:ss tt"
    destPattern->remove();

    bool amPmAdded = false;
    for (int i = 0; i <= srcPattern->length() - 1; i++)
    {
        UChar ch = srcPattern->charAt(i);
        switch (ch)
        {
            case ':':
            case '.':
            case 'H':
            case 'h':
            case 'm':
            case 's':
                destPattern->append(ch);
                break;

            case UCHAR_SPACE:
            case UCHAR_NBSPACE:
                destPattern->append(UCHAR_SPACE);
                break;

            case 'a': // AM/PM
                if (!amPmAdded)
                {
                    amPmAdded = true;
                    destPattern->append("tt");
                }
                break;
        }
    }
}

/*
PAL Function:
GetLocaleTimeFormat

Obtains time format information.
Returns 1 for success, 0 otherwise
*/
extern "C" int32_t GetLocaleTimeFormat(const UChar* localeName, int shortFormat, UChar* value, int32_t valueLength)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    DateFormat::EStyle style = (shortFormat != 0) ? DateFormat::kShort : DateFormat::kMedium;
    LocalPointer<DateFormat> dateFormat(DateFormat::createTimeInstance(style, locale));
    if (dateFormat == NULL || !dateFormat.isValid())
    {
        return UErrorCodeToBool(U_MEMORY_ALLOCATION_ERROR);
    }

    // cast to SimpleDateFormat so we can call toPattern()
    SimpleDateFormat* sdf = dynamic_cast<SimpleDateFormat*>(dateFormat.getAlias());
    if (sdf == NULL)
    {
        return UErrorCodeToBool(U_INTERNAL_PROGRAM_ERROR);
    }

    UnicodeString icuPattern;
    sdf->toPattern(icuPattern);

    UnicodeString dotnetPattern;
    NormalizeTimePattern(&icuPattern, &dotnetPattern);

    UErrorCode status = U_ZERO_ERROR;
    dotnetPattern.extract(value, valueLength, status);

    return UErrorCodeToBool(status);
}
