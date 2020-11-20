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
#define lengthof(rg)    (sizeof(rg)/sizeof(rg[0]))
#endif

typedef struct
{
    const char* name;
    const void* method;
} Entry;

static Entry s_globalizationNative[] =
{
    {"GlobalizationNative_ChangeCase", GlobalizationNative_ChangeCase},
    {"GlobalizationNative_ChangeCaseInvariant", GlobalizationNative_ChangeCaseInvariant},
    {"GlobalizationNative_ChangeCaseTurkish", GlobalizationNative_ChangeCaseTurkish},
    {"GlobalizationNative_CloseSortHandle", GlobalizationNative_CloseSortHandle},
    {"GlobalizationNative_CompareString", GlobalizationNative_CompareString},
    {"GlobalizationNative_EndsWith", GlobalizationNative_EndsWith},
    {"GlobalizationNative_EnumCalendarInfo", GlobalizationNative_EnumCalendarInfo},
    {"GlobalizationNative_GetCalendarInfo", GlobalizationNative_GetCalendarInfo},
    {"GlobalizationNative_GetCalendars", GlobalizationNative_GetCalendars},
    {"GlobalizationNative_GetDefaultLocaleName", GlobalizationNative_GetDefaultLocaleName},
    {"GlobalizationNative_GetICUVersion", GlobalizationNative_GetICUVersion},
    {"GlobalizationNative_GetJapaneseEraStartDate", GlobalizationNative_GetJapaneseEraStartDate},
    {"GlobalizationNative_GetLatestJapaneseEra", GlobalizationNative_GetLatestJapaneseEra},
    {"GlobalizationNative_GetLocaleInfoGroupingSizes", GlobalizationNative_GetLocaleInfoGroupingSizes},
    {"GlobalizationNative_GetLocaleInfoInt", GlobalizationNative_GetLocaleInfoInt},
    {"GlobalizationNative_GetLocaleInfoString", GlobalizationNative_GetLocaleInfoString},
    {"GlobalizationNative_GetLocaleName", GlobalizationNative_GetLocaleName},
    {"GlobalizationNative_GetLocales", GlobalizationNative_GetLocales},
    {"GlobalizationNative_GetLocaleTimeFormat", GlobalizationNative_GetLocaleTimeFormat},
    {"GlobalizationNative_GetSortHandle", GlobalizationNative_GetSortHandle},
    {"GlobalizationNative_GetSortKey", GlobalizationNative_GetSortKey},
    {"GlobalizationNative_GetSortVersion", GlobalizationNative_GetSortVersion},
    {"GlobalizationNative_GetTimeZoneDisplayName", GlobalizationNative_GetTimeZoneDisplayName},
    {"GlobalizationNative_IndexOf", GlobalizationNative_IndexOf},
    {"GlobalizationNative_InitICUFunctions", GlobalizationNative_InitICUFunctions},
    {"GlobalizationNative_InitOrdinalCasingPage", GlobalizationNative_InitOrdinalCasingPage},
    {"GlobalizationNative_IsNormalized", GlobalizationNative_IsNormalized},
    {"GlobalizationNative_IsPredefinedLocale", GlobalizationNative_IsPredefinedLocale},
    {"GlobalizationNative_LastIndexOf", GlobalizationNative_LastIndexOf},
    {"GlobalizationNative_LoadICU", GlobalizationNative_LoadICU},
    {"GlobalizationNative_NormalizeString", GlobalizationNative_NormalizeString},
    {"GlobalizationNative_StartsWith", GlobalizationNative_StartsWith},
    {"GlobalizationNative_ToAscii", GlobalizationNative_ToAscii},
    {"GlobalizationNative_ToUnicode", GlobalizationNative_ToUnicode},
};

extern "C" const void* GlobalizationResolveDllImport(const char* name)
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
