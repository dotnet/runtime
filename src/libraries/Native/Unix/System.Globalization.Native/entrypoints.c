// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdint.h>

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
    QCFuncElement("CompareStringOrdinalIgnoreCase", GlobalizationNative_CompareStringOrdinalIgnoreCase)
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
    QCFuncElement("IndexOfOrdinalIgnoreCase", GlobalizationNative_IndexOfOrdinalIgnoreCase)
    QCFuncElement("InitICUFunctions", GlobalizationNative_InitICUFunctions)
    QCFuncElement("IsNormalized", GlobalizationNative_IsNormalized)
    QCFuncElement("IsPredefinedLocale", GlobalizationNative_IsPredefinedLocale)
    QCFuncElement("LastIndexOf", GlobalizationNative_LastIndexOf)
    QCFuncElement("LoadICU", GlobalizationNative_LoadICU)
    QCFuncElement("NormalizeString", GlobalizationNative_NormalizeString)
    QCFuncElement("StartsWith", GlobalizationNative_StartsWith)
    QCFuncElement("ToAscii", GlobalizationNative_ToAscii)
    QCFuncElement("ToUnicode", GlobalizationNative_ToUnicode)
FCFuncEnd()
