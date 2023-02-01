// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include "pal_locale_internal.h"

#import <Foundation/Foundation.h>
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


int32_t NativeGetLocaleInfoString(const UChar* localeName,
                                                LocaleStringData localeStringData,
                                                UChar* value,
                                                int32_t valueLength,
                                                const UChar* uiLocaleName)
{
    NSLocale *currentLocale = [NSLocale currentLocale];
    
     UErrorCode status = U_ZERO_ERROR;
   // char locale[ULOC_FULLNAME_CAPACITY] = "";
   // char uiLocale[ULOC_FULLNAME_CAPACITY] = "";
// for icu
   // GetLocale(localeName, locale, ULOC_FULLNAME_CAPACITY, false, &status);

    if (U_FAILURE(status))
    {
        return UErrorCodeToBool(U_ILLEGAL_ARGUMENT_ERROR);
    }

    switch (localeStringData)
    {
        case LocaleString_LocalizedDisplayName:
            value = (UChar *)[[currentLocale localizedStringForLocaleIdentifier:currentLocale.localeIdentifier] UTF8String];
            break;
        case LocaleString_EnglishDisplayName:
            value = (UChar *)[currentLocale.localeIdentifier UTF8String];
            break;
        case LocaleString_NativeDisplayName:
            value = (UChar *)[currentLocale.localeIdentifier UTF8String];
            break;
        case LocaleString_LocalizedLanguageName:
            value = (UChar *)[[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
            break;
        case LocaleString_EnglishLanguageName:
            value = (UChar *)[[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
            break;
        case LocaleString_NativeLanguageName:
            value = (UChar *)[[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
            break;
        case LocaleString_EnglishCountryName:
            value = (UChar *)[[currentLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            break;
        case LocaleString_NativeCountryName:
            value = (UChar *)[[currentLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            break;
        case LocaleString_ThousandSeparator:
            value = (UChar *)[currentLocale.groupingSeparator UTF8String];
            break;
        case LocaleString_DecimalSeparator:
            value = (UChar *)[currentLocale.decimalSeparator UTF8String];
            break;
        case LocaleString_Digits:
            // TODO
            /*{
                // Native digit can be more than one 16-bit character (e.g. ccp-Cakm-BD locale which using surrogate pairs to represent the native digit).
                // We'll separate the native digits in the returned buffer by the character '\uFFFF'.
                int32_t symbolLength = 0;
                status = GetDigitSymbol(locale, status, UNUM_ZERO_DIGIT_SYMBOL, 0, value, valueLength, &symbolLength);

                int32_t charIndex = symbolLength;

                if (U_SUCCESS(status) && (uint32_t)charIndex < (uint32_t)valueLength)
                {
                    value[charIndex++] = 0xFFFF;

                    // symbols UNUM_ONE_DIGIT to UNUM_NINE_DIGIT are contiguous
                    for (int32_t symbol = UNUM_ONE_DIGIT_SYMBOL; symbol <= UNUM_NINE_DIGIT_SYMBOL && charIndex < valueLength - 3; symbol++)
                    {
                        status = GetDigitSymbol(locale, status, (UNumberFormatSymbol)symbol, charIndex, value, valueLength, &symbolLength);
                        charIndex += symbolLength;
                        if (!U_SUCCESS(status) || (uint32_t)charIndex >= (uint32_t)valueLength)
                        {
                            break;
                        }

                        value[charIndex++] = 0xFFFF;
                    }

                    if ((uint32_t)charIndex < (uint32_t)valueLength)
                    {
                        value[charIndex] = 0;
                    }
                }
            }*/
            break;
        case LocaleString_MonetarySymbol:
            value = (UChar *)[currentLocale.currencySymbol UTF8String];
            break;
        case LocaleString_Iso4217MonetarySymbol:
            // check if this is correct
            value = (UChar *)[currentLocale.currencySymbol UTF8String];
            break;
        case LocaleString_CurrencyEnglishName:
            value = (UChar *)[[currentLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            break;
        case LocaleString_CurrencyNativeName:
            value = (UChar *)[[currentLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            break;
        case LocaleString_MonetaryDecimalSeparator:
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_SEPARATOR_SYMBOL, value, valueLength, NULL);
            break;
        case LocaleString_MonetaryThousandSeparator:
            // TODO
            /*status =
                GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MONETARY_GROUPING_SEPARATOR_SYMBOL, value, valueLength, NULL);*/
            break;
        case LocaleString_AMDesignator:
            // TODO
            //status = GetLocaleInfoAmPm(locale, true, value, valueLength);
            break;
        case LocaleString_PMDesignator:
            // TODO
            //status = GetLocaleInfoAmPm(locale, false, value, valueLength);
            break;
        case LocaleString_PositiveSign:
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PLUS_SIGN_SYMBOL, value, valueLength, NULL);
            break;
        case LocaleString_NegativeSign:
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_MINUS_SIGN_SYMBOL, value, valueLength, NULL);
            break;
        case LocaleString_Iso639LanguageTwoLetterName:
            // TODO
            //status = GetLocaleIso639LanguageTwoLetterName(locale, value, valueLength);
            break;
        case LocaleString_Iso639LanguageThreeLetterName:
            // TODO
            //status = GetLocaleIso639LanguageThreeLetterName(locale, value, valueLength);
            break;
        case LocaleString_Iso3166CountryName:
            // TODO
            //status = GetLocaleIso3166CountryName(locale, value, valueLength);
            break;
        case LocaleString_Iso3166CountryName2:
            // TODO
            //status = GetLocaleIso3166CountryCode(locale, value, valueLength);
            break;
        case LocaleString_NaNSymbol:
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_NAN_SYMBOL, value, valueLength, NULL);
            break;
        case LocaleString_PositiveInfinitySymbol:
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_INFINITY_SYMBOL, value, valueLength, NULL);
            break;
        case LocaleString_ParentName:
            // TODO
           /* // ICU supports lang[-script][-region][-variant] so up to 4 parents
            // including invariant locale
            char localeNameTemp[ULOC_FULLNAME_CAPACITY];

            uloc_getParent(locale, localeNameTemp, ULOC_FULLNAME_CAPACITY, &status);
            u_charsToUChars_safe(localeNameTemp, value, valueLength, &status);
            if (U_SUCCESS(status))
            {
                FixupLocaleName(value, valueLength);
            }*/
            break;
        case LocaleString_PercentSymbol:
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PERCENT_SYMBOL, value, valueLength, NULL);
            break;
        case LocaleString_PerMilleSymbol:
            // TODO
            //status = GetLocaleInfoDecimalFormatSymbol(locale, UNUM_PERMILL_SYMBOL, value, valueLength, NULL);
            break;
        default:
            status = U_UNSUPPORTED_ERROR;
            break;
    }

    return UErrorCodeToBool(status);
}


