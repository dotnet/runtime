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
#include "pal_icushim_native_static.h"

#define PER_FUNCTION_BLOCK(fn) __typeof(fn)* fn##_ptr = fn;

// List of all functions from the ICU libraries that are used in the System.Globalization.Native.so
#define FOR_ALL_UNCONDITIONAL_STATIC_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(u_charsToUChars) \
    PER_FUNCTION_BLOCK(u_getVersion) \
    PER_FUNCTION_BLOCK(u_strcmp) \
    PER_FUNCTION_BLOCK(u_strcpy) \
    PER_FUNCTION_BLOCK(u_strlen) \
    PER_FUNCTION_BLOCK(u_strncpy) \
    PER_FUNCTION_BLOCK(u_tolower) \
    PER_FUNCTION_BLOCK(u_toupper) \
    PER_FUNCTION_BLOCK(u_uastrncpy) \
    PER_FUNCTION_BLOCK(ubrk_close) \
    PER_FUNCTION_BLOCK(ubrk_openRules) \
    PER_FUNCTION_BLOCK(ucal_add) \
    PER_FUNCTION_BLOCK(ucal_close) \
    PER_FUNCTION_BLOCK(ucal_get) \
    PER_FUNCTION_BLOCK(ucal_getAttribute) \
    PER_FUNCTION_BLOCK(ucal_getKeywordValuesForLocale) \
    PER_FUNCTION_BLOCK(ucal_getLimit) \
    PER_FUNCTION_BLOCK(ucal_getNow) \
    PER_FUNCTION_BLOCK(ucal_getTimeZoneDisplayName) \
    PER_FUNCTION_BLOCK(ucal_open) \
    PER_FUNCTION_BLOCK(ucal_openTimeZoneIDEnumeration) \
    PER_FUNCTION_BLOCK(ucal_set) \
    PER_FUNCTION_BLOCK(ucal_setMillis) \
    PER_FUNCTION_BLOCK(ucol_close) \
    PER_FUNCTION_BLOCK(ucol_closeElements) \
    PER_FUNCTION_BLOCK(ucol_getOffset) \
    PER_FUNCTION_BLOCK(ucol_getRules) \
    PER_FUNCTION_BLOCK(ucol_getSortKey) \
    PER_FUNCTION_BLOCK(ucol_getStrength) \
    PER_FUNCTION_BLOCK(ucol_getVersion) \
    PER_FUNCTION_BLOCK(ucol_next) \
    PER_FUNCTION_BLOCK(ucol_previous) \
    PER_FUNCTION_BLOCK(ucol_open) \
    PER_FUNCTION_BLOCK(ucol_openElements) \
    PER_FUNCTION_BLOCK(ucol_openRules) \
    PER_FUNCTION_BLOCK(ucol_setAttribute) \
    PER_FUNCTION_BLOCK(ucol_strcoll) \
    PER_FUNCTION_BLOCK(udat_close) \
    PER_FUNCTION_BLOCK(udat_countSymbols) \
    PER_FUNCTION_BLOCK(udat_format) \
    PER_FUNCTION_BLOCK(udat_getSymbols) \
    PER_FUNCTION_BLOCK(udat_open) \
    PER_FUNCTION_BLOCK(udat_setCalendar) \
    PER_FUNCTION_BLOCK(udat_toPattern) \
    PER_FUNCTION_BLOCK(udatpg_close) \
    PER_FUNCTION_BLOCK(udatpg_getBestPattern) \
    PER_FUNCTION_BLOCK(udatpg_open) \
    PER_FUNCTION_BLOCK(uenum_close) \
    PER_FUNCTION_BLOCK(uenum_count) \
    PER_FUNCTION_BLOCK(uenum_next) \
    PER_FUNCTION_BLOCK(uidna_close) \
    PER_FUNCTION_BLOCK(uidna_nameToASCII) \
    PER_FUNCTION_BLOCK(uidna_nameToUnicode) \
    PER_FUNCTION_BLOCK(uidna_openUTS46) \
    PER_FUNCTION_BLOCK(uloc_canonicalize) \
    PER_FUNCTION_BLOCK(uloc_countAvailable) \
    PER_FUNCTION_BLOCK(uloc_getAvailable) \
    PER_FUNCTION_BLOCK(uloc_getBaseName) \
    PER_FUNCTION_BLOCK(uloc_getCharacterOrientation) \
    PER_FUNCTION_BLOCK(uloc_getCountry) \
    PER_FUNCTION_BLOCK(uloc_getDefault) \
    PER_FUNCTION_BLOCK(uloc_getDisplayCountry) \
    PER_FUNCTION_BLOCK(uloc_getDisplayLanguage) \
    PER_FUNCTION_BLOCK(uloc_getDisplayName) \
    PER_FUNCTION_BLOCK(uloc_getISO3Country) \
    PER_FUNCTION_BLOCK(uloc_getISO3Language) \
    PER_FUNCTION_BLOCK(uloc_getKeywordValue) \
    PER_FUNCTION_BLOCK(uloc_getLanguage) \
    PER_FUNCTION_BLOCK(uloc_getLCID) \
    PER_FUNCTION_BLOCK(uloc_getName) \
    PER_FUNCTION_BLOCK(uloc_getParent) \
    PER_FUNCTION_BLOCK(uloc_setKeywordValue) \
    PER_FUNCTION_BLOCK(ulocdata_getCLDRVersion) \
    PER_FUNCTION_BLOCK(ulocdata_getMeasurementSystem) \
    PER_FUNCTION_BLOCK(unorm2_getNFCInstance) \
    PER_FUNCTION_BLOCK(unorm2_getNFDInstance) \
    PER_FUNCTION_BLOCK(unorm2_getNFKCInstance) \
    PER_FUNCTION_BLOCK(unorm2_getNFKDInstance) \
    PER_FUNCTION_BLOCK(unorm2_isNormalized) \
    PER_FUNCTION_BLOCK(unorm2_normalize) \
    PER_FUNCTION_BLOCK(unum_close) \
    PER_FUNCTION_BLOCK(unum_getAttribute) \
    PER_FUNCTION_BLOCK(unum_getSymbol) \
    PER_FUNCTION_BLOCK(unum_open) \
    PER_FUNCTION_BLOCK(unum_toPattern) \
    PER_FUNCTION_BLOCK(ures_close) \
    PER_FUNCTION_BLOCK(ures_getByKey) \
    PER_FUNCTION_BLOCK(ures_getSize) \
    PER_FUNCTION_BLOCK(ures_getStringByIndex) \
    PER_FUNCTION_BLOCK(ures_open) \
    PER_FUNCTION_BLOCK(usearch_close) \
    PER_FUNCTION_BLOCK(usearch_first) \
    PER_FUNCTION_BLOCK(usearch_getBreakIterator) \
    PER_FUNCTION_BLOCK(usearch_getMatchedLength) \
    PER_FUNCTION_BLOCK(usearch_last) \
    PER_FUNCTION_BLOCK(usearch_openFromCollator) \
    PER_FUNCTION_BLOCK(usearch_setPattern) \
    PER_FUNCTION_BLOCK(usearch_setText)

#define FOR_ALL_OS_CONDITIONAL_STATIC_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucurr_forLocale) \
    PER_FUNCTION_BLOCK(ucurr_getName) \
    PER_FUNCTION_BLOCK(uldn_close) \
    PER_FUNCTION_BLOCK(uldn_keyValueDisplayName) \
    PER_FUNCTION_BLOCK(uldn_open)

// The following are the list of the ICU APIs which are optional. If these APIs exist in the ICU version we load at runtime, then we'll use it.
// Otherwise, we'll just not provide the functionality to users which needed these APIs.
#define FOR_ALL_OPTIONAL_STATIC_ICU_FUNCTIONS \
    PER_FUNCTION_BLOCK(ucal_getWindowsTimeZoneID) \
    PER_FUNCTION_BLOCK(ucal_getTimeZoneIDForWindowsID) \
    PER_FUNCTION_BLOCK(ucol_setMaxVariable)

// ucol_setVariableTop is deprecated
// ucol_safeClone is deprecated

#define FOR_ALL_STATIC_ICU_FUNCTIONS \
    FOR_ALL_UNCONDITIONAL_STATIC_ICU_FUNCTIONS \
    FOR_ALL_OPTIONAL_STATIC_ICU_FUNCTIONS \
    FOR_ALL_OS_CONDITIONAL_STATIC_ICU_FUNCTIONS

void InitWithStaticLibICUFunctions()
{
     FOR_ALL_STATIC_ICU_FUNCTIONS
}
