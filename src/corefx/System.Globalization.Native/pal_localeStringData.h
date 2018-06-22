// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include "pal_compiler.h"
#include "pal_locale.h"

// Enum that corresponds to managed enum CultureData.LocaleStringData.
// The numeric values of the enum members match their Win32 counterparts.
typedef enum
{
    LocalizedDisplayName = 0x02,
    EnglishDisplayName = 0x00000072,
    NativeDisplayName = 0x00000073,
    LocalizedLanguageName = 0x0000006f,
    EnglishLanguageName = 0x00001001,
    NativeLanguageName = 0x04,
    EnglishCountryName = 0x00001002,
    NativeCountryName = 0x08,
    ListSeparator = 0x0C,
    DecimalSeparator = 0x0E,
    ThousandSeparator = 0x0F,
    Digits = 0x00000013,
    MonetarySymbol = 0x00000014,
    CurrencyEnglishName = 0x00001007,
    CurrencyNativeName = 0x00001008,
    Iso4217MonetarySymbol = 0x00000015,
    MonetaryDecimalSeparator = 0x00000016,
    MonetaryThousandSeparator = 0x00000017,
    AMDesignator = 0x00000028,
    PMDesignator = 0x00000029,
    PositiveSign = 0x00000050,
    NegativeSign = 0x00000051,
    Iso639LanguageTwoLetterName = 0x00000059,
    Iso639LanguageThreeLetterName = 0x00000067,
    Iso3166CountryName = 0x0000005A,
    Iso3166CountryName2= 0x00000068,
    NaNSymbol = 0x00000069,
    PositiveInfinitySymbol = 0x0000006a,
    ParentName = 0x0000006d,
    PercentSymbol = 0x00000076,
    PerMilleSymbol = 0x00000077
} LocaleStringData;

DLLEXPORT int32_t GlobalizationNative_GetLocaleInfoString(const UChar* localeName,
                                                          LocaleStringData localeStringData,
                                                          UChar* value,
                                                          int32_t valueLength);

DLLEXPORT int32_t GlobalizationNative_GetLocaleTimeFormat(const UChar* localeName,
                                                          int shortFormat, UChar* value,
                                                          int32_t valueLength);

