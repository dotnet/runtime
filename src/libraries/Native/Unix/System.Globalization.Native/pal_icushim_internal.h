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
#include <unicode/uclean.h>
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

#ifndef __typeof
#define __typeof decltype
#endif

#define HAVE_SET_MAX_VARIABLE 1
#define UDAT_STANDALONE_SHORTER_WEEKDAYS 1

#endif

#include "pal_compiler.h"

#if !defined(STATIC_ICU)
// List of all functions from the ICU libraries that are used in the System.Globalization.Native.so
#define FOR_ALL_UNCONDITIONAL_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(u_charsToUChars, libicuuc) \
    PER_FUNCTION_BLOCK(u_getVersion, libicuuc) \
    PER_FUNCTION_BLOCK(u_strlen, libicuuc) \
    PER_FUNCTION_BLOCK(u_strncpy, libicuuc) \
    PER_FUNCTION_BLOCK(u_tolower, libicuuc) \
    PER_FUNCTION_BLOCK(u_toupper, libicuuc) \
    PER_FUNCTION_BLOCK(ucal_add, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_close, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_get, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_getAttribute, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_getKeywordValuesForLocale, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_getLimit, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_getTimeZoneDisplayName, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_open, libicui18n) \
    PER_FUNCTION_BLOCK(ucal_set, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_close, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_closeElements, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_getRules, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_getSortKey, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_getStrength, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_getVersion, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_next, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_previous, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_open, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_openElements, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_openRules, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_safeClone, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_setAttribute, libicui18n) \
    PER_FUNCTION_BLOCK(ucol_strcoll, libicui18n) \
    PER_FUNCTION_BLOCK(udat_close, libicui18n) \
    PER_FUNCTION_BLOCK(udat_countSymbols, libicui18n) \
    PER_FUNCTION_BLOCK(udat_getSymbols, libicui18n) \
    PER_FUNCTION_BLOCK(udat_open, libicui18n) \
    PER_FUNCTION_BLOCK(udat_setCalendar, libicui18n) \
    PER_FUNCTION_BLOCK(udat_toPattern, libicui18n) \
    PER_FUNCTION_BLOCK(udatpg_close, libicui18n) \
    PER_FUNCTION_BLOCK(udatpg_getBestPattern, libicui18n) \
    PER_FUNCTION_BLOCK(udatpg_open, libicui18n) \
    PER_FUNCTION_BLOCK(uenum_close, libicuuc) \
    PER_FUNCTION_BLOCK(uenum_count, libicuuc) \
    PER_FUNCTION_BLOCK(uenum_next, libicuuc) \
    PER_FUNCTION_BLOCK(uidna_close, libicuuc) \
    PER_FUNCTION_BLOCK(uidna_nameToASCII, libicuuc) \
    PER_FUNCTION_BLOCK(uidna_nameToUnicode, libicuuc) \
    PER_FUNCTION_BLOCK(uidna_openUTS46, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_canonicalize, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_countAvailable, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getAvailable, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getBaseName, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getCharacterOrientation, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getCountry, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getDefault, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getDisplayCountry, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getDisplayLanguage, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getDisplayName, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getISO3Country, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getISO3Language, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getKeywordValue, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getLanguage, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getLCID, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getName, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_getParent, libicuuc) \
    PER_FUNCTION_BLOCK(uloc_setKeywordValue, libicuuc) \
    PER_FUNCTION_BLOCK(ulocdata_getCLDRVersion, libicui18n) \
    PER_FUNCTION_BLOCK(ulocdata_getMeasurementSystem, libicui18n) \
    PER_FUNCTION_BLOCK(unorm2_getNFCInstance, libicuuc) \
    PER_FUNCTION_BLOCK(unorm2_getNFDInstance, libicuuc) \
    PER_FUNCTION_BLOCK(unorm2_getNFKCInstance, libicuuc) \
    PER_FUNCTION_BLOCK(unorm2_getNFKDInstance, libicuuc) \
    PER_FUNCTION_BLOCK(unorm2_isNormalized, libicuuc) \
    PER_FUNCTION_BLOCK(unorm2_normalize, libicuuc) \
    PER_FUNCTION_BLOCK(unum_close, libicui18n) \
    PER_FUNCTION_BLOCK(unum_getAttribute, libicui18n) \
    PER_FUNCTION_BLOCK(unum_getSymbol, libicui18n) \
    PER_FUNCTION_BLOCK(unum_open, libicui18n) \
    PER_FUNCTION_BLOCK(unum_toPattern, libicui18n) \
    PER_FUNCTION_BLOCK(ures_close, libicuuc) \
    PER_FUNCTION_BLOCK(ures_getByKey, libicuuc) \
    PER_FUNCTION_BLOCK(ures_getSize, libicuuc) \
    PER_FUNCTION_BLOCK(ures_getStringByIndex, libicuuc) \
    PER_FUNCTION_BLOCK(ures_open, libicuuc) \
    PER_FUNCTION_BLOCK(usearch_close, libicui18n) \
    PER_FUNCTION_BLOCK(usearch_first, libicui18n) \
    PER_FUNCTION_BLOCK(usearch_getMatchedLength, libicui18n) \
    PER_FUNCTION_BLOCK(usearch_last, libicui18n) \
    PER_FUNCTION_BLOCK(usearch_openFromCollator, libicui18n)

#if HAVE_SET_MAX_VARIABLE
#define FOR_ALL_SET_VARIABLE_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucol_setMaxVariable, libicui18n)
#else

#define FOR_ALL_SET_VARIABLE_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucol_setVariableTop, libicui18n)
#endif

#if defined(TARGET_WINDOWS)
#define FOR_ALL_OS_CONDITIONAL_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucurr_forLocale, libicuuc) \
    PER_FUNCTION_BLOCK(ucurr_getName, libicuuc) \
    PER_FUNCTION_BLOCK(uldn_close, libicuuc) \
    PER_FUNCTION_BLOCK(uldn_keyValueDisplayName, libicuuc) \
    PER_FUNCTION_BLOCK(uldn_open, libicuuc)
#else
    // Unix ICU is dynamically resolved at runtime and these APIs in old versions
    // of ICU were in libicui18n
#define FOR_ALL_OS_CONDITIONAL_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucurr_forLocale, libicui18n) \
    PER_FUNCTION_BLOCK(ucurr_getName, libicui18n) \
    PER_FUNCTION_BLOCK(uldn_close, libicui18n) \
    PER_FUNCTION_BLOCK(uldn_keyValueDisplayName, libicui18n) \
    PER_FUNCTION_BLOCK(uldn_open, libicui18n)
#endif

#define FOR_ALL_ICU_FUNCTIONS \
    FOR_ALL_UNCONDITIONAL_ICU_FUNCTIONS \
    FOR_ALL_SET_VARIABLE_ICU_FUNCTIONS \
    FOR_ALL_OS_CONDITIONAL_ICU_FUNCTIONS

// Declare pointers to all the used ICU functions
#define PER_FUNCTION_BLOCK(fn, lib) EXTERN_C __typeof(fn)* fn##_ptr;
FOR_ALL_ICU_FUNCTIONS
#undef PER_FUNCTION_BLOCK

// Redefine all calls to ICU functions as calls through pointers that are set
// to the functions of the selected version of ICU in the initialization.
#define u_charsToUChars(...) u_charsToUChars_ptr(__VA_ARGS__)
#define u_getVersion(...) u_getVersion_ptr(__VA_ARGS__)
#define u_strlen(...) u_strlen_ptr(__VA_ARGS__)
#define u_strncpy(...) u_strncpy_ptr(__VA_ARGS__)
#define u_tolower(...) u_tolower_ptr(__VA_ARGS__)
#define u_toupper(...) u_toupper_ptr(__VA_ARGS__)
#define ucal_add(...) ucal_add_ptr(__VA_ARGS__)
#define ucal_close(...) ucal_close_ptr(__VA_ARGS__)
#define ucal_get(...) ucal_get_ptr(__VA_ARGS__)
#define ucal_getAttribute(...) ucal_getAttribute_ptr(__VA_ARGS__)
#define ucal_getKeywordValuesForLocale(...) ucal_getKeywordValuesForLocale_ptr(__VA_ARGS__)
#define ucal_getLimit(...) ucal_getLimit_ptr(__VA_ARGS__)
#define ucal_getTimeZoneDisplayName(...) ucal_getTimeZoneDisplayName_ptr(__VA_ARGS__)
#define ucal_open(...) ucal_open_ptr(__VA_ARGS__)
#define ucal_set(...) ucal_set_ptr(__VA_ARGS__)
#define ucol_close(...) ucol_close_ptr(__VA_ARGS__)
#define ucol_closeElements(...) ucol_closeElements_ptr(__VA_ARGS__)
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
#if HAVE_SET_MAX_VARIABLE
#define ucol_setMaxVariable(...) ucol_setMaxVariable_ptr(__VA_ARGS__)
#else
#define ucol_setVariableTop(...) ucol_setVariableTop_ptr(__VA_ARGS__)
#endif
#define ucol_strcoll(...) ucol_strcoll_ptr(__VA_ARGS__)
#define ucurr_forLocale(...) ucurr_forLocale_ptr(__VA_ARGS__)
#define ucurr_getName(...) ucurr_getName_ptr(__VA_ARGS__)
#define udat_close(...) udat_close_ptr(__VA_ARGS__)
#define udat_countSymbols(...) udat_countSymbols_ptr(__VA_ARGS__)
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
#define usearch_getMatchedLength(...) usearch_getMatchedLength_ptr(__VA_ARGS__)
#define usearch_last(...) usearch_last_ptr(__VA_ARGS__)
#define usearch_openFromCollator(...) usearch_openFromCollator_ptr(__VA_ARGS__)

#endif // !defined(STATIC_ICU)
