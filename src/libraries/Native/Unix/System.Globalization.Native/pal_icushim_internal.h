// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// Enable calling ICU functions through shims to enable support for
// multiple versions of ICU.

#pragma once

#if defined(TARGET_UNIX)

#include "config.h"

#if defined(TARGET_ANDROID)
#include "pal_icushim_internal_android.h"
#else

#define U_DISABLE_RENAMING 1

// All ICU headers need to be included here so that all function prototypes are
// available before the function pointers are declared below.
#include <unicode/ucurr.h>
#include <unicode/ucal.h>
#include <unicode/uchar.h>
#include <unicode/ucol.h>
#include <unicode/udat.h>
#include <unicode/udata.h>
#include <unicode/udatpg.h>
#include <unicode/uenum.h>
#include <unicode/uidna.h>
#include <unicode/uldnames.h>
#include <unicode/ulocdata.h>
#include <unicode/unorm2.h>
#include <unicode/unum.h>
#include <unicode/ures.h>
#include <unicode/usearch.h>
#include <unicode/utf16.h>
#include <unicode/utypes.h>
#include <unicode/urename.h>
#include <unicode/ustring.h>

#endif

#elif defined(TARGET_WINDOWS)

#include "icu.h"

#define HAVE_SET_MAX_VARIABLE 1
#define UDAT_STANDALONE_SHORTER_WEEKDAYS 1

#endif

#include "pal_compiler.h"

#if !defined(STATIC_ICU)
// ucol_setVariableTop is a deprecated function on the newer ICU versions and ucol_setMaxVariable should be used instead.
// As can run against ICU versions which not supported ucol_setMaxVariable, we'll dynamically try to get the pointer to ucol_setVariableTop
// when we couldn't get a pointer to ucol_setMaxVariable.
typedef uint32_t (*ucol_setVariableTop_func)(UCollator* coll, const UChar* varTop, int32_t len, UErrorCode* status);
extern ucol_setVariableTop_func ucol_setVariableTop_ptr;

#if !defined(TARGET_ANDROID)
// (U_ICU_VERSION_MAJOR_NUM < 52)
// The following APIs are not supported in the ICU versions less than 52. We need to define them manually.
// We have to do runtime check before using the pointers to these APIs. That is why these are listed in the FOR_ALL_OPTIONAL_ICU_FUNCTIONS list.
U_CAPI void U_EXPORT2 ucol_setMaxVariable(UCollator* coll, UColReorderCode group, UErrorCode* pErrorCode);
U_CAPI int32_t U_EXPORT2 ucal_getTimeZoneIDForWindowsID(const UChar* winid, int32_t len, const char* region, UChar* id, int32_t idCapacity, UErrorCode* status);
U_CAPI int32_t U_EXPORT2 ucal_getWindowsTimeZoneID(const UChar* id, int32_t len, UChar* winid, int32_t winidCapacity, UErrorCode* status);
#endif

// List of all functions from the ICU libraries that are used in the System.Globalization.Native.so
#define FOR_ALL_UNCONDITIONAL_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(u_charsToUChars, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_getVersion, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_strcmp, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_strcpy, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_strlen, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_strncpy, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_tolower, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_toupper, libicuuc, true) \
    PER_FUNCTION_BLOCK(u_uastrncpy, libicuuc, true) \
    PER_FUNCTION_BLOCK(ubrk_close, libicuuc, true) \
    PER_FUNCTION_BLOCK(ubrk_openRules, libicuuc, true) \
    PER_FUNCTION_BLOCK(ucal_add, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_close, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_get, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_getAttribute, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_getKeywordValuesForLocale, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_getLimit, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_getNow, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_getTimeZoneDisplayName, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_open, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_openTimeZoneIDEnumeration, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_set, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucal_setMillis, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_close, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_closeElements, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_getOffset, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_getRules, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_getSortKey, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_getStrength, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_getVersion, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_next, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_previous, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_open, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_openElements, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_openRules, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_safeClone, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_setAttribute, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucol_strcoll, libicui18n, true) \
    PER_FUNCTION_BLOCK(udat_close, libicui18n, true) \
    PER_FUNCTION_BLOCK(udat_countSymbols, libicui18n, true) \
    PER_FUNCTION_BLOCK(udat_format, libicui18n, true) \
    PER_FUNCTION_BLOCK(udat_getSymbols, libicui18n, true) \
    PER_FUNCTION_BLOCK(udat_open, libicui18n, true) \
    PER_FUNCTION_BLOCK(udat_setCalendar, libicui18n, true) \
    PER_FUNCTION_BLOCK(udat_toPattern, libicui18n, true) \
    PER_FUNCTION_BLOCK(udatpg_close, libicui18n, true) \
    PER_FUNCTION_BLOCK(udatpg_getBestPattern, libicui18n, true) \
    PER_FUNCTION_BLOCK(udatpg_open, libicui18n, true) \
    PER_FUNCTION_BLOCK(uenum_close, libicuuc, true) \
    PER_FUNCTION_BLOCK(uenum_count, libicuuc, true) \
    PER_FUNCTION_BLOCK(uenum_next, libicuuc, true) \
    PER_FUNCTION_BLOCK(uidna_close, libicuuc, true) \
    PER_FUNCTION_BLOCK(uidna_nameToASCII, libicuuc, true) \
    PER_FUNCTION_BLOCK(uidna_nameToUnicode, libicuuc, true) \
    PER_FUNCTION_BLOCK(uidna_openUTS46, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_canonicalize, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_countAvailable, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getAvailable, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getBaseName, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getCharacterOrientation, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getCountry, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getDefault, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getDisplayCountry, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getDisplayLanguage, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getDisplayName, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getISO3Country, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getISO3Language, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getKeywordValue, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getLanguage, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getLCID, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getName, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_getParent, libicuuc, true) \
    PER_FUNCTION_BLOCK(uloc_setKeywordValue, libicuuc, true) \
    PER_FUNCTION_BLOCK(ulocdata_getCLDRVersion, libicui18n, true) \
    PER_FUNCTION_BLOCK(ulocdata_getMeasurementSystem, libicui18n, true) \
    PER_FUNCTION_BLOCK(unorm2_getNFCInstance, libicuuc, true) \
    PER_FUNCTION_BLOCK(unorm2_getNFDInstance, libicuuc, true) \
    PER_FUNCTION_BLOCK(unorm2_getNFKCInstance, libicuuc, true) \
    PER_FUNCTION_BLOCK(unorm2_getNFKDInstance, libicuuc, true) \
    PER_FUNCTION_BLOCK(unorm2_isNormalized, libicuuc, true) \
    PER_FUNCTION_BLOCK(unorm2_normalize, libicuuc, true) \
    PER_FUNCTION_BLOCK(unum_close, libicui18n, true) \
    PER_FUNCTION_BLOCK(unum_getAttribute, libicui18n, true) \
    PER_FUNCTION_BLOCK(unum_getSymbol, libicui18n, true) \
    PER_FUNCTION_BLOCK(unum_open, libicui18n, true) \
    PER_FUNCTION_BLOCK(unum_toPattern, libicui18n, true) \
    PER_FUNCTION_BLOCK(ures_close, libicuuc, true) \
    PER_FUNCTION_BLOCK(ures_getByKey, libicuuc, true) \
    PER_FUNCTION_BLOCK(ures_getSize, libicuuc, true) \
    PER_FUNCTION_BLOCK(ures_getStringByIndex, libicuuc, true) \
    PER_FUNCTION_BLOCK(ures_open, libicuuc, true) \
    PER_FUNCTION_BLOCK(usearch_close, libicui18n, true) \
    PER_FUNCTION_BLOCK(usearch_first, libicui18n, true) \
    PER_FUNCTION_BLOCK(usearch_getBreakIterator, libicui18n, true) \
    PER_FUNCTION_BLOCK(usearch_getMatchedLength, libicui18n, true) \
    PER_FUNCTION_BLOCK(usearch_last, libicui18n, true) \
    PER_FUNCTION_BLOCK(usearch_openFromCollator, libicui18n, true) \
    PER_FUNCTION_BLOCK(usearch_setPattern, libicui18n, true) \
    PER_FUNCTION_BLOCK(usearch_setText, libicui18n, true)

#if defined(TARGET_WINDOWS)
#define FOR_ALL_OS_CONDITIONAL_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucurr_forLocale, libicuuc, true) \
    PER_FUNCTION_BLOCK(ucurr_getName, libicuuc, true) \
    PER_FUNCTION_BLOCK(uldn_close, libicuuc, true) \
    PER_FUNCTION_BLOCK(uldn_keyValueDisplayName, libicuuc, true) \
    PER_FUNCTION_BLOCK(uldn_open, libicuuc, true)
#else
    // Unix ICU is dynamically resolved at runtime and these APIs in old versions
    // of ICU were in libicui18n
#define FOR_ALL_OS_CONDITIONAL_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucurr_forLocale, libicui18n, true) \
    PER_FUNCTION_BLOCK(ucurr_getName, libicui18n, true) \
    PER_FUNCTION_BLOCK(uldn_close, libicui18n, true) \
    PER_FUNCTION_BLOCK(uldn_keyValueDisplayName, libicui18n, true) \
    PER_FUNCTION_BLOCK(uldn_open, libicui18n, true)
#endif

// The following are the list of the ICU APIs which are optional. If these APIs exist in the ICU version we load at runtime, then we'll use it.
// Otherwise, we'll just not provide the functionality to users which needed these APIs.
#define FOR_ALL_OPTIONAL_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucal_getWindowsTimeZoneID, libicui18n, false) \
    PER_FUNCTION_BLOCK(ucal_getTimeZoneIDForWindowsID, libicui18n, false) \
    PER_FUNCTION_BLOCK(ucol_setMaxVariable, libicui18n, false)

#define FOR_ALL_ICU_FUNCTIONS \
    FOR_ALL_UNCONDITIONAL_ICU_FUNCTIONS \
    FOR_ALL_OPTIONAL_ICU_FUNCTIONS \
    FOR_ALL_OS_CONDITIONAL_ICU_FUNCTIONS

// Declare pointers to all the used ICU functions
#define PER_FUNCTION_BLOCK(fn, lib, required) EXTERN_C TYPEOF(fn)* fn##_ptr;
FOR_ALL_ICU_FUNCTIONS
#undef PER_FUNCTION_BLOCK

// Redefine all calls to ICU functions as calls through pointers that are set
// to the functions of the selected version of ICU in the initialization.
#define u_charsToUChars(...) u_charsToUChars_ptr(__VA_ARGS__)
#define u_getVersion(...) u_getVersion_ptr(__VA_ARGS__)
#define u_strcmp(...) u_strcmp_ptr(__VA_ARGS__)
#define u_strcpy(...) u_strcpy_ptr(__VA_ARGS__)
#define u_strlen(...) u_strlen_ptr(__VA_ARGS__)
#define u_strncpy(...) u_strncpy_ptr(__VA_ARGS__)
#define u_tolower(...) u_tolower_ptr(__VA_ARGS__)
#define u_toupper(...) u_toupper_ptr(__VA_ARGS__)
#define u_uastrncpy(...) u_uastrncpy_ptr(__VA_ARGS__)
#define ubrk_close(...) ubrk_close_ptr(__VA_ARGS__)
#define ubrk_openRules(...) ubrk_openRules_ptr(__VA_ARGS__)
#define ucal_add(...) ucal_add_ptr(__VA_ARGS__)
#define ucal_close(...) ucal_close_ptr(__VA_ARGS__)
#define ucal_get(...) ucal_get_ptr(__VA_ARGS__)
#define ucal_getAttribute(...) ucal_getAttribute_ptr(__VA_ARGS__)
#define ucal_getKeywordValuesForLocale(...) ucal_getKeywordValuesForLocale_ptr(__VA_ARGS__)
#define ucal_getLimit(...) ucal_getLimit_ptr(__VA_ARGS__)
#define ucal_getNow(...) ucal_getNow_ptr(__VA_ARGS__)
#define ucal_getTimeZoneDisplayName(...) ucal_getTimeZoneDisplayName_ptr(__VA_ARGS__)
#define ucal_getTimeZoneIDForWindowsID(...) ucal_getTimeZoneIDForWindowsID_ptr(__VA_ARGS__)
#define ucal_getWindowsTimeZoneID(...) ucal_getWindowsTimeZoneID_ptr(__VA_ARGS__)
#define ucal_open(...) ucal_open_ptr(__VA_ARGS__)
#define ucal_openTimeZoneIDEnumeration(...) ucal_openTimeZoneIDEnumeration_ptr(__VA_ARGS__)
#define ucal_set(...) ucal_set_ptr(__VA_ARGS__)
#define ucal_setMillis(...) ucal_setMillis_ptr(__VA_ARGS__)
#define ucol_close(...) ucol_close_ptr(__VA_ARGS__)
#define ucol_closeElements(...) ucol_closeElements_ptr(__VA_ARGS__)
#define ucol_getOffset(...) ucol_getOffset_ptr(__VA_ARGS__)
#define ucol_getRules(...) ucol_getRules_ptr(__VA_ARGS__)
#define ucol_getSortKey(...) ucol_getSortKey_ptr(__VA_ARGS__)
#define ucol_getStrength(...) ucol_getStrength_ptr(__VA_ARGS__)
#define ucol_getVersion(...) ucol_getVersion_ptr(__VA_ARGS__)
#define ucol_next(...) ucol_next_ptr(__VA_ARGS__)
#define ucol_previous(...) ucol_previous_ptr(__VA_ARGS__)
#define ucol_open(...) ucol_open_ptr(__VA_ARGS__)
#define ucol_openElements(...) ucol_openElements_ptr(__VA_ARGS__)
#define ucol_openRules(...) ucol_openRules_ptr(__VA_ARGS__)
#define ucol_safeClone(...) ucol_safeClone_ptr(__VA_ARGS__)
#define ucol_setAttribute(...) ucol_setAttribute_ptr(__VA_ARGS__)
#define ucol_setMaxVariable(...) ucol_setMaxVariable_ptr(__VA_ARGS__)
#define ucol_strcoll(...) ucol_strcoll_ptr(__VA_ARGS__)
#define ucurr_forLocale(...) ucurr_forLocale_ptr(__VA_ARGS__)
#define ucurr_getName(...) ucurr_getName_ptr(__VA_ARGS__)
#define udat_close(...) udat_close_ptr(__VA_ARGS__)
#define udat_countSymbols(...) udat_countSymbols_ptr(__VA_ARGS__)
#define udat_format(...) udat_format_ptr(__VA_ARGS__)
#define udat_getSymbols(...) udat_getSymbols_ptr(__VA_ARGS__)
#define udat_open(...) udat_open_ptr(__VA_ARGS__)
#define udat_setCalendar(...) udat_setCalendar_ptr(__VA_ARGS__)
#define udat_toPattern(...) udat_toPattern_ptr(__VA_ARGS__)
#define udatpg_close(...) udatpg_close_ptr(__VA_ARGS__)
#define udatpg_getBestPattern(...) udatpg_getBestPattern_ptr(__VA_ARGS__)
#define udatpg_open(...) udatpg_open_ptr(__VA_ARGS__)
#define uenum_close(...) uenum_close_ptr(__VA_ARGS__)
#define uenum_count(...) uenum_count_ptr(__VA_ARGS__)
#define uenum_next(...) uenum_next_ptr(__VA_ARGS__)
#define uidna_close(...) uidna_close_ptr(__VA_ARGS__)
#define uidna_nameToASCII(...) uidna_nameToASCII_ptr(__VA_ARGS__)
#define uidna_nameToUnicode(...) uidna_nameToUnicode_ptr(__VA_ARGS__)
#define uidna_openUTS46(...) uidna_openUTS46_ptr(__VA_ARGS__)
#define uldn_close(...) uldn_close_ptr(__VA_ARGS__)
#define uldn_keyValueDisplayName(...) uldn_keyValueDisplayName_ptr(__VA_ARGS__)
#define uldn_open(...) uldn_open_ptr(__VA_ARGS__)
#define uloc_canonicalize(...) uloc_canonicalize_ptr(__VA_ARGS__)
#define uloc_countAvailable(...) uloc_countAvailable_ptr(__VA_ARGS__)
#define uloc_getAvailable(...) uloc_getAvailable_ptr(__VA_ARGS__)
#define uloc_getBaseName(...) uloc_getBaseName_ptr(__VA_ARGS__)
#define uloc_getCharacterOrientation(...) uloc_getCharacterOrientation_ptr(__VA_ARGS__)
#define uloc_getCountry(...) uloc_getCountry_ptr(__VA_ARGS__)
#define uloc_getDefault(...) uloc_getDefault_ptr(__VA_ARGS__)
#define uloc_getDisplayCountry(...) uloc_getDisplayCountry_ptr(__VA_ARGS__)
#define uloc_getDisplayLanguage(...) uloc_getDisplayLanguage_ptr(__VA_ARGS__)
#define uloc_getDisplayName(...) uloc_getDisplayName_ptr(__VA_ARGS__)
#define uloc_getISO3Country(...) uloc_getISO3Country_ptr(__VA_ARGS__)
#define uloc_getISO3Language(...) uloc_getISO3Language_ptr(__VA_ARGS__)
#define uloc_getKeywordValue(...) uloc_getKeywordValue_ptr(__VA_ARGS__)
#define uloc_getLanguage(...) uloc_getLanguage_ptr(__VA_ARGS__)
#define uloc_getLCID(...) uloc_getLCID_ptr(__VA_ARGS__)
#define uloc_getName(...) uloc_getName_ptr(__VA_ARGS__)
#define uloc_getParent(...) uloc_getParent_ptr(__VA_ARGS__)
#define uloc_setKeywordValue(...) uloc_setKeywordValue_ptr(__VA_ARGS__)
#define ulocdata_getCLDRVersion(...) ulocdata_getCLDRVersion_ptr(__VA_ARGS__)
#define ulocdata_getMeasurementSystem(...) ulocdata_getMeasurementSystem_ptr(__VA_ARGS__)
#define unorm2_getNFCInstance(...) unorm2_getNFCInstance_ptr(__VA_ARGS__)
#define unorm2_getNFDInstance(...) unorm2_getNFDInstance_ptr(__VA_ARGS__)
#define unorm2_getNFKCInstance(...) unorm2_getNFKCInstance_ptr(__VA_ARGS__)
#define unorm2_getNFKDInstance(...) unorm2_getNFKDInstance_ptr(__VA_ARGS__)
#define unorm2_isNormalized(...) unorm2_isNormalized_ptr(__VA_ARGS__)
#define unorm2_normalize(...) unorm2_normalize_ptr(__VA_ARGS__)
#define unum_close(...) unum_close_ptr(__VA_ARGS__)
#define unum_getAttribute(...) unum_getAttribute_ptr(__VA_ARGS__)
#define unum_getSymbol(...) unum_getSymbol_ptr(__VA_ARGS__)
#define unum_open(...) unum_open_ptr(__VA_ARGS__)
#define unum_toPattern(...) unum_toPattern_ptr(__VA_ARGS__)
#define ures_close(...) ures_close_ptr(__VA_ARGS__)
#define ures_getByKey(...) ures_getByKey_ptr(__VA_ARGS__)
#define ures_getSize(...) ures_getSize_ptr(__VA_ARGS__)
#define ures_getStringByIndex(...) ures_getStringByIndex_ptr(__VA_ARGS__)
#define ures_open(...) ures_open_ptr(__VA_ARGS__)
#define usearch_close(...) usearch_close_ptr(__VA_ARGS__)
#define usearch_first(...) usearch_first_ptr(__VA_ARGS__)
#define usearch_getBreakIterator(...) usearch_getBreakIterator_ptr(__VA_ARGS__)
#define usearch_getMatchedLength(...) usearch_getMatchedLength_ptr(__VA_ARGS__)
#define usearch_last(...) usearch_last_ptr(__VA_ARGS__)
#define usearch_openFromCollator(...) usearch_openFromCollator_ptr(__VA_ARGS__)
#define usearch_reset(...) usearch_reset_ptr(__VA_ARGS__)
#define usearch_setPattern(...) usearch_setPattern_ptr(__VA_ARGS__)
#define usearch_setText(...) usearch_setText_ptr(__VA_ARGS__)

#else // !defined(STATIC_ICU)

#define ucal_getWindowsTimeZoneID_ptr ucal_getWindowsTimeZoneID
#define ucal_getTimeZoneIDForWindowsID_ptr ucal_getTimeZoneIDForWindowsID

#endif // !defined(STATIC_ICU)
