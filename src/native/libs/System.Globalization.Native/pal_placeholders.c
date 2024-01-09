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

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wunused-parameter"
#pragma clang diagnostic ignored "-Wunused-command-line-argument"
// Placeholder for calendar data
int32_t GlobalizationNative_GetCalendars(
    const UChar* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
    // Use parameters as unused to avoid compiler warnings
    (void)localeName;
    (void)calendars;
    (void)calendarsCapacity;
    return 1;
}

ResultCode GlobalizationNative_GetCalendarInfo(
    const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, UChar* result, int32_t resultCapacity)
{
    (void)localeName;
    (void)calendarId;
    (void)dataType;
    (void)result;
    (void)resultCapacity;
    return Success;
}

int32_t GlobalizationNative_EnumCalendarInfo(
    EnumCalendarInfoCallback callback, const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, const void* context)
{
    (void)callback;
    (void)localeName;
    (void)calendarId;
    (void)dataType;
    (void)context;
    return 1;
}

int32_t GlobalizationNative_GetLatestJapaneseEra(void)
{
    return 1;
}

int32_t GlobalizationNative_GetJapaneseEraStartDate(
    int32_t era, int32_t* startYear, int32_t* startMonth, int32_t* startDay)
{
    (void)era;
    (void)startYear;
    (void)startMonth;
    (void)startDay;
    return 1;
}

// Placeholder for casing data
void GlobalizationNative_ChangeCase(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    (void)lpSrc;
    (void)cwSrcLength;
    (void)lpDst;
    (void)cwDstLength;
    (void)bToUpper;
}

void GlobalizationNative_ChangeCaseInvariant(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    (void)lpSrc;
    (void)cwSrcLength;
    (void)lpDst;
    (void)cwDstLength;
    (void)bToUpper;
}

void GlobalizationNative_ChangeCaseTurkish(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    (void)lpSrc;
    (void)cwSrcLength;
    (void)lpDst;
    (void)cwDstLength;
    (void)bToUpper;
}

// Placeholder for collation data
ResultCode GlobalizationNative_GetSortHandle(
    const char* lpLocaleName, SortHandle** pSortHandle)
{
    (void)lpLocaleName;
    (void)pSortHandle;
    return Success;
}

void GlobalizationNative_CloseSortHandle(SortHandle* pSortHandle)
{
    (void)pSortHandle;
}

int32_t GlobalizationNative_GetSortVersion(SortHandle* pSortHandle)
{
    (void)pSortHandle;
    return 1;
}

int32_t GlobalizationNative_CompareString(
    SortHandle* pSortHandle, const UChar* lpStr1, int32_t cwStr1Length, const UChar* lpStr2, int32_t cwStr2Length, int32_t options)
{
    (void)pSortHandle;
    (void)lpStr1;
    (void)cwStr1Length;
    (void)lpStr2;
    (void)cwStr2Length;
    (void)options;
    return 1;
}

int32_t GlobalizationNative_IndexOf(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    (void)pSortHandle;
    (void)lpTarget;
    (void)cwTargetLength;
    (void)lpSource;
    (void)cwSourceLength;
    (void)options;
    (void)pMatchedLength;
    return 1;
}

int32_t GlobalizationNative_LastIndexOf(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    (void)pSortHandle;
    (void)lpTarget;
    (void)cwTargetLength;
    (void)lpSource;
    (void)cwSourceLength;
    (void)options;
    (void)pMatchedLength;
    return 1;
}

int32_t GlobalizationNative_StartsWith(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    (void)pSortHandle;
    (void)lpTarget;
    (void)cwTargetLength;
    (void)lpSource;
    (void)cwSourceLength;
    (void)options;
    (void)pMatchedLength;
    return 1;
}

int32_t GlobalizationNative_EndsWith(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    (void)pSortHandle;
    (void)lpTarget;
    (void)cwTargetLength;
    (void)lpSource;
    (void)cwSourceLength;
    (void)options;
    (void)pMatchedLength;
    return 1;
}

int32_t GlobalizationNative_GetSortKey(
    SortHandle* pSortHandle, const UChar* lpStr, int32_t cwStrLength, uint8_t* sortKey, int32_t cbSortKeyLength, int32_t options)
{
    (void)pSortHandle;
    (void)lpStr;
    (void)cwStrLength;
    (void)sortKey;
    (void)cbSortKeyLength;
    (void)options;
    return 1;
}

// Placeholder for locale data
int32_t GlobalizationNative_GetLocales(
    UChar *value, int32_t valueLength)
{
    (void)value;
    (void)valueLength;
    return 1;
}

int32_t GlobalizationNative_GetLocaleName(
    const UChar* localeName, UChar* value, int32_t valueLength)
{
    (void)localeName;
    (void)value;
    (void)valueLength;
    return 1;
}

int32_t GlobalizationNative_GetDefaultLocaleName(
    UChar* value, int32_t valueLength)
{
    (void)value;
    (void)valueLength;
    return 1;
}

int32_t GlobalizationNative_IsPredefinedLocale(
    const UChar* localeName)
{
    (void)localeName;
    return 1;
}

int32_t GlobalizationNative_GetLocaleTimeFormat(
    const UChar* localeName, int shortFormat, UChar* value, int32_t valueLength)
{
    (void)localeName;
    (void)shortFormat;
    (void)value;
    (void)valueLength;
    return 1;
}

// Placeholder for locale number data
int32_t GlobalizationNative_GetLocaleInfoInt(
    const UChar* localeName, LocaleNumberData localeNumberData, int32_t* value)
{
    (void)localeName;
    (void)localeNumberData;
    (void)value;
    return 1;
}

int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(
    const UChar* localeName, LocaleNumberData localeGroupingData, int32_t* primaryGroupSize, int32_t* secondaryGroupSize)
{
    (void)localeName;
    (void)localeGroupingData;
    (void)primaryGroupSize;
    (void)secondaryGroupSize;
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
    (void)icuuc;
    (void)icuin;
    (void)version;
    (void)suffix;
}

int32_t GlobalizationNative_GetICUVersion(void)
{
    return 1;
}

int32_t
GlobalizationNative_LoadICUData(const char* path)
{
    (void)path;
    return 1;
}

// Placeholder for locale string data
int32_t GlobalizationNative_GetLocaleInfoString(
    const UChar* localeName, LocaleStringData localeStringData, UChar* value, int32_t valueLength, const UChar* uiLocaleName)
{
    (void)localeName;
    (void)localeStringData;
    (void)value;
    (void)valueLength;
    (void)uiLocaleName;
    return 1;
}

//Placeholder for normalization data
int32_t GlobalizationNative_IsNormalized(
    NormalizationForm normalizationForm, const UChar* lpStr, int32_t cwStrLength)
{
    (void)normalizationForm;
    (void)lpStr;
    (void)cwStrLength;
    return 1;
}

int32_t GlobalizationNative_NormalizeString(
    NormalizationForm normalizationForm, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    (void)normalizationForm;
    (void)lpSrc;
    (void)cwSrcLength;
    (void)lpDst;
    (void)cwDstLength;
    return 1;
}

// Placeholder for time zone data
int32_t GlobalizationNative_WindowsIdToIanaId(
    const UChar* windowsId, const char* region, UChar* ianaId, int32_t ianaIdLength)
{
    (void)windowsId;
    (void)region;
    (void)ianaId;
    (void)ianaIdLength;
    return 1;
}

ResultCode GlobalizationNative_GetTimeZoneDisplayName(
    const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength)
{
    (void)localeName;
    (void)timeZoneId;
    (void)type;
    (void)result;
    (void)resultLength;
    return Success;
}

int32_t GlobalizationNative_IanaIdToWindowsId(
    const UChar* ianaId, UChar* windowsId, int32_t windowsIdLength)
{
    (void)ianaId;
    (void)windowsId;
    (void)windowsIdLength;
    return 1;
}
#pragma clang diagnostic pop
