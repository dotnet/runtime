// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdlib.h>

#include "pal_locale_hg.h"
#include "pal_compiler.h"
#include "pal_errors.h"

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

