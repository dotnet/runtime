// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"
#include "pal_localeStringData.h"
#include "pal_localeNumberData.h"

#import <Foundation/Foundation.h>
#import <Foundation/NSFormatter.h>

char* DetectDefaultAppleLocaleName()
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

#if defined(TARGET_OSX) || defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)

const char* GlobalizationNative_GetLocaleNameNative(const char* localeName)
{
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    const char* value = [currentLocale.localeIdentifier UTF8String];
    return strdup(value);
}

const char* GlobalizationNative_GetLocaleInfoStringNative(const char* localeName, LocaleStringData localeStringData)
{
    const char* value;
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
        /// <summary>Display name (language + country usually) in English, eg "German (Germany)" (corresponds to LOCALE_SENGLISHDISPLAYNAME)</summary>
        case LocaleString_EnglishDisplayName:
            value = [[gbLocale displayNameForKey:NSLocaleIdentifier value:currentLocale.localeIdentifier] UTF8String];
           break;
        /// <summary>Display name in native locale language, eg "Deutsch (Deutschland) (corresponds to LOCALE_SNATIVEDISPLAYNAME)</summary>
        case LocaleString_NativeDisplayName:
            value = [[currentLocale displayNameForKey:NSLocaleIdentifier value:currentLocale.localeIdentifier] UTF8String];
            break;
        /// <summary>Language Display Name for a language, eg "German" in UI language (corresponds to LOCALE_SLOCALIZEDLANGUAGENAME)</summary>
        case LocaleString_LocalizedLanguageName:
        /// <summary>English name of language, eg "German" (corresponds to LOCALE_SENGLISHLANGUAGENAME)</summary>
        case LocaleString_EnglishLanguageName:
            value = [[gbLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
            break;
        /// <summary>native name of language, eg "Deutsch" (corresponds to LOCALE_SNATIVELANGUAGENAME)</summary>
        case LocaleString_NativeLanguageName:
            value = [[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
           break;
        /// <summary>English name of country, eg "Germany" (corresponds to LOCALE_SENGLISHCOUNTRYNAME)</summary>
        case LocaleString_EnglishCountryName:
            value = [[gbLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            break;
        /// <summary>native name of country, eg "Deutschland" (corresponds to LOCALE_SNATIVECOUNTRYNAME)</summary>
        case LocaleString_NativeCountryName:
            value = [[currentLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            break;
        case LocaleString_ThousandSeparator:
            value = [currentLocale.groupingSeparator UTF8String];
            break;
        case LocaleString_DecimalSeparator:
            value = [currentLocale.decimalSeparator UTF8String];
            // or value = [[currentLocale objectForKey:NSLocaleDecimalSeparator] UTF8String];
            break;
        case LocaleString_MonetarySymbol:
            value = [currentLocale.currencySymbol UTF8String];
            break;
        case LocaleString_Iso4217MonetarySymbol:
            // check if this is correct, check currencyISOCode
            value = [currentLocale.currencySymbol UTF8String];
            break;
        case LocaleString_CurrencyEnglishName:
            value = [[gbLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            break;
        case LocaleString_CurrencyNativeName:
            value = [[currentLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            break;
        case LocaleString_AMDesignator:
            value = [dateFormatter.AMSymbol UTF8String];
            break;
        case LocaleString_PMDesignator:
            value = [dateFormatter.PMSymbol UTF8String];
            break;
        case LocaleString_PositiveSign:
            value = [numberFormatter.plusSign UTF8String];
            break;
        case LocaleString_NegativeSign:
            value = [numberFormatter.minusSign UTF8String];
            break;
        case LocaleString_Iso639LanguageTwoLetterName:
            // check if this is correct
            value = [[currentLocale objectForKey:NSLocaleLanguageCode] UTF8String];
            break;
        case LocaleString_Iso3166CountryName:
            value = [currentLocale.countryCode UTF8String];
            break;
        case LocaleString_NaNSymbol:
            value = [numberFormatter.notANumberSymbol UTF8String];
            break;
        case LocaleString_PositiveInfinitySymbol:
            value = [numberFormatter.positiveInfinitySymbol UTF8String];
            break;
        case LocaleString_NegativeInfinitySymbol:
            value = [numberFormatter.negativeInfinitySymbol UTF8String];
            break;
        case LocaleString_PercentSymbol:
            value = [numberFormatter.percentSymbol UTF8String];
            break;
        case LocaleString_PerMilleSymbol:
            value = [numberFormatter.perMillSymbol UTF8String];
            break;
        // TODO find mapping for below cases
        // https://github.com/dotnet/runtime/issues/83514
        case LocaleString_Digits:
        case LocaleString_MonetaryDecimalSeparator:
        case LocaleString_MonetaryThousandSeparator:
        case LocaleString_Iso639LanguageThreeLetterName:
        case LocaleString_ParentName:
        case LocaleString_Iso3166CountryName2:            
        default:
            value = "";
            break;
    }
    return strdup(value);
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
    bool isSuccess = true;
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
            if (value < 0)
            {
                isSuccess = false;
            }
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
                isSuccess = false;
            }
            break;
        }        
        case LocaleNumber_ReadingLayout:
        {
            NSLocaleLanguageDirection langDir = [NSLocale characterDirectionForLanguage:[[NSLocale currentLocale] objectForKey:NSLocaleLanguageCode]];
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
            isSuccess = false;
            break;
    }

    assert(isSuccess);

    return value;
}

/*
PAL Function:
GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative

Returns primary grouping size for decimal and currency
*/
int32_t GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData)
{
    bool isSuccess = true;
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
            isSuccess = false;
            assert(isSuccess);
            break;
    }
    return [numberFormatter groupingSize];    
}

/*
PAL Function:
GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative

Returns secondary grouping size for decimal and currency
*/
int32_t GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData)
{
    bool isSuccess = true;
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
            isSuccess = false;
            assert(isSuccess);
            break;
    }

    return [numberFormatter secondaryGroupingSize];
}

/*
PAL Function:
GlobalizationNative_GetLocaleTimeFormatNative

Returns time format information (in native format, it needs to be converted to .NET's format).
*/
const char* GlobalizationNative_GetLocaleTimeFormatNative(const char* localeName, int shortFormat)
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

#endif

#if defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)
const char* GlobalizationNative_GetICUDataPathFallback(void)
{
    NSString *bundlePath = [[NSBundle mainBundle] pathForResource:@"icudt" ofType:@"dat"];
    return strdup([bundlePath UTF8String]);
}
#endif
