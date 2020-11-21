// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <string.h>

typedef uint16_t UChar;

// Include System.Globalization.Native headers
#include "pal_calendarData.h"
#include "pal_casing.h"
#include "pal_collation.h"
#include "pal_locale.h"
#include "pal_localeNumberData.h"
#include "pal_localeStringData.h"
#include "pal_icushim.h"
#include "pal_idna.h"
#include "pal_normalization.h"
#include "pal_timeZoneInfo.h"

#ifndef lengthof
#define lengthof(rg)    (int)(sizeof(rg)/sizeof(rg[0]))
#endif

typedef struct
{
    const char* name;
    const void* method;
} Entry;

static Entry s_globalizationNative[] =
{
    {"GlobalizationNative_ChangeCase", (void*)GlobalizationNative_ChangeCase},
    {"GlobalizationNative_ChangeCaseInvariant", (void*)GlobalizationNative_ChangeCaseInvariant},
    {"GlobalizationNative_ChangeCaseTurkish", (void*)GlobalizationNative_ChangeCaseTurkish},
    {"GlobalizationNative_CloseSortHandle", (void*)GlobalizationNative_CloseSortHandle},
    {"GlobalizationNative_CompareString", (void*)GlobalizationNative_CompareString},
    {"GlobalizationNative_EndsWith", (void*)GlobalizationNative_EndsWith},
    {"GlobalizationNative_EnumCalendarInfo", (void*)GlobalizationNative_EnumCalendarInfo},
    {"GlobalizationNative_GetCalendarInfo", (void*)GlobalizationNative_GetCalendarInfo},
    {"GlobalizationNative_GetCalendars", (void*)GlobalizationNative_GetCalendars},
    {"GlobalizationNative_GetDefaultLocaleName", (void*)GlobalizationNative_GetDefaultLocaleName},
    {"GlobalizationNative_GetICUVersion", (void*)GlobalizationNative_GetICUVersion},
    {"GlobalizationNative_GetJapaneseEraStartDate", (void*)GlobalizationNative_GetJapaneseEraStartDate},
    {"GlobalizationNative_GetLatestJapaneseEra", (void*)GlobalizationNative_GetLatestJapaneseEra},
    {"GlobalizationNative_GetLocaleInfoGroupingSizes", (void*)GlobalizationNative_GetLocaleInfoGroupingSizes},
    {"GlobalizationNative_GetLocaleInfoInt", (void*)GlobalizationNative_GetLocaleInfoInt},
    {"GlobalizationNative_GetLocaleInfoString", (void*)GlobalizationNative_GetLocaleInfoString},
    {"GlobalizationNative_GetLocaleName", (void*)GlobalizationNative_GetLocaleName},
    {"GlobalizationNative_GetLocales", (void*)GlobalizationNative_GetLocales},
    {"GlobalizationNative_GetLocaleTimeFormat", (void*)GlobalizationNative_GetLocaleTimeFormat},
    {"GlobalizationNative_GetSortHandle", (void*)GlobalizationNative_GetSortHandle},
    {"GlobalizationNative_GetSortKey", (void*)GlobalizationNative_GetSortKey},
    {"GlobalizationNative_GetSortVersion", (void*)GlobalizationNative_GetSortVersion},
    {"GlobalizationNative_GetTimeZoneDisplayName", (void*)GlobalizationNative_GetTimeZoneDisplayName},
    {"GlobalizationNative_IndexOf", (void*)GlobalizationNative_IndexOf},
    {"GlobalizationNative_InitICUFunctions", (void*)GlobalizationNative_InitICUFunctions},
    {"GlobalizationNative_InitOrdinalCasingPage", (void*)GlobalizationNative_InitOrdinalCasingPage},
    {"GlobalizationNative_IsNormalized", (void*)GlobalizationNative_IsNormalized},
    {"GlobalizationNative_IsPredefinedLocale", (void*)GlobalizationNative_IsPredefinedLocale},
    {"GlobalizationNative_LastIndexOf", (void*)GlobalizationNative_LastIndexOf},
    {"GlobalizationNative_LoadICU", (void*)GlobalizationNative_LoadICU},
    {"GlobalizationNative_NormalizeString", (void*)GlobalizationNative_NormalizeString},
    {"GlobalizationNative_StartsWith", (void*)GlobalizationNative_StartsWith},
    {"GlobalizationNative_ToAscii", (void*)GlobalizationNative_ToAscii},
    {"GlobalizationNative_ToUnicode", (void*)GlobalizationNative_ToUnicode},
};

EXTERN_C const void* GlobalizationResolveDllImport(const char* name);

EXTERN_C const void* GlobalizationResolveDllImport(const char* name)
{
    for (int i = 0; i < lengthof(s_globalizationNative); i++)
    {
        if (strcmp(name, s_globalizationNative[i].name) == 0)
        {
            return s_globalizationNative[i].method;
        }
    }

    return NULL;
}
