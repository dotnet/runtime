// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"

#import <Foundation/Foundation.h>
#import <Foundation/NSFormatter.h>

#include "pal_localeStringData.h"

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

int32_t NativeGetLocaleName(const UChar* localeName,
                                         UChar* value,
                                         int32_t valueLength)
{
     UErrorCode status = U_ZERO_ERROR;
     NSLocale *currentLocale = [NSLocale currentLocale]; 
     value = (UChar *)[currentLocale.localeIdentifier UTF8String]; 
     return UErrorCodeToBool(status);
}


UChar* NativeGetLocaleInfoString(const UChar* localeName,
                                                LocaleStringData localeStringData,
                                                UChar* value,
                                                int32_t valueLength,
                                                const UChar* uiLocaleName)
{
    NSLog(@"NSLog NativeGetLocaleInfoString is running");
    NSLocale *currentLocale = [NSLocale currentLocale];
    NSLocale *locale = [NSLocale autoupdatingCurrentLocale];

    NSLog(@"NSLog NativeGetLocaleInfoString is running after NSLocale");
   // UErrorCode status = U_ZERO_ERROR;
    NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
    NSLog(@"NSLog NativeGetLocaleInfoString is running after NSNumberFormatter");
   /* if (U_FAILURE(status))
    {
       // return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
       return value;
    }*/

    switch (localeStringData)
    {
        case LocaleString_LocalizedDisplayName:
        NSLog(@"NSLog LocaleString_LocalizedDisplayName case is running");
            value = (UChar *)[[currentLocale localizedStringForLocaleIdentifier:currentLocale.localeIdentifier] UTF8String];
            //return value;
            break;
        case LocaleString_EnglishDisplayName:
        NSLog(@"NSLog LocaleString_EnglishDisplayName case is running");
        //check why is this wrong?
            value = (UChar *)[locale.localeIdentifier UTF8String];
           //value = (UChar *)[[currentLocale localizedStringForLocaleIdentifier:currentLocale.localeIdentifier] UTF8String];
           //return value;
           break;
        case LocaleString_NativeDisplayName:
        NSLog(@"NSLog LocaleString_NativeDisplayName case is running");
            value = (UChar *)[currentLocale.localeIdentifier UTF8String];
            //return value;
            break;
        case LocaleString_LocalizedLanguageName:
        NSLog(@"NSLog LocaleString_LocalizedLanguageName case is running");
            value = (UChar *)[[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
            //return value;
            break;
        case LocaleString_EnglishLanguageName:
        NSLog(@"NSLog LocaleString_EnglishLanguageName case is running");
            value = (UChar *)[[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
            //return value;
            break;
        case LocaleString_NativeLanguageName:
        NSLog(@"NSLog LocaleString_NativeLanguageName case is running");
            value = (UChar *)[[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
           //return value;
           break;
        case LocaleString_EnglishCountryName:
        NSLog(@"NSLog LocaleString_EnglishCountryName case is running");
            value = (UChar *)[[currentLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            //return value;
            break;
        case LocaleString_NativeCountryName:
        NSLog(@"NSLog LocaleString_NativeCountryName case is running");
            value = (UChar *)[[currentLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            //return value;
            break;
        case LocaleString_ThousandSeparator:
        NSLog(@"NSLog LocaleString_ThousandSeparator case is running");
            value = (UChar *)[currentLocale.groupingSeparator UTF8String];
            //return value;
            break;
        case LocaleString_DecimalSeparator:
        NSLog(@"NSLog LocaleString_DecimalSeparator case is running");
            value = (UChar *)[currentLocale.decimalSeparator UTF8String];
            //return value;
            break;
        case LocaleString_Digits:
        NSLog(@"NSLog LocaleString_Digits case is running");
            // TODO
           //return value;
           break;
        case LocaleString_MonetarySymbol:
        NSLog(@"NSLog LocaleString_MonetarySymbol case is running");
            value = (UChar *)[currentLocale.currencySymbol UTF8String];
            //return value;
            break;
        case LocaleString_Iso4217MonetarySymbol:
        NSLog(@"NSLog LocaleString_Iso4217MonetarySymbol case is running");
            // check if this is correct
            value = (UChar *)[currentLocale.currencySymbol UTF8String];
            //return value;
            break;
        case LocaleString_CurrencyEnglishName:
        NSLog(@"NSLog LocaleString_CurrencyEnglishName case is running");
            value = (UChar *)[[currentLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            //return value;
            break;
        case LocaleString_CurrencyNativeName:
        NSLog(@"NSLog LocaleString_CurrencyNativeName case is running");
            value = (UChar *)[[currentLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            //return value;
            break;
        case LocaleString_MonetaryDecimalSeparator:
        NSLog(@"NSLog LocaleString_MonetaryDecimalSeparator case is running");
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_SEPARATOR_SYMBOL, value, valueLength, NULL);
            //return value;
            break;
        case LocaleString_MonetaryThousandSeparator:
        NSLog(@"NSLog LocaleString_MonetaryThousandSeparator case is running");
            // TODO
            /*status =
                GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_GROUPING_SEPARATOR_SYMBOL, value, valueLength, NULL);*/
            //return value;
            break;
        case LocaleString_AMDesignator:
        NSLog(@"NSLog LocaleString_AMDesignator case is running");
            // TODO
            //status = GetLocaleInfoAmPm(locale, true, value, valueLength);
            //return value;
            break;
        case LocaleString_PMDesignator:
        NSLog(@"NSLog LocaleString_PMDesignator case is running");
            // TODO
            //status = GetLocaleInfoAmPm(locale, false, value, valueLength);
            //return value;
            break;
        case LocaleString_PositiveSign:
        NSLog(@"NSLog LocaleString_PositiveSign case is running");
            value = (UChar *)[numberFormatter.plusSign UTF8String];
            //return value;
            break;
        case LocaleString_NegativeSign:
        NSLog(@"NSLog LocaleString_NegativeSign case is running");
            value = (UChar *)[numberFormatter.minusSign UTF8String];
            //return value;
            break;
        case LocaleString_Iso639LanguageTwoLetterName:
        NSLog(@"NSLog LocaleString_LocalizedDisplayName case is running");
            // TODO
            //status = GetLocaleIso639LanguageTwoLetterName(locale, value, valueLength);
            //return value;
            break;
        case LocaleString_Iso639LanguageThreeLetterName:
        NSLog(@"NSLog LocaleString_Iso639LanguageThreeLetterName case is running");
            // TODO
            //status = GetLocaleIso639LanguageThreeLetterName(locale, value, valueLength);
            //return value;
            break;
        case LocaleString_Iso3166CountryName:
        NSLog(@"NSLog LocaleString_Iso3166CountryName case is running");
            // TODO
            value = (UChar *)[currentLocale.countryCode UTF8String];
            //status = GetLocaleIso3166CountryName(locale, value, valueLength);
            //return value;
            break;
        case LocaleString_Iso3166CountryName2:
        NSLog(@"NSLog LocaleString_Iso3166CountryName2 case is running");
            // TODO
            //status = GetLocaleIso3166CountryCode(locale, value, valueLength);
            //return value;
            break;
        case LocaleString_NaNSymbol:
        NSLog(@"NSLog LocaleString_NaNSymbol case is running");
            value = (UChar *)[numberFormatter.notANumberSymbol UTF8String];
            //return value;
            break;
        case LocaleString_PositiveInfinitySymbol:
        NSLog(@"NSLog LocaleString_PositiveInfinitySymbol case is running");
            value = (UChar *)[numberFormatter.positiveInfinitySymbol UTF8String];
            //return value;
            break;
        case LocaleString_NegativeInfinitySymbol:
        NSLog(@"NSLog LocaleString_NegativeInfinitySymbol case is running");
            value = (UChar *)[numberFormatter.negativeInfinitySymbol UTF8String];
            //return value;
            break;
        case LocaleString_ParentName:
        NSLog(@"NSLog LocaleString_ParentName case is running");
            // TODO
          
            //return value;
            break;
        case LocaleString_PercentSymbol:
        NSLog(@"NSLog LocaleString_PercentSymbol case is running");
            value = (UChar *)[numberFormatter.percentSymbol UTF8String];
            //return value;
            break;
        case LocaleString_PerMilleSymbol:
        NSLog(@"NSLog LocaleString_PerMilleSymbol case is running");
            value = (UChar *)[numberFormatter.perMillSymbol UTF8String];
            //return value;
            break;
        default:
        NSLog(@"NSLog default case is running");
           // status = U_UNSUPPORTED_ERROR;
           //return value;
            break;
    }
     NSLog(@"Globalization nativeGetLocaleInfo value: %s", (char*)value);
    //return UErrorCodeToBool(U_ZERO_ERROR);
    return value;
}


