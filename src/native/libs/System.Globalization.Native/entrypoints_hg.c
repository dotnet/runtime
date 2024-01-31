// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/entrypoints.h>

typedef uint16_t UChar;

// Include System.HybridGlobalization.Native headers
#include "pal_calendarData_hg.h"
#include "pal_casing_hg.h"
#include "pal_collation_hg.h"
#include "pal_locale_hg.h"
#include "pal_icushim.h"
#include "pal_idna.h"
#include "pal_normalization_hg.h"
#include "pal_timeZoneInfo_hg.h"

static const Entry s_globalizationNative[] =
{
    DllImportEntry(GlobalizationNative_ChangeCaseInvariantNative)
    DllImportEntry(GlobalizationNative_ChangeCaseNative)
    DllImportEntry(GlobalizationNative_CompareStringNative)
    DllImportEntry(GlobalizationNative_GetDefaultLocaleNameNative)
    DllImportEntry(GlobalizationNative_EndsWithNative)
    DllImportEntry(GlobalizationNative_GetCalendarInfoNative)
    DllImportEntry(GlobalizationNative_GetCalendarsNative)
    DllImportEntry(GlobalizationNative_GetJapaneseEraStartDateNative)
    DllImportEntry(GlobalizationNative_GetLatestJapaneseEraNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoIntNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative)
    DllImportEntry(GlobalizationNative_GetLocaleInfoStringNative)
    DllImportEntry(GlobalizationNative_GetLocaleNameNative)
    DllImportEntry(GlobalizationNative_GetLocalesNative)
    DllImportEntry(GlobalizationNative_GetLocaleTimeFormatNative)
    DllImportEntry(GlobalizationNative_GetSortKeyNative)
    DllImportEntry(GlobalizationNative_GetTimeZoneDisplayNameNative)
    DllImportEntry(GlobalizationNative_IndexOfNative)
    DllImportEntry(GlobalizationNative_IsNormalizedNative)
    DllImportEntry(GlobalizationNative_NormalizeStringNative)
    DllImportEntry(GlobalizationNative_StartsWithNative)
    DllImportEntry(GlobalizationNative_ToAscii)
    DllImportEntry(GlobalizationNative_ToUnicode)
    DllImportEntry(GlobalizationNative_InitOrdinalCasingPage)
};

EXTERN_C const void* HybridGlobalizationResolveDllImport(const char* name);

EXTERN_C const void* HybridGlobalizationResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_globalizationNative, ARRAY_SIZE(s_globalizationNative), name);
}
