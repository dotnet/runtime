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

#define FCFuncStart(name) EXTERN_C const void* name[]; const void* name[] = {
#define FCFuncEnd() (void*)0x01 /* FCFuncFlag_EndOfArray */ };

#define QCFuncElement(name,impl) \
    (void*)0x8 /* FCFuncFlag_QCall */, (void*)(impl), (void*)name,


FCFuncStart(gPalGlobalizationNative)
    QCFuncElement("ChangeCase", GlobalizationNative_ChangeCase)
    QCFuncElement("ChangeCaseInvariant", GlobalizationNative_ChangeCaseInvariant)
    QCFuncElement("ChangeCaseTurkish", GlobalizationNative_ChangeCaseTurkish)
    QCFuncElement("CloseSortHandle", GlobalizationNative_CloseSortHandle)
    QCFuncElement("CompareString", GlobalizationNative_CompareString)
    QCFuncElement("EndsWith", GlobalizationNative_EndsWith)
    QCFuncElement("EnumCalendarInfo", GlobalizationNative_EnumCalendarInfo)
    QCFuncElement("GetCalendarInfo", GlobalizationNative_GetCalendarInfo)
    QCFuncElement("GetCalendars", GlobalizationNative_GetCalendars)
    QCFuncElement("GetDefaultLocaleName", GlobalizationNative_GetDefaultLocaleName)
    QCFuncElement("GetICUVersion", GlobalizationNative_GetICUVersion)
    QCFuncElement("GetJapaneseEraStartDate", GlobalizationNative_GetJapaneseEraStartDate)
    QCFuncElement("GetLatestJapaneseEra", GlobalizationNative_GetLatestJapaneseEra)
    QCFuncElement("GetLocaleInfoGroupingSizes", GlobalizationNative_GetLocaleInfoGroupingSizes)
    QCFuncElement("GetLocaleInfoInt", GlobalizationNative_GetLocaleInfoInt)
    QCFuncElement("GetLocaleInfoString", GlobalizationNative_GetLocaleInfoString)
    QCFuncElement("GetLocaleName", GlobalizationNative_GetLocaleName)
    QCFuncElement("GetLocales", GlobalizationNative_GetLocales)
    QCFuncElement("GetLocaleTimeFormat", GlobalizationNative_GetLocaleTimeFormat)
    QCFuncElement("GetSortHandle", GlobalizationNative_GetSortHandle)
    QCFuncElement("GetSortKey", GlobalizationNative_GetSortKey)
    QCFuncElement("GetSortVersion", GlobalizationNative_GetSortVersion)
    QCFuncElement("GetTimeZoneDisplayName", GlobalizationNative_GetTimeZoneDisplayName)
    QCFuncElement("IndexOf", GlobalizationNative_IndexOf)
    QCFuncElement("InitICUFunctions", GlobalizationNative_InitICUFunctions)
    QCFuncElement("InitOrdinalCasingPage", GlobalizationNative_InitOrdinalCasingPage)
    QCFuncElement("IsNormalized", GlobalizationNative_IsNormalized)
    QCFuncElement("IsPredefinedLocale", GlobalizationNative_IsPredefinedLocale)
    QCFuncElement("LastIndexOf", GlobalizationNative_LastIndexOf)
    QCFuncElement("LoadICU", GlobalizationNative_LoadICU)
    QCFuncElement("NormalizeString", GlobalizationNative_NormalizeString)
    QCFuncElement("StartsWith", GlobalizationNative_StartsWith)
    QCFuncElement("ToAscii", GlobalizationNative_ToAscii)
    QCFuncElement("ToUnicode", GlobalizationNative_ToUnicode)
FCFuncEnd()

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
