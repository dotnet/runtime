// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_icushim_internal.h"
#include "pal_icushim.h"
#include "pal_calendarData.h"
#include "pal_casing.h"
#include "pal_collation.h"
#include "pal_locale.h"
#include "pal_localeNumberData.h"
#include "pal_localeStringData.h"
#include "pal_normalization.h"
#include "pal_timeZoneInfo.h"

// Placeholder for calendar data
int32_t GlobalizationNative_GetCalendars(
    const UChar* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
    return 1;
}

ResultCode GlobalizationNative_GetCalendarInfo(
    const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, UChar* result, int32_t resultCapacity)
{
    return Success;
}

int32_t GlobalizationNative_EnumCalendarInfo(
    EnumCalendarInfoCallback callback, const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, const void* context)
{
    return 1;
}

int32_t GlobalizationNative_GetLatestJapaneseEra(void)
{
    return 1;
}

int32_t GlobalizationNative_GetJapaneseEraStartDate(
    int32_t era, int32_t* startYear, int32_t* startMonth, int32_t* startDay)
{
    return 1;
}

// Placeholder for casing data
void GlobalizationNative_ChangeCase(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
}

void GlobalizationNative_ChangeCaseInvariant(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
}

void GlobalizationNative_ChangeCaseTurkish(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
}

// Placeholder for collation data
ResultCode GlobalizationNative_GetSortHandle(
    const char* lpLocaleName, SortHandle** pSortHandle)
{
    return Success;
}

void GlobalizationNative_CloseSortHandle(SortHandle* pSortHandle) 
{
}

int32_t GlobalizationNative_GetSortVersion(SortHandle* pSortHandle)
{
    return 1;
}

int32_t GlobalizationNative_CompareString(
    SortHandle* pSortHandle, const UChar* lpStr1, int32_t cwStr1Length, const UChar* lpStr2, int32_t cwStr2Length, int32_t options)
{
    return 1;
}

int32_t GlobalizationNative_IndexOf(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    return 1;
}

int32_t GlobalizationNative_LastIndexOf(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    return 1;
}

int32_t GlobalizationNative_StartsWith(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    return 1;
}

int32_t GlobalizationNative_EndsWith(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    return 1;
}

int32_t GlobalizationNative_GetSortKey(
    SortHandle* pSortHandle, const UChar* lpStr, int32_t cwStrLength, uint8_t* sortKey, int32_t cbSortKeyLength, int32_t options)
{
    return 1;
}

// Placeholder for locale data
int32_t GlobalizationNative_GetLocales(
    UChar *value, int32_t valueLength)
{
    return 1;
}

int32_t GlobalizationNative_GetLocaleName(
    const UChar* localeName, UChar* value, int32_t valueLength)
{
    return 1;
}

int32_t GlobalizationNative_GetDefaultLocaleName(
    UChar* value, int32_t valueLength)
{
    return 1;
}

int32_t GlobalizationNative_IsPredefinedLocale(
    const UChar* localeName)
{
    return 1;
}

int32_t GlobalizationNative_GetLocaleTimeFormat(
    const UChar* localeName, int shortFormat, UChar* value, int32_t valueLength)
{
    return 1;
}

// Placeholder for locale number data
int32_t GlobalizationNative_GetLocaleInfoInt(
    const UChar* localeName, LocaleNumberData localeNumberData, int32_t* value)
{
    return 1;
}

int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(
    const UChar* localeName, LocaleNumberData localeGroupingData, int32_t* primaryGroupSize, int32_t* secondaryGroupSize)
{
    return 1;
}

// Placeholder for icu shim data
int32_t GlobalizationNative_LoadICU(void)
{
    return 1;
}

void GlobalizationNative_InitICUFunctions(
    void* icuuc, void* icuin, const char* version, const char* suffix)
{
}

int32_t GlobalizationNative_GetICUVersion(void)
{
    return 1;
}

int32_t
GlobalizationNative_LoadICUData(const char* path)
{
    return 1;
}

// Placeholder for locale string data
int32_t GlobalizationNative_GetLocaleInfoString(
    const UChar* localeName, LocaleStringData localeStringData, UChar* value, int32_t valueLength, const UChar* uiLocaleName)
{
    return 1;
}

//Placeholder for normalization data
int32_t GlobalizationNative_IsNormalized(
    NormalizationForm normalizationForm, const UChar* lpStr, int32_t cwStrLength)
{
    return 1;
}

int32_t GlobalizationNative_NormalizeString(
    NormalizationForm normalizationForm, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    return 1;
}

// Placeholder for time zone data
int32_t GlobalizationNative_WindowsIdToIanaId(
    const UChar* windowsId, const char* region, UChar* ianaId, int32_t ianaIdLength)
{
    return 1;
}

ResultCode GlobalizationNative_GetTimeZoneDisplayName(
    const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength)
{
    return Success;
}

int32_t GlobalizationNative_IanaIdToWindowsId(
    const UChar* ianaId, UChar* windowsId, int32_t windowsIdLength)
{
    return 1;
}
