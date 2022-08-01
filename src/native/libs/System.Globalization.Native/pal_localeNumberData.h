// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"

// Enum that corresponds to managed enum CultureData.LocaleNumberData.
// The numeric values of the enum members match their Win32 counterparts.
typedef enum
{
    LocaleNumber_LanguageId = 0x01,
    LocaleNumber_MeasurementSystem = 0x0D,
    LocaleNumber_FractionalDigitsCount = 0x00000011,
    LocaleNumber_NegativeNumberFormat = 0x00001010,
    LocaleNumber_MonetaryFractionalDigitsCount = 0x00000019,
    LocaleNumber_PositiveMonetaryNumberFormat = 0x0000001B,
    LocaleNumber_NegativeMonetaryNumberFormat = 0x0000001C,
    LocaleNumber_FirstDayofWeek = 0x0000100C,
    LocaleNumber_FirstWeekOfYear = 0x0000100D,
    LocaleNumber_ReadingLayout = 0x00000070,
    LocaleNumber_NegativePercentFormat = 0x00000074,
    LocaleNumber_PositivePercentFormat = 0x00000075,
    LocaleNumber_Digit = 0x00000010,
    LocaleNumber_Monetary = 0x00000018
} LocaleNumberData;

// Enum that corresponds to managed enum System.Globalization.CalendarWeekRule
typedef enum
{
    WeekRule_FirstDay = 0,
    WeekRule_FirstFullWeek = 1,
    WeekRule_FirstFourDayWeek = 2
} CalendarWeekRule;

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoInt(const UChar* localeName,
                                                       LocaleNumberData localeNumberData,
                                                       int32_t* value);

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(const UChar* localeName,
                                                                 LocaleNumberData localeGroupingData,
                                                                 int32_t* primaryGroupSize,
                                                                 int32_t* secondaryGroupSize);
