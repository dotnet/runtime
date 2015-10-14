//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <assert.h>
#include <string.h>

#include "locale.hpp"

#include "unicode/calendar.h"
#include "unicode/decimfmt.h"
#include "unicode/localpointer.h"
#include "unicode/numfmt.h"
#include "unicode/ulocdata.h"

// invariant character definitions used by ICU
#define UCHAR_CURRENCY ((UChar)0x00A4)   // international currency
#define UCHAR_SPACE ((UChar)0x0020)      // space
#define UCHAR_NBSPACE ((UChar)0x00A0)    // space
#define UCHAR_DIGIT ((UChar)0x0023)      // '#'
#define UCHAR_SEMICOLON ((UChar)0x003B)  // ';'
#define UCHAR_MINUS ((UChar)0x002D)      // '-'
#define UCHAR_PERCENT ((UChar)0x0025)    // '%'
#define UCHAR_OPENPAREN ((UChar)0x0028)  // '('
#define UCHAR_CLOSEPAREN ((UChar)0x0029) // ')'

#define ARRAY_LENGTH(array) (sizeof(array) / sizeof(array[0]))

// Enum that corresponds to managed enum CultureData.LocaleNumberData.
// The numeric values of the enum members match their Win32 counterparts.
enum LocaleNumberData : int32_t
{
    LanguageId = 0x00000001,
    MeasurementSystem = 0x0000000D,
    FractionalDigitsCount = 0x00000011,
    NegativeNumberFormat = 0x00001010,
    MonetaryFractionalDigitsCount = 0x00000019,
    PositiveMonetaryNumberFormat = 0x0000001B,
    NegativeMonetaryNumberFormat = 0x0000001C,
    FirstDayofWeek = 0x0000100C,
    FirstWeekOfYear = 0x0000100D,
    ReadingLayout = 0x00000070,
    NegativePercentFormat = 0x00000074,
    PositivePercentFormat = 0x00000075,
    Digit = 0x00000010,
    Monetary = 0x00000018
};

// Enum that corresponds to managed enum System.Globalization.CalendarWeekRule
enum CalendarWeekRule : int32_t
{
    FirstDay = 0,
    FirstFullWeek = 1,
    FirstFourDayWeek = 2
};

/*
Function:
NormalizeNumericPattern

Returns a numeric string pattern in a format that we can match against the
appropriate managed pattern.
*/
void NormalizeNumericPattern(const UnicodeString* srcPattern, UnicodeString* destPattern, bool isNegative)
{
    // A srcPattern example: "#,##0.00 C;(#,##0.00 C)" but where C is the
    // international currency symbol (UCHAR_CURRENCY)
    // The positive pattern comes first, then an optional negative pattern
    // separated by a semicolon
    // A destPattern example: "(C n)" where C represents the currency symbol, and
    // n is the number
    destPattern->remove();

    int iStart = 0;
    int iEnd = srcPattern->length() - 1;
    int32_t iNegativePatternStart = srcPattern->indexOf(UCHAR_SEMICOLON);
    if (iNegativePatternStart >= 0)
    {
        if (isNegative)
        {
            iStart = iNegativePatternStart + 1;
        }
        else
        {
            iEnd = iNegativePatternStart - 1;
        }
    }

    bool minusAdded = false;
    bool digitAdded = false;
    bool currencyAdded = false;
    bool spaceAdded = false;

    for (int i = iStart; i <= iEnd; i++)
    {
        UChar ch = srcPattern->charAt(i);
        switch (ch)
        {
            case UCHAR_DIGIT:
                if (!digitAdded)
                {
                    digitAdded = true;
                    destPattern->append('n');
                }
                break;

            case UCHAR_CURRENCY:
                if (!currencyAdded)
                {
                    currencyAdded = true;
                    destPattern->append('C');
                }
                break;

            case UCHAR_SPACE:
            case UCHAR_NBSPACE:
                if (!spaceAdded)
                {
                    spaceAdded = true;
                    destPattern->append(UCHAR_SPACE);
                }
                else
                {
                    assert(false);
                }
                break;

            case UCHAR_MINUS:
            case UCHAR_OPENPAREN:
            case UCHAR_CLOSEPAREN:
                minusAdded = true;
                destPattern->append(ch);
                break;

            case UCHAR_PERCENT:
                destPattern->append(ch);
                break;
        }
    }

    // if there is no negative subpattern, the ICU convention is to prefix the
    // minus sign
    if (isNegative && !minusAdded)
    {
        destPattern->insert(0, UCHAR_MINUS);
    }
}

/*
Function:
GetNumericPattern

Determines the pattern from the decimalFormat and returns the matching pattern's
index from patterns[].
Returns index -1 if no pattern is found.
*/
int GetNumericPattern(DecimalFormat* decimalFormat, const char* patterns[], int patternsCount, bool isNegative)
{
    const int INVALID_FORMAT = -1;
    const int MAX_DOTNET_NUMERIC_PATTERN_LENGTH = 6; // example: "(C n)" plus terminator
    char charPattern[MAX_DOTNET_NUMERIC_PATTERN_LENGTH] = {0};

    UnicodeString icuPattern;
    decimalFormat->toPattern(icuPattern);

    UnicodeString normalizedPattern;
    NormalizeNumericPattern(&icuPattern, &normalizedPattern, isNegative);

    assert(normalizedPattern.length() > 0);
    assert(normalizedPattern.length() < MAX_DOTNET_NUMERIC_PATTERN_LENGTH);
    if (normalizedPattern.length() == 0 || normalizedPattern.length() >= MAX_DOTNET_NUMERIC_PATTERN_LENGTH)
    {
        return INVALID_FORMAT;
    }

    u_UCharsToChars(normalizedPattern.getTerminatedBuffer(), charPattern, normalizedPattern.length() + 1);

    for (int i = 0; i < patternsCount; i++)
    {
        if (strcmp(charPattern, patterns[i]) == 0)
        {
            return i;
        }
    };

    assert(false); // should have found a valid pattern
    return INVALID_FORMAT;
}

/*
Function:
GetCurrencyNegativePattern

Implementation of NumberFormatInfo.CurrencyNegativePattern.
Returns the pattern index.
*/
int GetCurrencyNegativePattern(const Locale& locale)
{
    const int DEFAULT_VALUE = 0;
    static const char* Patterns[] = {"(Cn)",
                                     "-Cn",
                                     "C-n",
                                     "Cn-",
                                     "(nC)",
                                     "-nC",
                                     "n-C",
                                     "nC-",
                                     "-n C",
                                     "-C n",
                                     "n C-",
                                     "C n-",
                                     "C -n",
                                     "n- C",
                                     "(C n)",
                                     "(n C)"};
    UErrorCode status = U_ZERO_ERROR;

    LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_CURRENCY, status));
    assert(U_SUCCESS(status));
    if (U_SUCCESS(status))
    {
        DecimalFormat* decimalFormat = dynamic_cast<DecimalFormat*>(format.getAlias());
        assert(decimalFormat != NULL);
        if (decimalFormat != NULL)
        {
            int value = GetNumericPattern(decimalFormat, Patterns, ARRAY_LENGTH(Patterns), true);
            if (value >= 0)
            {
                return value;
            }
        }
    }

    return DEFAULT_VALUE;
}

/*
Function:
GetCurrencyPositivePattern

Implementation of NumberFormatInfo.CurrencyPositivePattern.
Returns the pattern index.
*/
int GetCurrencyPositivePattern(const Locale& locale)
{
    const int DEFAULT_VALUE = 0;
    static const char* Patterns[] = {"Cn", "nC", "C n", "n C"};
    UErrorCode status = U_ZERO_ERROR;

    LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_CURRENCY, status));
    assert(U_SUCCESS(status));
    if (U_SUCCESS(status))
    {
        DecimalFormat* decimalFormat = dynamic_cast<DecimalFormat*>(format.getAlias());
        assert(decimalFormat != NULL);
        if (decimalFormat != NULL)
        {
            int value = GetNumericPattern(decimalFormat, Patterns, ARRAY_LENGTH(Patterns), false);
            if (value >= 0)
            {
                return value;
            }
        }
    }

    return DEFAULT_VALUE;
}

/*
Function:
GetNumberNegativePattern

Implementation of NumberFormatInfo.NumberNegativePattern.
Returns the pattern index.
*/
int GetNumberNegativePattern(const Locale& locale)
{
    const int DEFAULT_VALUE = 1;
    static const char* Patterns[] = {"(n)", "-n", "- n", "n-", "n -"};
    UErrorCode status = U_ZERO_ERROR;

    LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_DECIMAL, status));
    assert(U_SUCCESS(status));
    if (U_SUCCESS(status))
    {
        DecimalFormat* decimalFormat = dynamic_cast<DecimalFormat*>(format.getAlias());
        assert(decimalFormat != NULL);
        if (decimalFormat != NULL)
        {
            int value = GetNumericPattern(decimalFormat, Patterns, ARRAY_LENGTH(Patterns), true);
            if (value >= 0)
            {
                return value;
            }
        }
    }

    return DEFAULT_VALUE;
}

/*
Function:
GetPercentNegativePattern

Implementation of NumberFormatInfo.PercentNegativePattern.
Returns the pattern index.
*/
int GetPercentNegativePattern(const Locale& locale)
{
    const int DEFAULT_VALUE = 0;
    static const char* Patterns[] = {
        "-n %", "-n%", "-%n", "%-n", "%n-", "n-%", "n%-", "-% n", "n %-", "% n-", "% -n", "n- %"};
    UErrorCode status = U_ZERO_ERROR;

    LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_PERCENT, status));
    assert(U_SUCCESS(status));
    if (U_SUCCESS(status))
    {
        DecimalFormat* decimalFormat = dynamic_cast<DecimalFormat*>(format.getAlias());
        assert(decimalFormat != NULL);
        if (decimalFormat != NULL)
        {
            int value = GetNumericPattern(decimalFormat, Patterns, ARRAY_LENGTH(Patterns), true);
            if (value >= 0)
            {
                return value;
            }
        }
    }

    return DEFAULT_VALUE;
}

/*
Function:
GetPercentPositivePattern

Implementation of NumberFormatInfo.PercentPositivePattern.
Returns the pattern index.
*/
int GetPercentPositivePattern(const Locale& locale)
{
    const int DEFAULT_VALUE = 0;
    static const char* Patterns[] = {"n %", "n%", "%n", "% n"};
    UErrorCode status = U_ZERO_ERROR;

    LocalPointer<NumberFormat> format(NumberFormat::createInstance(locale, UNUM_PERCENT, status));
    assert(U_SUCCESS(status));
    if (U_SUCCESS(status))
    {
        DecimalFormat* decimalFormat = dynamic_cast<DecimalFormat*>(format.getAlias());
        assert(decimalFormat != NULL);
        if (decimalFormat != NULL)
        {
            int value = GetNumericPattern(decimalFormat, Patterns, ARRAY_LENGTH(Patterns), false);
            if (value >= 0)
            {
                return value;
            }
        }
    }

    return DEFAULT_VALUE;
}

/*
Function:
GetMeasurementSystem

Obtains the measurement system for the local, determining if US or metric.
Returns 1 for US, 0 otherwise.
*/
UErrorCode GetMeasurementSystem(const char* localeId, int32_t* value)
{
    UErrorCode status = U_ZERO_ERROR;

    UMeasurementSystem measurementSystem = ulocdata_getMeasurementSystem(localeId, &status);
    if (U_SUCCESS(status))
    {
        *value = (measurementSystem == UMeasurementSystem::UMS_US) ? 1 : 0;
    }

    return status;
}

/*
PAL Function:
GetLocaleInfoInt

Obtains integer locale information
Returns 1 for success, 0 otherwise
*/
extern "C" int32_t GetLocaleInfoInt(const UChar* localeName, LocaleNumberData localeNumberData, int32_t* value)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    UErrorCode status = U_ZERO_ERROR;

    switch (localeNumberData)
    {
        case LanguageId:
            *value = locale.getLCID();
            break;
        case MeasurementSystem:
            status = GetMeasurementSystem(locale.getName(), value);
            break;
        case FractionalDigitsCount:
        {
            UNumberFormat* numformat = unum_open(UNUM_DECIMAL, NULL, 0, locale.getName(), NULL, &status);
            if (U_SUCCESS(status))
            {
                *value = unum_getAttribute(numformat, UNUM_MAX_FRACTION_DIGITS);
                unum_close(numformat);
            }
            break;
        }
        case NegativeNumberFormat:
            *value = GetNumberNegativePattern(locale);
            break;
        case MonetaryFractionalDigitsCount:
        {
            UNumberFormat* numformat = unum_open(UNUM_CURRENCY, NULL, 0, locale.getName(), NULL, &status);
            if (U_SUCCESS(status))
            {
                *value = unum_getAttribute(numformat, UNUM_MAX_FRACTION_DIGITS);
                unum_close(numformat);
            }
            break;
        }
        case PositiveMonetaryNumberFormat:
            *value = GetCurrencyPositivePattern(locale);
            break;
        case NegativeMonetaryNumberFormat:
            *value = GetCurrencyNegativePattern(locale);
            break;
        case FirstWeekOfYear:
        {
            // corresponds to DateTimeFormat.CalendarWeekRule
            LocalPointer<Calendar> calendar(Calendar::createInstance(locale, status));
            if (U_SUCCESS(status))
            {
                // values correspond to LOCALE_IFIRSTWEEKOFYEAR
                int minDaysInWeek = calendar->getMinimalDaysInFirstWeek();
                if (minDaysInWeek == 1)
                {
                    *value = CalendarWeekRule::FirstDay;
                }
                else if (minDaysInWeek == 7)
                {
                    *value = CalendarWeekRule::FirstFullWeek;
                }
                else if (minDaysInWeek >= 4)
                {
                    *value = CalendarWeekRule::FirstFourDayWeek;
                }
                else
                {
                    status = U_UNSUPPORTED_ERROR;
                }
            }
            break;
        }
        case ReadingLayout:
        {
            // coresponds to values 0 and 1 in LOCALE_IREADINGLAYOUT (values 2 and 3 not
            // used in coreclr)
            //  0 - Left to right (such as en-US)
            //  1 - Right to left (such as arabic locales)
            ULayoutType orientation = uloc_getCharacterOrientation(locale.getName(), &status);
            // alternative implementation in ICU 54+ is Locale.isRightToLeft() which
            // also supports script tags in locale
            if (U_SUCCESS(status))
            {
                *value = (orientation == ULOC_LAYOUT_RTL) ? 1 : 0;
            }
            break;
        }
        case FirstDayofWeek:
        {
            LocalPointer<Calendar> pcalendar(Calendar::createInstance(locale, status));
            if (U_SUCCESS(status))
            {
                *value = pcalendar->getFirstDayOfWeek(status) - 1; // .NET is 0-based and ICU is 1-based
            }
            break;
        }
        case NegativePercentFormat:
            *value = GetPercentNegativePattern(locale);
            break;
        case PositivePercentFormat:
            *value = GetPercentPositivePattern(locale);
            break;
        default:
            status = U_UNSUPPORTED_ERROR;
            assert(false);
            break;
    }

    return UErrorCodeToBool(status);
}

/*
PAL Function:
GetLocaleInfoGroupingSizes

Obtains grouping sizes for decimal and currency
Returns 1 for success, 0 otherwise
*/
extern "C" int32_t GetLocaleInfoGroupingSizes(const UChar* localeName,
                                              LocaleNumberData localeGroupingData,
                                              int32_t* primaryGroupSize,
                                              int32_t* secondaryGroupSize)
{
    Locale locale = GetLocale(localeName);
    if (locale.isBogus())
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    UNumberFormatStyle style;
    switch (localeGroupingData)
    {
        case Digit:
            style = UNUM_DECIMAL;
            break;
        case Monetary:
            style = UNUM_CURRENCY;
            break;
        default:
            return UErrorCodeToBool(U_UNSUPPORTED_ERROR);
    }

    UErrorCode status = U_ZERO_ERROR;
    UNumberFormat* numformat = unum_open(style, NULL, 0, locale.getName(), NULL, &status);
    if (U_SUCCESS(status))
    {
        *primaryGroupSize = unum_getAttribute(numformat, UNUM_GROUPING_SIZE);
        *secondaryGroupSize = unum_getAttribute(numformat, UNUM_SECONDARY_GROUPING_SIZE);
        unum_close(numformat);
    }

    return UErrorCodeToBool(status);
}
