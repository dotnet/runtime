// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __LIB_NATIVE_ENTRYPOINTS
#define __LIB_NATIVE_ENTRYPOINTS
#endif

#ifdef FEATURE_GLOBALIZATION
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
#endif // FEATURE_GLOBALIZATION

#ifndef BYTE
typedef unsigned char BYTE;
#endif

#ifndef LPVOID
typedef void *LPVOID;
#endif

#define FCFuncStart(name) extern const LPVOID name[]; const LPVOID name[] = {
#define FCFuncEnd() (BYTE*)0x01 /* FCFuncFlag_EndOfArray */ };

#define QCFuncElement(name,impl) \
    (BYTE*)0x8 /* FCFuncFlag_QCall */, (LPVOID)(impl), (LPVOID)name,

#if defined(__cplusplus)
extern "C" {
#endif // defined(__cplusplus)

#ifdef FEATURE_GLOBALIZATION
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
    QCFuncElement("IsNormalized", GlobalizationNative_IsNormalized)
    QCFuncElement("IsPredefinedLocale", GlobalizationNative_IsPredefinedLocale)
    QCFuncElement("LastIndexOf", GlobalizationNative_LastIndexOf)
    QCFuncElement("LoadICU", GlobalizationNative_LoadICU)
    QCFuncElement("NormalizeString", GlobalizationNative_NormalizeString)
    QCFuncElement("StartsWith", GlobalizationNative_StartsWith)
    QCFuncElement("ToAscii", GlobalizationNative_ToAscii)
    QCFuncElement("ToUnicode", GlobalizationNative_ToUnicode)
FCFuncEnd()
#endif // FEATURE_GLOBALIZATION

#if defined(__cplusplus)
}
#endif // defined(__cplusplus)
