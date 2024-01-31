// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdlib.h>

#include "pal_common.h"
#include "pal_locale_hg.h"
#include "pal_compiler.h"
#include "pal_errors.h"

// the function pointer definition for the callback used in EnumCalendarInfo
typedef void (PAL_CALLBACK_CALLTYPE *EnumCalendarInfoCallback)(const UChar*, const void*);

PALEXPORT int32_t GlobalizationNative_GetCalendars(const UChar* localeName,
                                                   CalendarId* calendars,
                                                   int32_t calendarsCapacity);

PALEXPORT ResultCode GlobalizationNative_GetCalendarInfo(const UChar* localeName,
                                                         CalendarId calendarId,
                                                         CalendarDataType dataType,
                                                         UChar* result,
                                                         int32_t resultCapacity);

PALEXPORT int32_t GlobalizationNative_EnumCalendarInfo(EnumCalendarInfoCallback callback,
                                                       const UChar* localeName,
                                                       CalendarId calendarId,
                                                       CalendarDataType dataType,
                                                       const void* context);

PALEXPORT int32_t GlobalizationNative_GetLatestJapaneseEra(void);

PALEXPORT int32_t GlobalizationNative_GetJapaneseEraStartDate(int32_t era,
                                                              int32_t* startYear,
                                                              int32_t* startMonth,
                                                              int32_t* startDay);
#if defined(APPLE_HYBRID_GLOBALIZATION)
PALEXPORT const char* GlobalizationNative_GetCalendarInfoNative(const char* localeName,
                                                                CalendarId calendarId,
                                                                CalendarDataType dataType);

PALEXPORT int32_t GlobalizationNative_GetCalendarsNative(const char* localeName,
                                                         CalendarId* calendars,
                                                         int32_t calendarsCapacity);

PALEXPORT int32_t GlobalizationNative_GetLatestJapaneseEraNative(void);

PALEXPORT int32_t GlobalizationNative_GetJapaneseEraStartDateNative(int32_t era,
                                                                    int32_t* startYear,
                                                                    int32_t* startMonth,
                                                                    int32_t* startDay);
#endif
