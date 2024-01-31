// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <string.h>
#include "pal_hybrid.h"
#include "pal_locale_hg.h"
#include "pal_localeStringData.h"
#include "pal_localeNumberData.h"

#import <Foundation/Foundation.h>
#import <Foundation/NSFormatter.h>

#if !__has_feature(objc_arc)
#error This file relies on ARC for memory management, but ARC is not enabled.
#endif

const char* GlobalizationNative_GetLocaleNameNative(const char* localeName)
{
    @autoreleasepool
    {
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
        const char* value = [currentLocale.localeIdentifier UTF8String];
        return strdup(value);
    }
}

/**
 * Useful constant for the maximum size of the whole locale ID
 * (including the terminating NULL and all keywords).
 */
#define FULLNAME_CAPACITY 157

static void GetParent(const char* localeID, char* parent, int32_t parentCapacity)
{
    const char *lastUnderscore;
    int32_t i;

    if (localeID == NULL)
        localeID = [NSLocale systemLocale].localeIdentifier.UTF8String;

    lastUnderscore = strrchr(localeID, '-');
    if (lastUnderscore != NULL)
    {
        i = (int32_t)(lastUnderscore - localeID);
    }
    else
    {
        i = 0;
    }

    if (i > 0)
    {
        // primary lang subtag und (undefined).
        if (strncasecmp(localeID, "und-", 4) == 0)
        {
            localeID += 3;
            i -= 3;
            memmove(parent, localeID, MIN(i, parentCapacity));
        }
        else if (parent != localeID)
        {
            memcpy(parent, localeID, MIN(i, parentCapacity));
        }
    }

    // terminate chars 
    if (i >= 0 && i < parentCapacity)
       parent[i] = 0;
}

/**
 * Lookup 'key' in the array 'list'.  The array 'list' should contain
 * a NULL entry, followed by more entries, and a second NULL entry.
 *
 * The 'list' param should be LANGUAGES, LANGUAGES_3, COUNTRIES, or
 * COUNTRIES_3.
 */
static int16_t _findIndex(const char* const* list, const char* key)
{
    const char* const* anchor = list;
    int32_t pass = 0;

    /* Make two passes through two NULL-terminated arrays at 'list' */
    while (pass++ < 2) {
        while (*list) {
            if (strcmp(key, *list) == 0) {
                return (int16_t)(list - anchor);
            }
            list++;
        }
        ++list;     /* skip final NULL *CWB*/
    }
    return -1;
}

static const char* getISO3CountryByCountryCode(const char* countryCode)
{
    int16_t offset = _findIndex(COUNTRIES, countryCode);
    if (offset < 0)
        return "";

    return COUNTRIES_3[offset];
}

static const char* getISO3LanguageByLangCode(const char* langCode)
{
    int16_t offset = _findIndex(LANGUAGES, langCode);
    if (offset < 0)
        return "";
    return LANGUAGES_3[offset];
}

const char* GlobalizationNative_GetLocaleInfoStringNative(const char* localeName, LocaleStringData localeStringData, const char* currentUILocaleName)
{
    @autoreleasepool
    {
        NSString *value;
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        numberFormatter.locale = currentLocale;
        NSDateFormatter* dateFormatter = [[NSDateFormatter alloc] init];
        [dateFormatter setLocale:currentLocale];
        NSLocale *gbLocale = [[NSLocale alloc] initWithLocaleIdentifier:@"en_GB"];

        switch (localeStringData)
        {
            ///// <summary>localized name of locale, eg "German (Germany)" in UI language (corresponds to LOCALE_SLOCALIZEDDISPLAYNAME)</summary>
            case LocaleString_LocalizedDisplayName:
            {
                NSString *currUILocaleName = [NSString stringWithFormat:@"%s", currentUILocaleName == NULL ? GlobalizationNative_GetDefaultLocaleNameNative() : currentUILocaleName];
                NSLocale *currentUILocale = [[NSLocale alloc] initWithLocaleIdentifier:currUILocaleName];
                value = [currentUILocale displayNameForKey:NSLocaleIdentifier value:currentLocale.localeIdentifier];
                break;
            }
            /// <summary>Display name (language + country usually) in English, eg "German (Germany)" (corresponds to LOCALE_SENGLISHDISPLAYNAME)</summary>
            case LocaleString_EnglishDisplayName:
                value = [gbLocale displayNameForKey:NSLocaleIdentifier value:currentLocale.localeIdentifier];
                break;
            /// <summary>Display name in native locale language, eg "Deutsch (Deutschland) (corresponds to LOCALE_SNATIVEDISPLAYNAME)</summary>
            case LocaleString_NativeDisplayName:
                value = [currentLocale displayNameForKey:NSLocaleIdentifier value:currentLocale.localeIdentifier];
                break;
            /// <summary>Language Display Name for a language, eg "German" in UI language (corresponds to LOCALE_SLOCALIZEDLANGUAGENAME)</summary>
            case LocaleString_LocalizedLanguageName:
            {
                NSString *currUILocaleName = [NSString stringWithFormat:@"%s", currentUILocaleName == NULL ? GlobalizationNative_GetDefaultLocaleNameNative() : currentUILocaleName];
                NSLocale *currentUILocale = [[NSLocale alloc] initWithLocaleIdentifier:currUILocaleName];
                value = [currentUILocale localizedStringForLanguageCode:currentLocale.languageCode];
                break;
            }
            /// <summary>English name of language, eg "German" (corresponds to LOCALE_SENGLISHLANGUAGENAME)</summary>
            case LocaleString_EnglishLanguageName:
                value = [gbLocale localizedStringForLanguageCode:currentLocale.languageCode];
                break;
            /// <summary>native name of language, eg "Deutsch" (corresponds to LOCALE_SNATIVELANGUAGENAME)</summary>
            case LocaleString_NativeLanguageName:
                value = [currentLocale localizedStringForLanguageCode:currentLocale.languageCode];
            break;
            /// <summary>English name of country, eg "Germany" (corresponds to LOCALE_SENGLISHCOUNTRYNAME)</summary>
            case LocaleString_EnglishCountryName:
                value = [gbLocale localizedStringForCountryCode:currentLocale.countryCode];
                break;
            /// <summary>native name of country, eg "Deutschland" (corresponds to LOCALE_SNATIVECOUNTRYNAME)</summary>
            case LocaleString_NativeCountryName:
                value = [currentLocale localizedStringForCountryCode:currentLocale.countryCode];
                break;
            case LocaleString_ThousandSeparator:
                value = currentLocale.groupingSeparator;
                break;
            case LocaleString_DecimalSeparator:
                value = currentLocale.decimalSeparator;
                // or value = [[currentLocale objectForKey:NSLocaleDecimalSeparator] UTF8String];
                break;
            case LocaleString_Digits:
            {
                NSString *digitsString = @"0123456789";
                NSNumberFormatter *nf1 = [[NSNumberFormatter alloc] init];
                [nf1 setLocale:currentLocale];

                NSNumber *newNum = [nf1 numberFromString:digitsString];
                value = [newNum stringValue];
                break;
            }
            case LocaleString_MonetarySymbol:
                value = currentLocale.currencySymbol;
                break;
            case LocaleString_Iso4217MonetarySymbol:
                // check if this is correct, check currencyISOCode
                value = currentLocale.currencyCode;
                break;
            case LocaleString_CurrencyEnglishName:
                value = [gbLocale localizedStringForCurrencyCode:currentLocale.currencyCode];
                break;
            case LocaleString_CurrencyNativeName:
                value = [currentLocale localizedStringForCurrencyCode:currentLocale.currencyCode];
                break;
            case LocaleString_MonetaryDecimalSeparator:
                value = numberFormatter.currencyDecimalSeparator;
                break;
            case LocaleString_MonetaryThousandSeparator:
                value = numberFormatter.currencyGroupingSeparator;
                break;
            case LocaleString_AMDesignator:
                value = dateFormatter.AMSymbol;
                break;
            case LocaleString_PMDesignator:
                value = dateFormatter.PMSymbol;
                break;
            case LocaleString_PositiveSign:
                value = numberFormatter.plusSign;
                break;
            case LocaleString_NegativeSign:
                value = numberFormatter.minusSign;
                break;
            case LocaleString_Iso639LanguageTwoLetterName:
                value = [currentLocale objectForKey:NSLocaleLanguageCode];
                break;
            case LocaleString_Iso639LanguageThreeLetterName:
            {
                NSString *iso639_2 = [currentLocale objectForKey:NSLocaleLanguageCode];
                return iso639_2 == nil ? strdup("") : strdup(getISO3LanguageByLangCode([iso639_2 UTF8String]));
            }
            case LocaleString_Iso3166CountryName:
                value = [currentLocale objectForKey:NSLocaleCountryCode];
                break;
            case LocaleString_Iso3166CountryName2:
            {
                NSString* countryCode = [currentLocale objectForKey:NSLocaleCountryCode];
                return countryCode == nil ? strdup("") : strdup(getISO3CountryByCountryCode([countryCode UTF8String]));
            }
            case LocaleString_NaNSymbol:
                value = numberFormatter.notANumberSymbol;
                break;
            case LocaleString_PositiveInfinitySymbol:
                value = numberFormatter.positiveInfinitySymbol;
                break;
            case LocaleString_NegativeInfinitySymbol:
                value = numberFormatter.negativeInfinitySymbol;
                break;
            case LocaleString_PercentSymbol:
                value = numberFormatter.percentSymbol;
                break;
            case LocaleString_PerMilleSymbol:
                value = numberFormatter.perMillSymbol;
                break;
            case LocaleString_ParentName:
            {
                char localeNameTemp[FULLNAME_CAPACITY];
                const char* lName = [currentLocale.localeIdentifier UTF8String];
                GetParent(lName, localeNameTemp, FULLNAME_CAPACITY);
                return strdup(localeNameTemp);
            }
            default:
                value = nil;
                break;
        }

        return value == nil ? strdup("") : strdup([value UTF8String]);
    }
}

// invariant character definitions
#define CHAR_CURRENCY ((char)0x00A4)   // international currency
#define CHAR_SPACE ((char)0x0020)      // space
#define CHAR_NBSPACE ((char)0x00A0)    // no-break space
#define CHAR_DIGIT ((char)0x0023)      // '#'
#define CHAR_MINUS ((char)0x002D)      // '-'
#define CHAR_PERCENT ((char)0x0025)    // '%'
#define CHAR_OPENPAREN ((char)0x0028)  // '('
#define CHAR_CLOSEPAREN ((char)0x0029) // ')'
#define CHAR_ZERO ((char)0x0030)       // '0'

/*
Function:
NormalizeNumericPattern

Returns a numeric string pattern in a format that we can match against the
appropriate managed pattern. Examples:
For PositiveMonetaryNumberFormat "¤#,##0.00" becomes "Cn"
For NegativeNumberFormat "#,##0.00;(#,##0.00)" becomes "(n)"
*/
static char* NormalizeNumericPattern(const char* srcPattern, int isNegative)
{
    int iStart = 0;
    int iEnd = strlen(srcPattern);

    // ';'  separates positive and negative subpatterns.
    // When there is no explicit negative subpattern,
    // an implicit negative subpattern is formed from the positive pattern with a prefixed '-'.
    char * ptrNegativePattern = strrchr(srcPattern,';');
    if (ptrNegativePattern)
    {
        int32_t iNegativePatternStart = ptrNegativePattern - srcPattern;
        if (isNegative)
        {
            iStart = iNegativePatternStart + 1;
        }
        else
        {
            iEnd = iNegativePatternStart - 1;
        }
    }

    int minusAdded = false;

    for (int i = iStart; i <= iEnd; i++)
    {
        switch (srcPattern[i])
        {
            case CHAR_MINUS:
            case CHAR_OPENPAREN:
            case CHAR_CLOSEPAREN:
                minusAdded = true;
                break;
        }

        if (minusAdded)
           break;
    }

    // international currency symbol (CHAR_CURRENCY)
    // The positive pattern comes first, then an optional negative pattern
    // separated by a semicolon
    // A destPattern example: "(C n)" where C represents the currency symbol, and
    // n is the number
    char* destPattern;
    int index = 0;

    // if there is no negative subpattern, prefix the minus sign
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

    int digitAdded = false;
    int currencyAdded = false;
    int spaceAdded = false;

    for (int i = iStart; i <= iEnd; i++)
    {
        char ch = srcPattern[i];
        switch (ch)
        {
            case CHAR_DIGIT:
            case CHAR_ZERO:
                if (!digitAdded)
                {
                    digitAdded = true;
                    destPattern[index++] = 'n';
                }
                break;

            case CHAR_CURRENCY:
                if (!currencyAdded)
                {
                    currencyAdded = true;
                    destPattern[index++] = 'C';
                }
                break;

            case CHAR_SPACE:
            case CHAR_NBSPACE:
                if (!spaceAdded)
                {
                    spaceAdded = true;
                    destPattern[index++] = ' ';
                }
                break;

            case CHAR_MINUS:
            case CHAR_OPENPAREN:
            case CHAR_CLOSEPAREN:
            case CHAR_PERCENT:
                destPattern[index++] = ch;
                break;
        }
    }

    const int MAX_DOTNET_NUMERIC_PATTERN_LENGTH = 6; // example: "(C n)" plus terminator

    if (destPattern[0] == '\0' || strlen (destPattern) >= MAX_DOTNET_NUMERIC_PATTERN_LENGTH)
    {
        free (destPattern);
        return NULL;
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
static int GetPatternIndex(char* normalizedPattern,const char* patterns[], int patternsCount)
{
    const int INVALID_FORMAT = -1;

    if (!normalizedPattern)
    {
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

static int32_t GetValueForNumberFormat(NSLocale *currentLocale, LocaleNumberData localeNumberData)
{
    NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
    numberFormatter.locale = currentLocale;
    const char *pFormat;
    int32_t value;

    switch(localeNumberData)
    {
        case LocaleNumber_PositiveMonetaryNumberFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
            static const char* Patterns[] = {"Cn", "nC", "C n", "n C"};
            pFormat = [[numberFormatter positiveFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, false);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_NegativeMonetaryNumberFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
            static const char* Patterns[] = {"(Cn)", "-Cn", "C-n", "Cn-", "(nC)", "-nC", "n-C", "nC-", "-n C",
                                             "-C n", "n C-", "C n-", "C -n", "n- C", "(C n)", "(n C)", "C- n" };
            pFormat = [[numberFormatter negativeFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, true);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_NegativeNumberFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
            static const char* Patterns[] = {"(n)", "-n", "- n", "n-", "n -"};
            pFormat = [[numberFormatter negativeFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, true);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_NegativePercentFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterPercentStyle;
            static const char* Patterns[] = {"-n %", "-n%", "-%n", "%-n", "%n-", "n-%", "n%-", "-% n", "n %-", "% n-", "% -n", "n- %"};
            pFormat = [[numberFormatter negativeFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, true);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_PositivePercentFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterPercentStyle;
            static const char* Patterns[] = {"n %", "n%", "%n", "% n"};
            pFormat = [[numberFormatter positiveFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, false);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        default:
            return -1;
    }

    return value;
}

int32_t GlobalizationNative_GetLocaleInfoIntNative(const char* localeName, LocaleNumberData localeNumberData)
{
    @autoreleasepool
    {
#ifndef NDEBUG
        bool isSuccess = true;
#endif
        int32_t value;
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];

        switch (localeNumberData)
        {
            case LocaleNumber_MeasurementSystem:
            {
                const char *measurementSystem = [[currentLocale objectForKey:NSLocaleMeasurementSystem] UTF8String];
                NSLocale *usLocale = [[NSLocale alloc] initWithLocaleIdentifier:@"en_US"];
                const char *us_measurementSystem = [[usLocale objectForKey:NSLocaleMeasurementSystem] UTF8String];
                value = (measurementSystem == us_measurementSystem) ? 1 : 0;
                break;
            }
            case LocaleNumber_FractionalDigitsCount:
            {
                NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
                numberFormatter.locale = currentLocale;
                numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
                value = (int32_t)numberFormatter.maximumFractionDigits;
                break;
            }
            case LocaleNumber_MonetaryFractionalDigitsCount:
            {
                NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
                numberFormatter.locale = currentLocale;
                numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
                value = (int32_t)numberFormatter.maximumFractionDigits;
                break;
            }
            case LocaleNumber_PositiveMonetaryNumberFormat:
            case LocaleNumber_NegativeMonetaryNumberFormat:
            case LocaleNumber_NegativeNumberFormat:
            case LocaleNumber_NegativePercentFormat:
            case LocaleNumber_PositivePercentFormat:
            {
                value = GetValueForNumberFormat(currentLocale, localeNumberData);
#ifndef NDEBUG
                if (value < 0)
                {
                    isSuccess = false;
                }
#endif
                break;
            }
            case LocaleNumber_FirstWeekOfYear:
            {
                NSCalendar *calendar = [currentLocale objectForKey:NSLocaleCalendar];
                int minDaysInWeek = (int32_t)[calendar minimumDaysInFirstWeek];
                if (minDaysInWeek == 1)
                {
                    value = WeekRule_FirstDay;
                }
                else if (minDaysInWeek == 7)
                {
                    value = WeekRule_FirstFullWeek;
                }
                else if (minDaysInWeek >= 4)
                {
                    value = WeekRule_FirstFourDayWeek;
                }
                else
                {
                    value = -1;
#ifndef NDEBUG
                    isSuccess = false;
#endif
                }
                break;
            }
            case LocaleNumber_ReadingLayout:
            {
                NSLocaleLanguageDirection langDir = [NSLocale characterDirectionForLanguage:[currentLocale objectForKey:NSLocaleLanguageCode]];
                //  0 - Left to right (such as en-US)
                //  1 - Right to left (such as arabic locales)
                value = NSLocaleLanguageDirectionRightToLeft == langDir ? 1 : 0;
                break;
            }
            case LocaleNumber_FirstDayofWeek:
            {
                NSCalendar *calendar = [currentLocale objectForKey:NSLocaleCalendar];
                value = [calendar firstWeekday] - 1; // .NET is 0-based and in Apple is 1-based;
                break;
            }
            default:
                value = -1;
#ifndef NDEBUG
                isSuccess = false;
#endif
                break;
        }

        assert(isSuccess);

        return value;
    }
}

/*
PAL Function:
GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative

Returns primary grouping size for decimal and currency
*/
int32_t GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData)
{
    @autoreleasepool
    {
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        numberFormatter.locale = currentLocale;

        switch (localeGroupingData)
        {
            case LocaleNumber_Digit:
                numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
                break;
            case LocaleNumber_Monetary:
                numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
                break;
            default:
                assert(false);
                break;
        }
        return [numberFormatter groupingSize];
    }
}

/*
PAL Function:
GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative

Returns secondary grouping size for decimal and currency
*/
int32_t GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData)
{
    @autoreleasepool
    {
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
        NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
        numberFormatter.locale = currentLocale;

        switch (localeGroupingData)
        {
            case LocaleNumber_Digit:
                numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
                break;
            case LocaleNumber_Monetary:
                numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
                break;
            default:
                assert(false);
                break;
        }

        return [numberFormatter secondaryGroupingSize];
    }
}

/*
PAL Function:
GlobalizationNative_GetLocaleTimeFormatNative

Returns time format information (in native format, it needs to be converted to .NET's format).
*/
const char* GlobalizationNative_GetLocaleTimeFormatNative(const char* localeName, int shortFormat)
{
    @autoreleasepool
    {
        NSString *locName = [NSString stringWithFormat:@"%s", localeName];
        NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
        NSDateFormatter* dateFormatter = [[NSDateFormatter alloc] init];
        [dateFormatter setLocale:currentLocale];

        if (shortFormat != 0)
        {
            [dateFormatter setTimeStyle:NSDateFormatterShortStyle];
        }
        else
        {
            [dateFormatter setTimeStyle:NSDateFormatterMediumStyle];
        }

        return strdup([[dateFormatter dateFormat] UTF8String]);
    }
}

// GlobalizationNative_GetLocalesNative gets all locale names and store it in the value buffer
// in case of success, it returns the count of the characters stored in value buffer
// in case of failure, it returns negative number.
// if the input value buffer is null, it returns the length needed to store the
// locale names list.
// if the value is not null, it fills the value with locale names separated by the length
// of each name.
int32_t GlobalizationNative_GetLocalesNative(UChar* value, int32_t length)
{
    @autoreleasepool
    {
        NSArray<NSString*>* availableLocaleIdentifiers = [NSLocale availableLocaleIdentifiers];
        int32_t index = 0;
        int32_t totalLength = 0;
        int32_t availableLength = (int32_t)[availableLocaleIdentifiers count];

        if (availableLength <=  0)
            return -1; // failed

        for (NSInteger i = 0; i < availableLength; i++) 
        {
            NSString *localeIdentifier = availableLocaleIdentifiers[i];
            int32_t localeNameLength = localeIdentifier.length;
            totalLength += localeNameLength + 1; // add 1 for the name length
            if (value != NULL)
            {
                if (totalLength > length)
                    return -3;

                value[index++] = (UChar) localeNameLength;

                for (int j = 0; j < localeNameLength; j++)
                {
                    if ((UChar)[localeIdentifier characterAtIndex:j] == '_')
                    {
                        value[index++] = (UChar) '-';
                    }
                    else
                    {
                        value[index++] = (UChar) [localeIdentifier characterAtIndex:j];
                    }
                }
            }
        }
        return totalLength;
    }
}

const char* GlobalizationNative_GetDefaultLocaleNameNative(void)
{
    @autoreleasepool
    {
        if (NSLocale.preferredLanguages.count > 0)
        {
            return strdup([NSLocale.preferredLanguages[0] UTF8String]);
        }
        else
        {
            NSLocale *currentLocale = [NSLocale currentLocale];
            NSString *localeName = @"";

            if (!currentLocale)
            {
                return strdup([localeName UTF8String]);
            }

            if ([currentLocale.languageCode length] > 0 && [currentLocale.countryCode length] > 0)
            {
                localeName = [NSString stringWithFormat:@"%@-%@", currentLocale.languageCode, currentLocale.countryCode];
            }
            else
            {
                localeName = currentLocale.localeIdentifier;
            }

            return strdup([localeName UTF8String]);
        }
    }
}

// GlobalizationNative_IsPredefinedLocaleNative returns TRUE if localeName exists in availableLocaleIdentifiers.
// Otherwise it returns FALSE;

int32_t GlobalizationNative_IsPredefinedLocaleNative(const char* localeName)
{
    @autoreleasepool
    {
        NSString *localeIdentifier = [NSString stringWithFormat:@"%s", localeName];
        NSString *localeIdentifierByRegionDesignator = [localeIdentifier stringByReplacingOccurrencesOfString:@"-" withString:@"_"];
        NSArray<NSString *> *availableLocales = [NSLocale availableLocaleIdentifiers];

        if ([availableLocales containsObject:localeIdentifier] || [availableLocales containsObject:localeIdentifierByRegionDesignator])
        {
            return true;
        }

        return false;
    }
}

