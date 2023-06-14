// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>

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

static const Entry s_globalizationNative[] =
{
    DllImportEntry(GlobalizationNative_ChangeCase)
    DllImportEntry(GlobalizationNative_ChangeCaseInvariant)
    DllImportEntry(GlobalizationNative_ChangeCaseTurkish)
    DllImportEntry(GlobalizationNative_CloseSortHandle)
    DllImportEntry(GlobalizationNative_CompareString)
    DllImportEntry(GlobalizationNative_EndsWith)
    DllImportEntry(GlobalizationNative_EnumCalendarInfo)
    DllImportEntry(GlobalizationNative_GetCalendarInfo)
    DllImportEntry(GlobalizationNative_GetCalendars)
    DllImportEntry(GlobalizationNative_GetDefaultLocaleName)
    DllImportEntry(GlobalizationNative_GetICUVersion)
    DllImportEntry(GlobalizationNative_GetJapaneseEraStartDate)
    DllImportEntry(GlobalizationNative_GetLatestJapaneseEra)
    DllImportEntry(GlobalizationNative_GetLocaleInfoGroupingSizes)
    DllImportEntry(GlobalizationNative_GetLocaleInfoInt)
    DllImportEntry(GlobalizationNative_GetLocaleInfoString)
    DllImportEntry(GlobalizationNative_GetLocaleName)
    DllImportEntry(GlobalizationNative_GetLocales)
    DllImportEntry(GlobalizationNative_GetLocaleTimeFormat)
    DllImportEntry(GlobalizationNative_GetSortHandle)
    DllImportEntry(GlobalizationNative_GetSortKey)
    DllImportEntry(GlobalizationNative_GetSortVersion)
    DllImportEntry(GlobalizationNative_GetTimeZoneDisplayName)
    DllImportEntry(GlobalizationNative_IanaIdToWindowsId)
    DllImportEntry(GlobalizationNative_IndexOf)
    DllImportEntry(GlobalizationNative_InitICUFunctions)
    DllImportEntry(GlobalizationNative_InitOrdinalCasingPage)
    DllImportEntry(GlobalizationNative_IsNormalized)
    DllImportEntry(GlobalizationNative_IsPredefinedLocale)
    DllImportEntry(GlobalizationNative_LastIndexOf)
    DllImportEntry(GlobalizationNative_LoadICU)
#if defined(STATIC_ICU)
    DllImportEntry(GlobalizationNative_LoadICUData)
#endif
    DllImportEntry(GlobalizationNative_NormalizeString)
    DllImportEntry(GlobalizationNative_StartsWith)
    DllImportEntry(GlobalizationNative_ToAscii)
    DllImportEntry(GlobalizationNative_ToUnicode)
    DllImportEntry(GlobalizationNative_WindowsIdToIanaId)
#ifdef __APPLE__
    DllImportEntry(GlobalizationNative_CompareStringNative)
    DllImportEntry(GlobalizationNative_GetLocaleNameNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoStringNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoIntNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative)
    DllImportEntry(GlobalizationNative_GetLocaleTimeFormatNative)
#endif
};

EXTERN_C const void* GlobalizationResolveDllImport(const char* name);

EXTERN_C const void* GlobalizationResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_globalizationNative, ARRAY_SIZE(s_globalizationNative), name);
}
