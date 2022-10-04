// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>
#include <string.h>

#include <minipal/utils.h>

#include "pal_locale_internal.h"
#include "pal_localeNumberData.h"

// invariant character definitions used by ICU
#define UCHAR_CURRENCY ((UChar)0x00A4)   // international currency
#define UCHAR_SPACE ((UChar)0x0020)      // space
#define UCHAR_NBSPACE ((UChar)0x00A0)    // space
#define UCHAR_DIGIT ((UChar)0x0023)      // '#'
#define UCHAR_MINUS ((UChar)0x002D)      // '-'
#define UCHAR_PERCENT ((UChar)0x0025)    // '%'
#define UCHAR_OPENPAREN ((UChar)0x0028)  // '('
#define UCHAR_CLOSEPAREN ((UChar)0x0029) // ')'
#define UCHAR_ZERO ((UChar)0x0030)       // '0'

/*
Function:
NormalizeNumericPattern

Returns a numeric string pattern in a format that we can match against the
appropriate managed pattern.
*/
static char* NormalizeNumericPattern(const UChar* srcPattern, int isNegative)
{
    int iStart = 0;
    int iEnd = u_strlen(srcPattern);
    int32_t iNegativePatternStart = -1;

    for (int i = iStart; i < iEnd; i++)
    {
        if (srcPattern[i] == ';')
        {
            iNegativePatternStart = i;
        }
    }

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

    int index = 0;
    int minusAdded = false;
    int digitAdded = false;
    int currencyAdded = false;
    int spaceAdded = false;

    for (int i = iStart; i <= iEnd; i++)
    {
        UChar ch = srcPattern[i];
        switch (ch)
        {
            case UCHAR_MINUS:
            case UCHAR_OPENPAREN:
            case UCHAR_CLOSEPAREN:
                minusAdded = true;
                break;
        }
    }

    // international currency symbol (UCHAR_CURRENCY)
    // The positive pattern comes first, then an optional negative pattern
    // separated by a semicolon
    // A destPattern example: "(C n)" where C represents the currency symbol, and
    // n is the number
    char* destPattern;

    // if there is no negative subpattern, the ICU convention is to prefix the
    // minus sign
    if (isNegative && !minusAdded)
    {
        int length = (iEnd - iStart) + 2;
        destPattern = (char*)calloc((size_t)length, sizeof(char));
        if (!destPattern)
        {
            return NULL;
        }
        destPattern[index++] = '-';
    }
    else
    {
        int length = (iEnd - iStart) + 1;
        destPattern = (char*)calloc((size_t)length, sizeof(char));
        if (!destPattern)
        {
            return NULL;
        }
    }

    for (int i = iStart; i <= iEnd; i++)
    {
        UChar ch = srcPattern[i];
        switch (ch)
        {
            case UCHAR_DIGIT:
            case UCHAR_ZERO:
                if (!digitAdded)
                {
                    digitAdded = true;
                    destPattern[index++] = 'n';
                }
                break;

            case UCHAR_CURRENCY:
                if (!currencyAdded)
                {
                    currencyAdded = true;
                    destPattern[index++] = 'C';
                }
                break;

            case UCHAR_SPACE:
            case UCHAR_NBSPACE:
                if (!spaceAdded)
                {
                    spaceAdded = true;
                    destPattern[index++] = ' ';
                }
                break;

            case UCHAR_MINUS:
            case UCHAR_OPENPAREN:
            case UCHAR_CLOSEPAREN:
                minusAdded = true;
                destPattern[index++] = (char)ch;
                break;

            case UCHAR_PERCENT:
                destPattern[index++] = '%';
                break;
        }
    }

    return destPattern;
}

/*
Function:
GetNumericPattern

Determines the pattern from the decimalFormat and returns the matching pattern's
index from patterns[].
Returns index -1 if no pattern is found.
*/
static int GetNumericPattern(const UNumberFormat* pNumberFormat,
                             const char* patterns[],
                             int patternsCount,
                             int isNegative)
{
    const int INVALID_FORMAT = -1;
    const int MAX_DOTNET_NUMERIC_PATTERN_LENGTH = 6; // example: "(C n)" plus terminator

    UErrorCode ignore = U_ZERO_ERROR;
    int32_t icuPatternLength = unum_toPattern(pNumberFormat, false, NULL, 0, &ignore) + 1;

    UChar* icuPattern = (UChar*)calloc((size_t)icuPatternLength, sizeof(UChar));
    if (icuPattern == NULL)
    {
        return U_MEMORY_ALLOCATION_ERROR;
    }

    UErrorCode err = U_ZERO_ERROR;

    unum_toPattern(pNumberFormat, false, icuPattern, icuPatternLength, &err);

    assert(U_SUCCESS(err));

    char* normalizedPattern = NormalizeNumericPattern(icuPattern, isNegative);

    free(icuPattern);

    if (!normalizedPattern)
    {
        return U_MEMORY_ALLOCATION_ERROR;
    }

    size_t normalizedPatternLength = strlen(normalizedPattern);

    assert(normalizedPatternLength > 0);
    assert(normalizedPatternLength < MAX_DOTNET_NUMERIC_PATTERN_LENGTH);

    if (normalizedPatternLength == 0 || normalizedPatternLength >= MAX_DOTNET_NUMERIC_PATTERN_LENGTH)
    {
        free(normalizedPattern);
        return INVALID_FORMAT;
    }

    for (int i = 0; i < patternsCount; i++)
    {
        if (strcmp(normalizedPattern, patterns[i]) == 0)
        {
            free(normalizedPattern);
            return i;
        }
    }

    assert(false); // should have found a valid pattern

    free(normalizedPattern);
    return INVALID_FORMAT;
}

/*
Function:
GetCurrencyNegativePattern

Implementation of NumberFormatInfo.CurrencyNegativePattern.
Returns the pattern index.
*/
static int GetCurrencyNegativePattern(const char* locale)
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
                                     "(n C)",
                                     "C- n" };
    UErrorCode status = U_ZERO_ERROR;

    UNumberFormat* pFormat = unum_open(UNUM_CURRENCY, NULL, 0, locale, NULL, &status);

    assert(U_SUCCESS(status));

    if (U_SUCCESS(status))
    {
        int value = GetNumericPattern(pFormat, Patterns, ARRAY_SIZE(Patterns), true);
        if (value >= 0)
        {
            unum_close(pFormat);
            return value;
        }
    }

    unum_close(pFormat);
    return DEFAULT_VALUE;
}

/*
Function:
GetCurrencyPositivePattern

Implementation of NumberFormatInfo.CurrencyPositivePattern.
Returns the pattern index.
*/
static int GetCurrencyPositivePattern(const char* locale)
{
    const int DEFAULT_VALUE = 0;
    static const char* Patterns[] = {"Cn", "nC", "C n", "n C"};
    UErrorCode status = U_ZERO_ERROR;

    UNumberFormat* pFormat = unum_open(UNUM_CURRENCY, NULL, 0, locale, NULL, &status);

    assert(U_SUCCESS(status));

    if (U_SUCCESS(status))
    {
        int value = GetNumericPattern(pFormat, Patterns, ARRAY_SIZE(Patterns), false);
        if (value >= 0)
        {
            unum_close(pFormat);
            return value;
        }
    }

    unum_close(pFormat);
    return DEFAULT_VALUE;
}

/*
Function:
GetNumberNegativePattern

Implementation of NumberFormatInfo.NumberNegativePattern.
Returns the pattern index.
*/
static int GetNumberNegativePattern(const char* locale)
{
    const int DEFAULT_VALUE = 1;
    static const char* Patterns[] = {"(n)", "-n", "- n", "n-", "n -"};
    UErrorCode status = U_ZERO_ERROR;

    UNumberFormat* pFormat = unum_open(UNUM_DECIMAL, NULL, 0, locale, NULL, &status);

    assert(U_SUCCESS(status));

    if (U_SUCCESS(status))
    {
        int value = GetNumericPattern(pFormat, Patterns, ARRAY_SIZE(Patterns), true);
        if (value >= 0)
        {
            unum_close(pFormat);
            return value;
        }
    }

    unum_close(pFormat);
    return DEFAULT_VALUE;
}

/*
Function:
GetPercentNegativePattern

Implementation of NumberFormatInfo.PercentNegativePattern.
Returns the pattern index.
*/
static int GetPercentNegativePattern(const char* locale)
{
    const int DEFAULT_VALUE = 0;
    static const char* Patterns[] = {
        "-n %", "-n%", "-%n", "%-n", "%n-", "n-%", "n%-", "-% n", "n %-", "% n-", "% -n", "n- %"};
    UErrorCode status = U_ZERO_ERROR;

    UNumberFormat* pFormat = unum_open(UNUM_PERCENT, NULL, 0, locale, NULL, &status);

    assert(U_SUCCESS(status));

    if (U_SUCCESS(status))
    {
        int value = GetNumericPattern(pFormat, Patterns, ARRAY_SIZE(Patterns), true);
        if (value >= 0)
        {
            unum_close(pFormat);
            return value;
        }
    }

    unum_close(pFormat);
    return DEFAULT_VALUE;
}

/*
Function:
GetPercentPositivePattern

Implementation of NumberFormatInfo.PercentPositivePattern.
Returns the pattern index.
*/
static int GetPercentPositivePattern(const char* locale)
{
    const int DEFAULT_VALUE = 0;
    static const char* Patterns[] = {"n %", "n%", "%n", "% n"};
    UErrorCode status = U_ZERO_ERROR;

    UNumberFormat* pFormat = unum_open(UNUM_PERCENT, NULL, 0, locale, NULL, &status);

    assert(U_SUCCESS(status));

    if (U_SUCCESS(status))
    {
        int value = GetNumericPattern(pFormat, Patterns, ARRAY_SIZE(Patterns), false);
        if (value >= 0)
        {
            unum_close(pFormat);
            return value;
        }
    }

    unum_close(pFormat);
    return DEFAULT_VALUE;
}

/*
Function:
GetMeasurementSystem

Obtains the measurement system for the local, determining if US or metric.
Returns 1 for US, 0 otherwise.
*/
static UErrorCode GetMeasurementSystem(const char* locale, int32_t* value)
{
    UErrorCode status = U_ZERO_ERROR;

    UMeasurementSystem measurementSystem = ulocdata_getMeasurementSystem(locale, &status);
    if (U_SUCCESS(status))
    {
        *value = (measurementSystem == UMS_US) ? 1 : 0;
    }

    return status;
}

/*
PAL Function:
GetLocaleInfoInt

Obtains integer locale information
Returns 1 for success, 0 otherwise
*/
int32_t GlobalizationNative_GetLocaleInfoInt(
    const UChar* localeName, LocaleNumberData localeNumberData, int32_t* value)
{
    UErrorCode status = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &status);

    if (U_FAILURE(status))
    {
        return false;
    }

    switch (localeNumberData)
    {
        case LocaleNumber_LanguageId:
            *value = (int32_t)uloc_getLCID(locale);
            break;
        case LocaleNumber_MeasurementSystem:
            status = GetMeasurementSystem(locale, value);
            break;
        case LocaleNumber_FractionalDigitsCount:
        {
            UNumberFormat* numformat = unum_open(UNUM_DECIMAL, NULL, 0, locale, NULL, &status);
            if (U_SUCCESS(status))
            {
                *value = unum_getAttribute(numformat, UNUM_MAX_FRACTION_DIGITS);
                unum_close(numformat);
            }
            break;
        }
        case LocaleNumber_NegativeNumberFormat:
            *value = GetNumberNegativePattern(locale);
            break;
        case LocaleNumber_MonetaryFractionalDigitsCount:
        {
            UNumberFormat* numformat = unum_open(UNUM_CURRENCY, NULL, 0, locale, NULL, &status);
            if (U_SUCCESS(status))
            {
                *value = unum_getAttribute(numformat, UNUM_MAX_FRACTION_DIGITS);
                unum_close(numformat);
            }
            break;
        }
        case LocaleNumber_PositiveMonetaryNumberFormat:
            *value = GetCurrencyPositivePattern(locale);
            break;
        case LocaleNumber_NegativeMonetaryNumberFormat:
            *value = GetCurrencyNegativePattern(locale);
            break;
        case LocaleNumber_FirstWeekOfYear:
        {
            // corresponds to DateTimeFormat.CalendarWeekRule
            UCalendar* pCal = ucal_open(NULL, 0, locale, UCAL_TRADITIONAL, &status);

            if (U_SUCCESS(status))
            {
                // values correspond to LOCALE_IFIRSTWEEKOFYEAR
                int minDaysInWeek = ucal_getAttribute(pCal, UCAL_MINIMAL_DAYS_IN_FIRST_WEEK);
                if (minDaysInWeek == 1)
                {
                    *value = WeekRule_FirstDay;
                }
                else if (minDaysInWeek == 7)
                {
                    *value = WeekRule_FirstFullWeek;
                }
                else if (minDaysInWeek >= 4)
                {
                    *value = WeekRule_FirstFourDayWeek;
                }
                else
                {
                    status = U_UNSUPPORTED_ERROR;
                }
            }
            ucal_close(pCal);
            break;
        }
        case LocaleNumber_ReadingLayout:
        {
            // corresponds to values 0 and 1 in LOCALE_IREADINGLAYOUT (values 2 and 3 not
            // used in coreclr)
            //  0 - Left to right (such as en-US)
            //  1 - Right to left (such as arabic locales)
            ULayoutType orientation = uloc_getCharacterOrientation(locale, &status);
            // alternative implementation in ICU 54+ is uloc_isRightToLeft() which
            // also supports script tags in locale
            if (U_SUCCESS(status))
            {
                *value = (orientation == ULOC_LAYOUT_RTL) ? 1 : 0;
            }
            break;
        }
        case LocaleNumber_FirstDayofWeek:
        {
            UCalendar* pCal = ucal_open(NULL, 0, locale, UCAL_TRADITIONAL, &status);

            if (U_SUCCESS(status))
            {
                *value = ucal_getAttribute(pCal, UCAL_FIRST_DAY_OF_WEEK) - 1; // .NET is 0-based and ICU is 1-based
            }
            ucal_close(pCal);
            break;
        }
        case LocaleNumber_NegativePercentFormat:
            *value = GetPercentNegativePattern(locale);
            break;
        case LocaleNumber_PositivePercentFormat:
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
int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(
    const UChar* localeName, LocaleNumberData localeGroupingData, int32_t* primaryGroupSize, int32_t* secondaryGroupSize)
{
    UErrorCode status = U_ZERO_ERROR;
    char locale[ULOC_FULLNAME_CAPACITY];
    GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &status);

    if (U_FAILURE(status))
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    UNumberFormatStyle style;
    switch (localeGroupingData)
    {
        case LocaleNumber_Digit:
            style = UNUM_DECIMAL;
            break;
        case LocaleNumber_Monetary:
            style = UNUM_CURRENCY;
            break;
        default:
            return UErrorCodeToBool(U_UNSUPPORTED_ERROR);
    }

    UNumberFormat* numformat = unum_open(style, NULL, 0, locale, NULL, &status);
    if (U_SUCCESS(status))
    {
        *primaryGroupSize = unum_getAttribute(numformat, UNUM_GROUPING_SIZE);
        *secondaryGroupSize = unum_getAttribute(numformat, UNUM_SECONDARY_GROUPING_SIZE);
        unum_close(numformat);
    }

    return UErrorCodeToBool(status);
}
