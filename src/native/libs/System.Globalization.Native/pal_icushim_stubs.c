// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "pal_icushim_internal.h"

typedef struct UCollator UCollator;
typedef struct UCollationElements UCollationElements;
typedef struct UEnumeration UEnumeration;
typedef struct UIDNA UIDNA;
typedef struct UNormalizer2 UNormalizer2;
typedef struct ULocaleDisplayNames ULocaleDisplayNames;
typedef struct UResourceBundle UResourceBundle;
typedef struct UStringSearch UStringSearch;
typedef struct UBreakIterator UBreakIterator;

typedef int8_t UBool;
typedef uint16_t UChar;
typedef int32_t UChar32;
typedef double UDate;
typedef uint8_t UVersionInfo[U_MAX_VERSION_LENGTH];

typedef void* UNumberFormat;
typedef void* UDateFormat;
typedef void* UDateTimePatternGenerator;
typedef void* UCalendar;


#define CREATE_STUB_VOID(fn_decl) fn_decl {}

#define CREATE_STUB_WITH_RETURN(type, fn_decl) fn_decl { return (type)0; }

CREATE_STUB_WITH_RETURN(UCollator *, UCollator * ucol_safeClone(const UCollator* coll, void* stackBuffer, int32_t* pBufferSize, UErrorCode* status))
CREATE_STUB_WITH_RETURN(const char *, const char * u_errorName(UErrorCode status))

CREATE_STUB_VOID(void udata_setCommonData(const void * v, UErrorCode * err))

CREATE_STUB_VOID(void u_charsToUChars(const char * cs, UChar * us, int32_t length))
CREATE_STUB_VOID(void u_getVersion(UVersionInfo versionArray))
CREATE_STUB_WITH_RETURN(int32_t, int32_t u_strlen(const UChar * s))
CREATE_STUB_WITH_RETURN(int32_t, int32_t u_strcmp(const UChar * s1, const UChar * s2))
CREATE_STUB_WITH_RETURN(UChar *, UChar * u_strcpy(UChar * dst, const UChar * src))
CREATE_STUB_WITH_RETURN(UChar *, UChar * u_strncpy(UChar * dst, const UChar * src, int32_t n))
CREATE_STUB_WITH_RETURN(UChar32, UChar32 u_tolower(UChar32 c))
CREATE_STUB_WITH_RETURN(UChar32, UChar32 u_toupper(UChar32 c))

CREATE_STUB_WITH_RETURN(UChar*, UChar* u_uastrncpy(UChar * dst, const char * src, int32_t n))
CREATE_STUB_VOID(void ubrk_close(UBreakIterator * bi))
CREATE_STUB_WITH_RETURN(UBreakIterator*, UBreakIterator* ubrk_openRules(const UChar * rules, int32_t rulesLength, const UChar * text, int32_t textLength, UParseError * parseErr, UErrorCode * status))
CREATE_STUB_VOID(void ucal_add(UCalendar * cal, UCalendarDateFields field, int32_t amount, UErrorCode * status))
CREATE_STUB_VOID(void ucal_close(UCalendar * cal))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucal_get(const UCalendar * cal, UCalendarDateFields field, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucal_getAttribute(const UCalendar * cal, UCalendarAttribute attr))
CREATE_STUB_WITH_RETURN(UEnumeration *, UEnumeration * ucal_getKeywordValuesForLocale(const char * key, const char * locale, UBool commonlyUsed, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucal_getLimit(const UCalendar * cal, UCalendarDateFields field, UCalendarLimitType type, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UDate, UDate ucal_getNow(void))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucal_getTimeZoneDisplayName(const UCalendar * cal, UCalendarDisplayNameType type, const char * locale, UChar * result, int32_t resultLength, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucal_getTimeZoneIDForWindowsID(const UChar * winid, int32_t	len, const char * region, UChar * id, int32_t idCapacity, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucal_getWindowsTimeZoneID(const UChar *	id, int32_t	len, UChar * winid, int32_t	winidCapacity, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UCalendar *, UCalendar * ucal_open(const UChar * zoneID, int32_t len, const char * locale, UCalendarType type, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UEnumeration *, UEnumeration * ucal_openTimeZoneIDEnumeration(USystemTimeZoneType zoneType, const char * region, const int32_t * rawOffset, UErrorCode * ec))
CREATE_STUB_VOID(void ucal_set(UCalendar * cal, UCalendarDateFields field, int32_t value))
CREATE_STUB_VOID(void ucal_setMillis(UCalendar * cal, UDate dateTime, UErrorCode * status))
CREATE_STUB_VOID(void ucol_close(UCollator * coll))
CREATE_STUB_VOID(void ucol_closeElements(UCollationElements * elems))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucol_getOffset(const UCollationElements *elems))
CREATE_STUB_WITH_RETURN(const UChar *, const UChar * ucol_getRules(const UCollator * coll, int32_t * length))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucol_getSortKey(const UCollator * coll, const UChar * source, int32_t sourceLength, uint8_t * result, int32_t resultLength))
CREATE_STUB_WITH_RETURN(UCollationStrength, UCollationStrength ucol_getStrength(const UCollator * coll))
CREATE_STUB_VOID(void ucol_getVersion(const UCollator * coll, UVersionInfo info))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucol_next(UCollationElements * elems, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucol_previous(UCollationElements * elems, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UCollator *, UCollator * ucol_open(const char * loc, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UCollationElements *, UCollationElements * ucol_openElements(const UCollator * coll, const UChar * text, int32_t textLength, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UCollator *, UCollator * ucol_openRules(const UChar * rules, int32_t rulesLength, UColAttributeValue normalizationMode, UCollationStrength strength, UParseError * parseError, UErrorCode * status))
//CREATE_STUB_WITH_RETURN(UCollator *, UCollator * ucol_clone(const UCollator * coll, UErrorCode * status))
CREATE_STUB_VOID(void ucol_setAttribute(UCollator * coll, UColAttribute attr, UColAttributeValue value, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UCollationResult, UCollationResult ucol_strcoll(const UCollator * coll, const UChar * source, int32_t sourceLength, const UChar * target, int32_t targetLength))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ucurr_forLocale(const char * locale, UChar * buff, int32_t buffCapacity, UErrorCode * ec))
CREATE_STUB_WITH_RETURN(const UChar *, const UChar * ucurr_getName(const UChar * currency, const char * locale, UCurrNameStyle nameStyle, UBool * isChoiceFormat, int32_t * len, UErrorCode * ec))
CREATE_STUB_VOID(void udat_close(UDateFormat * format))
CREATE_STUB_WITH_RETURN(int32_t, int32_t udat_countSymbols(const UDateFormat * fmt, UDateFormatSymbolType type))
CREATE_STUB_WITH_RETURN(int32_t, int32_t udat_format(const UDateFormat * format, UDate dateToFormat, UChar * result, int32_t resultLength, UFieldPosition * position, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t udat_getSymbols(const UDateFormat * fmt, UDateFormatSymbolType type, int32_t symbolIndex, UChar * result, int32_t resultLength, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UDateFormat *, UDateFormat * udat_open(UDateFormatStyle timeStyle, UDateFormatStyle dateStyle, const char * locale, const UChar * tzID, int32_t tzIDLength, const UChar * pattern, int32_t patternLength, UErrorCode * status))
CREATE_STUB_VOID(void udat_setCalendar(UDateFormat * fmt, const UCalendar * calendarToSet))
CREATE_STUB_WITH_RETURN(int32_t, int32_t udat_toPattern(const UDateFormat * fmt, UBool localized, UChar * result, int32_t resultLength, UErrorCode * status))
CREATE_STUB_VOID(void udatpg_close(UDateTimePatternGenerator * dtpg))
CREATE_STUB_WITH_RETURN(int32_t, int32_t udatpg_getBestPattern(UDateTimePatternGenerator * dtpg, const UChar * skeleton, int32_t length, UChar * bestPattern, int32_t capacity, UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(UDateTimePatternGenerator *, UDateTimePatternGenerator * udatpg_open(const char * locale, UErrorCode * pErrorCode))
CREATE_STUB_VOID(void uenum_close(UEnumeration * en))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uenum_count(UEnumeration * en, UErrorCode * status))
CREATE_STUB_WITH_RETURN(const char *, const char * uenum_next(UEnumeration * en, int32_t * resultLength, UErrorCode * status))
CREATE_STUB_VOID(void uidna_close(UIDNA * idna))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uidna_nameToASCII(const UIDNA * idna, const UChar * name, int32_t length, UChar * dest, int32_t capacity, UIDNAInfo * pInfo, UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uidna_nameToUnicode(const UIDNA * idna, const UChar * name, int32_t length, UChar * dest, int32_t capacity, UIDNAInfo * pInfo, UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(UIDNA *, UIDNA * uidna_openUTS46(uint32_t options, UErrorCode * pErrorCode))
CREATE_STUB_VOID(void uldn_close(ULocaleDisplayNames * ldn))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uldn_keyValueDisplayName(const ULocaleDisplayNames * ldn, const char * key, const char * value, UChar * result, int32_t maxResultSize, UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(ULocaleDisplayNames *, ULocaleDisplayNames * uldn_open(const char * locale, UDialectHandling dialectHandling, UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_canonicalize(const char * localeID, char * name, int32_t nameCapacity, UErrorCode * err))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_countAvailable(void))
CREATE_STUB_WITH_RETURN(const char *, const char * uloc_getAvailable(int32_t n))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getBaseName(const char * localeID, char * name, int32_t nameCapacity, UErrorCode * err))
CREATE_STUB_WITH_RETURN(ULayoutType, ULayoutType uloc_getCharacterOrientation(const char * localeId, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getCountry(const char * localeID, char * country, int32_t countryCapacity, UErrorCode * err))
CREATE_STUB_WITH_RETURN(const char *, const char * uloc_getDefault(void))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getDisplayCountry(const char * locale, const char * displayLocale, UChar * country, int32_t countryCapacity, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getDisplayLanguage(const char * locale, const char * displayLocale, UChar * language, int32_t languageCapacity, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getDisplayName(const char * localeID, const char * inLocaleID, UChar * result, int32_t maxResultSize, UErrorCode * err))
CREATE_STUB_WITH_RETURN(const char *, const char * uloc_getISO3Country(const char * localeID))
CREATE_STUB_WITH_RETURN(const char *, const char * uloc_getISO3Language(const char * localeID))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getKeywordValue(const char * localeID, const char * keywordName, char * buffer, int32_t bufferCapacity, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getLanguage(const char * localeID, char * language, int32_t languageCapacity, UErrorCode * err))
CREATE_STUB_WITH_RETURN(uint32_t, uint32_t uloc_getLCID(const char * localeID))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getName(const char * localeID, char * name, int32_t nameCapacity, UErrorCode * err))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_getParent(const char * localeID, char * parent, int32_t parentCapacity, UErrorCode * err))
CREATE_STUB_WITH_RETURN(int32_t, int32_t uloc_setKeywordValue(const char * keywordName, const char * keywordValue, char * buffer, int32_t bufferCapacity, UErrorCode * status))
CREATE_STUB_VOID(void ulocdata_getCLDRVersion(UVersionInfo versionArray, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UMeasurementSystem, UMeasurementSystem ulocdata_getMeasurementSystem(const char * localeID, UErrorCode * status))
CREATE_STUB_WITH_RETURN(const UNormalizer2 *, const UNormalizer2 * unorm2_getNFCInstance(UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(const UNormalizer2 *, const UNormalizer2 * unorm2_getNFDInstance(UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(const UNormalizer2 *, const UNormalizer2 * unorm2_getNFKCInstance(UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(const UNormalizer2 *, const UNormalizer2 * unorm2_getNFKDInstance(UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(UBool, UBool unorm2_isNormalized(const UNormalizer2 * norm2, const UChar * s, int32_t length, UErrorCode * pErrorCode))
CREATE_STUB_WITH_RETURN(int32_t, int32_t unorm2_normalize(const UNormalizer2 * norm2, const UChar * src, int32_t length, UChar * dest, int32_t capacity, UErrorCode * pErrorCode))
CREATE_STUB_VOID(void unum_close(UNumberFormat * fmt))
CREATE_STUB_WITH_RETURN(int32_t, int32_t unum_getAttribute(const UNumberFormat * fmt, UNumberFormatAttribute attr))
CREATE_STUB_WITH_RETURN(int32_t, int32_t unum_getSymbol(const UNumberFormat * fmt, UNumberFormatSymbol symbol, UChar * buffer, int32_t size, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UNumberFormat *, UNumberFormat * unum_open(UNumberFormatStyle style, const UChar * pattern, int32_t patternLength, const char * locale, UParseError * parseErr, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t unum_toPattern(const UNumberFormat * fmt, UBool isPatternLocalized, UChar * result, int32_t resultLength, UErrorCode * status))
CREATE_STUB_VOID(void ures_close(UResourceBundle * resourceBundle))
CREATE_STUB_WITH_RETURN(UResourceBundle *, UResourceBundle * ures_getByKey(const UResourceBundle * resourceBundle, const char * key, UResourceBundle * fillIn, UErrorCode * status))
CREATE_STUB_WITH_RETURN(int32_t, int32_t ures_getSize(const UResourceBundle * resourceBundle))
CREATE_STUB_WITH_RETURN(const UChar *, const UChar * ures_getStringByIndex(const UResourceBundle * resourceBundle, int32_t indexS, int32_t * len, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UResourceBundle *, UResourceBundle * ures_open(const char * packageName, const char * locale, UErrorCode * status))
CREATE_STUB_VOID(void usearch_close(UStringSearch * searchiter))
CREATE_STUB_WITH_RETURN(int32_t, int32_t usearch_first(UStringSearch * strsrch, UErrorCode * status))
CREATE_STUB_WITH_RETURN(const UBreakIterator*, const UBreakIterator* usearch_getBreakIterator(const UStringSearch * strsrch))
CREATE_STUB_WITH_RETURN(int32_t, int32_t usearch_getMatchedLength(const UStringSearch * strsrch))
CREATE_STUB_WITH_RETURN(int32_t, int32_t usearch_last(UStringSearch * strsrch, UErrorCode * status))
CREATE_STUB_WITH_RETURN(UStringSearch *, UStringSearch * usearch_openFromCollator(const UChar * pattern, int32_t patternlength, const UChar * text, int32_t textlength, const UCollator * collator, UBreakIterator * breakiter, UErrorCode * status))
CREATE_STUB_VOID(void usearch_setPattern(UStringSearch * strsrch, const UChar * pattern, int32_t patternlength, UErrorCode * status))
CREATE_STUB_VOID(void usearch_setText(UStringSearch * strsrch, const UChar * text, int32_t textlength, UErrorCode * status))
CREATE_STUB_VOID(void ucol_setMaxVariable(UCollator * coll, UColReorderCode group, UErrorCode * pErrorCode))

#undef CREATE_STUB_CREATE_STUB_VOID
#undef CREATE_STUB_WITH_RETURN
