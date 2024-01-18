// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <assert.h>

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

#ifdef DEBUG
#define assert_err(cond, msg, err) do \
{ \
  if(!(cond)) \
  { \
    fprintf(stderr, "%s (%d): error %d: %s. %s (%s failed)\n", __FILE__, __LINE__, err, msg, strerror(err), #cond); \
    assert(false && "assert_err failed"); \
  } \
} while(0)
#define assert_msg(cond, msg, val) do \
{ \
  if(!(cond)) \
  { \
    fprintf(stderr, "%s (%d): error %d: %s (%s failed)\n", __FILE__, __LINE__, val, msg, #cond); \
    assert(false && "assert_msg failed"); \
  } \
} while(0)
#else // DEBUG
#define assert_err(cond, msg, err)
#define assert_msg(cond, msg, val)
#endif // DEBUG


// Placeholder for calendar data
int32_t GlobalizationNative_GetCalendars(
    const UChar* localeName, CalendarId* calendars, int32_t calendarsCapacity)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

ResultCode GlobalizationNative_GetCalendarInfo(
    const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, UChar* result, int32_t resultCapacity)
{
    assert_msg(false, "Not supported on this platform", 0);
    return UnknownError;
}

int32_t GlobalizationNative_EnumCalendarInfo(
    EnumCalendarInfoCallback callback, const UChar* localeName, CalendarId calendarId, CalendarDataType dataType, const void* context)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_GetLatestJapaneseEra(void)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_GetJapaneseEraStartDate(
    int32_t era, int32_t* startYear, int32_t* startMonth, int32_t* startDay)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

// Placeholder for casing data
void GlobalizationNative_ChangeCase(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    assert_msg(false, "Not supported on this platform", 0);
}

void GlobalizationNative_ChangeCaseInvariant(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    assert_msg(false, "Not supported on this platform", 0);
}

void GlobalizationNative_ChangeCaseTurkish(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    assert_msg(false, "Not supported on this platform", 0);
}

// Placeholder for collation data
ResultCode GlobalizationNative_GetSortHandle(
    const char* lpLocaleName, SortHandle** pSortHandle)
{
    assert_msg(false, "Not supported on this platform", 0);
    return UnknownError;
}

void GlobalizationNative_CloseSortHandle(SortHandle* pSortHandle) 
{
    assert_msg(false, "Not supported on this platform", 0);
}

int32_t GlobalizationNative_GetSortVersion(SortHandle* pSortHandle)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_CompareString(
    SortHandle* pSortHandle, const UChar* lpStr1, int32_t cwStr1Length, const UChar* lpStr2, int32_t cwStr2Length, int32_t options)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_IndexOf(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_LastIndexOf(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_StartsWith(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_EndsWith(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options, int32_t* pMatchedLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_GetSortKey(
    SortHandle* pSortHandle, const UChar* lpStr, int32_t cwStrLength, uint8_t* sortKey, int32_t cbSortKeyLength, int32_t options)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

// Placeholder for locale data
int32_t GlobalizationNative_GetLocales(
    UChar *value, int32_t valueLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_GetLocaleName(
    const UChar* localeName, UChar* value, int32_t valueLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_GetDefaultLocaleName(
    UChar* value, int32_t valueLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_IsPredefinedLocale(
    const UChar* localeName)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_GetLocaleTimeFormat(
    const UChar* localeName, int shortFormat, UChar* value, int32_t valueLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

// Placeholder for locale number data
int32_t GlobalizationNative_GetLocaleInfoInt(
    const UChar* localeName, LocaleNumberData localeNumberData, int32_t* value)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_GetLocaleInfoGroupingSizes(
    const UChar* localeName, LocaleNumberData localeGroupingData, int32_t* primaryGroupSize, int32_t* secondaryGroupSize)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

// Placeholder for icu shim data
int32_t GlobalizationNative_LoadICU(void)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

void GlobalizationNative_InitICUFunctions(
    void* icuuc, void* icuin, const char* version, const char* suffix)
{
    assert_msg(false, "Not supported on this platform", 0);
}

int32_t GlobalizationNative_GetICUVersion(void)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t
GlobalizationNative_LoadICUData(const char* path)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

// Placeholder for locale string data
int32_t GlobalizationNative_GetLocaleInfoString(
    const UChar* localeName, LocaleStringData localeStringData, UChar* value, int32_t valueLength, const UChar* uiLocaleName)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

//Placeholder for normalization data
int32_t GlobalizationNative_IsNormalized(
    NormalizationForm normalizationForm, const UChar* lpStr, int32_t cwStrLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

int32_t GlobalizationNative_NormalizeString(
    NormalizationForm normalizationForm, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

// Placeholder for time zone data
int32_t GlobalizationNative_WindowsIdToIanaId(
    const UChar* windowsId, const char* region, UChar* ianaId, int32_t ianaIdLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}

ResultCode GlobalizationNative_GetTimeZoneDisplayName(
    const UChar* localeName, const UChar* timeZoneId, TimeZoneDisplayNameType type, UChar* result, int32_t resultLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return UnknownError;
}

int32_t GlobalizationNative_IanaIdToWindowsId(
    const UChar* ianaId, UChar* windowsId, int32_t windowsIdLength)
{
    assert_msg(false, "Not supported on this platform", 0);
    return 0;
}
