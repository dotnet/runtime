// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"

// Enum that corresponds to managed enum CultureData.LocaleStringData.
// The numeric values of the enum members match their Win32 counterparts.
typedef enum
{
    LocaleString_LocalizedDisplayName = 0x02,
    LocaleString_EnglishDisplayName = 0x00000072,
    LocaleString_NativeDisplayName = 0x00000073,
    LocaleString_LocalizedLanguageName = 0x0000006f,
    LocaleString_EnglishLanguageName = 0x00001001,
    LocaleString_NativeLanguageName = 0x04,
    LocaleString_EnglishCountryName = 0x00001002,
    LocaleString_NativeCountryName = 0x08,
    LocaleString_DecimalSeparator = 0x0E,
    LocaleString_ThousandSeparator = 0x0F,
    LocaleString_Digits = 0x00000013,
    LocaleString_MonetarySymbol = 0x00000014,
    LocaleString_CurrencyEnglishName = 0x00001007,
    LocaleString_CurrencyNativeName = 0x00001008,
    LocaleString_Iso4217MonetarySymbol = 0x00000015,
    LocaleString_MonetaryDecimalSeparator = 0x00000016,
    LocaleString_MonetaryThousandSeparator = 0x00000017,
    LocaleString_AMDesignator = 0x00000028,
    LocaleString_PMDesignator = 0x00000029,
    LocaleString_PositiveSign = 0x00000050,
    LocaleString_NegativeSign = 0x00000051,
    LocaleString_Iso639LanguageTwoLetterName = 0x00000059,
    LocaleString_Iso639LanguageThreeLetterName = 0x00000067,
    LocaleString_Iso3166CountryName = 0x0000005A,
    LocaleString_Iso3166CountryName2= 0x00000068,
    LocaleString_NaNSymbol = 0x00000069,
    LocaleString_PositiveInfinitySymbol = 0x0000006a,
    LocaleString_NegativeInfinitySymbol = 0x0000006b,
    LocaleString_ParentName = 0x0000006d,
    LocaleString_PercentSymbol = 0x00000076,
    LocaleString_PerMilleSymbol = 0x00000077
} LocaleStringData;
PALEXPORT int32_t GlobalizationNative_GetLocaleInfoString(const UChar* localeName,
                                                          LocaleStringData localeStringData,
                                                          UChar* value,
                                                          int32_t valueLength,
                                                          const UChar* uiLocaleName);
#if defined(APPLE_HYBRID_GLOBALIZATION)
PALEXPORT const char* GlobalizationNative_GetLocaleInfoStringNative(const char* localeName, LocaleStringData localeStringData, const char* currentUILocaleName);
#endif  

