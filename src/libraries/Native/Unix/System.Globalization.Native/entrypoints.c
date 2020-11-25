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
#define lengthof(rg) (sizeof(rg)/sizeof(rg[0]))
#endif

typedef struct
{
    const char* name;
    const void* method;
} Entry;

// expands to:      {"impl", (void*)impl},
#define OverrideEntry(impl) \
    {#impl, (void*)impl},

static const Entry s_globalizationNative[] =
{
    OverrideEntry(GlobalizationNative_ChangeCase)
    OverrideEntry(GlobalizationNative_ChangeCaseInvariant)
    OverrideEntry(GlobalizationNative_ChangeCaseTurkish)
    OverrideEntry(GlobalizationNative_CloseSortHandle)
    OverrideEntry(GlobalizationNative_CompareString)
    OverrideEntry(GlobalizationNative_EndsWith)
    OverrideEntry(GlobalizationNative_EnumCalendarInfo)
    OverrideEntry(GlobalizationNative_GetCalendarInfo)
    OverrideEntry(GlobalizationNative_GetCalendars)
    OverrideEntry(GlobalizationNative_GetDefaultLocaleName)
    OverrideEntry(GlobalizationNative_GetICUVersion)
    OverrideEntry(GlobalizationNative_GetJapaneseEraStartDate)
    OverrideEntry(GlobalizationNative_GetLatestJapaneseEra)
    OverrideEntry(GlobalizationNative_GetLocaleInfoGroupingSizes)
    OverrideEntry(GlobalizationNative_GetLocaleInfoInt)
    OverrideEntry(GlobalizationNative_GetLocaleInfoString)
    OverrideEntry(GlobalizationNative_GetLocaleName)
    OverrideEntry(GlobalizationNative_GetLocales)
    OverrideEntry(GlobalizationNative_GetLocaleTimeFormat)
    OverrideEntry(GlobalizationNative_GetSortHandle)
    OverrideEntry(GlobalizationNative_GetSortKey)
    OverrideEntry(GlobalizationNative_GetSortVersion)
    OverrideEntry(GlobalizationNative_GetTimeZoneDisplayName)
    OverrideEntry(GlobalizationNative_IndexOf)
    OverrideEntry(GlobalizationNative_InitICUFunctions)
    OverrideEntry(GlobalizationNative_InitOrdinalCasingPage)
    OverrideEntry(GlobalizationNative_IsNormalized)
    OverrideEntry(GlobalizationNative_IsPredefinedLocale)
    OverrideEntry(GlobalizationNative_LastIndexOf)
    OverrideEntry(GlobalizationNative_LoadICU)
    OverrideEntry(GlobalizationNative_NormalizeString)
    OverrideEntry(GlobalizationNative_StartsWith)
    OverrideEntry(GlobalizationNative_ToAscii)
    OverrideEntry(GlobalizationNative_ToUnicode)
};

EXTERN_C const void* GlobalizationResolveDllImport(const char* name);

EXTERN_C const void* GlobalizationResolveDllImport(const char* name)
{
    for (size_t i = 0; i < lengthof(s_globalizationNative); i++)
    {
        if (strcmp(name, s_globalizationNative[i].name) == 0)
        {
            return s_globalizationNative[i].method;
        }
    }

    return NULL;
}
