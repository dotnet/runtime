// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include "pal_compiler.h"
#include "pal_locale.h"

// Enum that corresponds to managed enum CultureData.LocaleNumberData.
// The numeric values of the enum members match their Win32 counterparts.
typedef enum
{
    LanguageId = 0x01,
    MeasurementSystem = 0x0D,
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
} LocaleNumberData;

// Enum that corresponds to managed enum System.Globalization.CalendarWeekRule
typedef enum
{
    FirstDay = 0,
    FirstFullWeek = 1,
    FirstFourDayWeek = 2
} CalendarWeekRule;

DLLEXPORT int32_t GlobalizationNative_GetLocaleInfoInt(const UChar* localeName,
                                                       LocaleNumberData localeNumberData,
                                                       int32_t* value);

DLLEXPORT int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(const UChar* localeName,
                                                                 LocaleNumberData localeGroupingData,
                                                                 int32_t* primaryGroupSize,
                                                                 int32_t* secondaryGroupSize);
