// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"
#include "pal_localeStringData.h"

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
#endif

#if defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)
const char* GlobalizationNative_GetICUDataPathFallback(void)
{
    NSString *bundlePath = [[NSBundle mainBundle] pathForResource:@"icudt" ofType:@"dat"];
    return strdup([bundlePath UTF8String]);
}
#endif
