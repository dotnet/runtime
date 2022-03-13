// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// definitions for ICU types and functions available in Android,
// extracted from ICU header files

#pragma once

#define U_MAX_VERSION_LENGTH 4

#define U_SUCCESS(x) ((x)<=U_ZERO_ERROR)
#define U_FAILURE(x) ((x)>U_ZERO_ERROR)

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

#ifndef TRUE
#   define TRUE  1
#endif
#ifndef FALSE
#   define FALSE 0
#endif

#define USEARCH_DONE -1

#define UCOL_NULLORDER ((int32_t)0xFFFFFFFF)
#define UCOL_BUILDER_VERSION 9
#define UCOL_RUNTIME_VERSION 9

#define ULOC_FULLNAME_CAPACITY 157
#define ULOC_LANG_CAPACITY 12
#define ULOC_KEYWORDS_CAPACITY 96
#define ULOC_ENGLISH "en"
#define ULOC_US "en_US"

#define U16_IS_LEAD(c) (((c)&0xfffffc00)==0xd800)
#define U16_IS_TRAIL(c) (((c)&0xfffffc00)==0xdc00)
#define U16_SURROGATE_OFFSET ((0xd800<<10UL)+0xdc00-0x10000)

#define U16_GET_SUPPLEMENTARY(lead, trail) \
    (((UChar32)(lead)<<10UL)+(UChar32)(trail)-U16_SURROGATE_OFFSET)

#define U16_APPEND(s, i, capacity, c, isError) { \
    if((uint32_t)(c)<=0xffff) { \
        (s)[(i)++]=(uint16_t)(c); \
    } else if((uint32_t)(c)<=0x10ffff && (i)+1<(capacity)) { \
        (s)[(i)++]=(uint16_t)(((c)>>10)+0xd7c0); \
        (s)[(i)++]=(uint16_t)(((c)&0x3ff)|0xdc00); \
    } else { \
        (isError)=TRUE; \
    } \
}

#define U16_NEXT(s, i, length, c) { \
    (c)=(s)[(i)++]; \
    if(U16_IS_LEAD(c)) { \
        uint16_t __c2; \
        if((i)!=(length) && U16_IS_TRAIL(__c2=(s)[(i)])) { \
            ++(i); \
            (c)=U16_GET_SUPPLEMENTARY((c), __c2); \
        } \
    } \
}

#define U16_FWD_1(s, i, length) { \
    if(U16_IS_LEAD((s)[(i)++]) && (i)!=(length) && U16_IS_TRAIL((s)[i])) { \
        ++(i); \
    } \
}

#define UIDNA_INFO_INITIALIZER { \
    (int16_t)sizeof(UIDNAInfo), \
    FALSE, FALSE, \
    0, 0, 0 }

typedef enum UErrorCode {
    U_STRING_NOT_TERMINATED_WARNING = -124,
    U_USING_DEFAULT_WARNING = -127,
    U_ZERO_ERROR =  0,
    U_ILLEGAL_ARGUMENT_ERROR = 1,
    U_INTERNAL_PROGRAM_ERROR = 5,
    U_MEMORY_ALLOCATION_ERROR = 7,
    U_BUFFER_OVERFLOW_ERROR = 15,
    U_UNSUPPORTED_ERROR = 16,
} UErrorCode;

typedef enum UCalendarDateFields {
    UCAL_ERA,
    UCAL_YEAR,
    UCAL_MONTH,
    UCAL_WEEK_OF_YEAR,
    UCAL_WEEK_OF_MONTH,
    UCAL_DATE,
    UCAL_DAY_OF_YEAR,
    UCAL_DAY_OF_WEEK,
    UCAL_DAY_OF_WEEK_IN_MONTH,
    UCAL_AM_PM,
    UCAL_HOUR,
    UCAL_HOUR_OF_DAY,
    UCAL_MINUTE,
    UCAL_SECOND,
    UCAL_MILLISECOND,
    UCAL_ZONE_OFFSET,
    UCAL_DST_OFFSET,
    UCAL_YEAR_WOY,
    UCAL_DOW_LOCAL,
    UCAL_EXTENDED_YEAR,
    UCAL_JULIAN_DAY,
    UCAL_MILLISECONDS_IN_DAY,
    UCAL_IS_LEAP_MONTH,
    UCAL_FIELD_COUNT,
    UCAL_DAY_OF_MONTH = UCAL_DATE
} UCalendarDateFields;

typedef enum UColAttribute {
    UCOL_FRENCH_COLLATION,
    UCOL_ALTERNATE_HANDLING,
    UCOL_CASE_FIRST,
    UCOL_CASE_LEVEL,
    UCOL_NORMALIZATION_MODE,
    UCOL_DECOMPOSITION_MODE = UCOL_NORMALIZATION_MODE,
    UCOL_STRENGTH,
    UCOL_NUMERIC_COLLATION = UCOL_STRENGTH + 2,
    UCOL_ATTRIBUTE_COUNT
} UColAttribute;

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wduplicate-enum"
typedef enum UColAttributeValue {
    UCOL_DEFAULT = -1,
    UCOL_PRIMARY = 0,
    UCOL_SECONDARY = 1,
    UCOL_TERTIARY = 2,
    UCOL_DEFAULT_STRENGTH = UCOL_TERTIARY,
    UCOL_CE_STRENGTH_LIMIT,
    UCOL_QUATERNARY=3,
    UCOL_IDENTICAL=15,
    UCOL_STRENGTH_LIMIT,
    UCOL_OFF = 16,
    UCOL_ON = 17,
    UCOL_SHIFTED = 20,
    UCOL_NON_IGNORABLE = 21,
    UCOL_LOWER_FIRST = 24,
    UCOL_UPPER_FIRST = 25,
} UColAttributeValue;
#pragma clang diagnostic pop

typedef UColAttributeValue UCollationStrength;

typedef enum UCalendarAttribute {
    UCAL_LENIENT,
    UCAL_FIRST_DAY_OF_WEEK,
    UCAL_MINIMAL_DAYS_IN_FIRST_WEEK,
    UCAL_REPEATED_WALL_TIME,
    UCAL_SKIPPED_WALL_TIME
} UCalendarAttribute;

typedef enum UCalendarLimitType {
    UCAL_MINIMUM,
    UCAL_MAXIMUM,
    UCAL_GREATEST_MINIMUM,
    UCAL_LEAST_MAXIMUM,
    UCAL_ACTUAL_MINIMUM,
    UCAL_ACTUAL_MAXIMUM
} UCalendarLimitType;

typedef enum UCalendarDisplayNameType {
    UCAL_STANDARD,
    UCAL_SHORT_STANDARD,
    UCAL_DST,
    UCAL_SHORT_DST
} UCalendarDisplayNameType;

typedef enum ULayoutType {
    ULOC_LAYOUT_LTR = 0,
    ULOC_LAYOUT_RTL = 1,
    ULOC_LAYOUT_TTB = 2,
    ULOC_LAYOUT_BTT = 3,
    ULOC_LAYOUT_UNKNOWN
} ULayoutType;

typedef enum UDialectHandling {
    ULDN_STANDARD_NAMES = 0,
    ULDN_DIALECT_NAMES
} UDialectHandling;

typedef enum UMeasurementSystem {
    UMS_SI,
    UMS_US,
    UMS_UK,
} UMeasurementSystem;

typedef enum UNumberFormatSymbol {
    UNUM_DECIMAL_SEPARATOR_SYMBOL = 0,
    UNUM_GROUPING_SEPARATOR_SYMBOL = 1,
    UNUM_PATTERN_SEPARATOR_SYMBOL = 2,
    UNUM_PERCENT_SYMBOL = 3,
    UNUM_ZERO_DIGIT_SYMBOL = 4,
    UNUM_DIGIT_SYMBOL = 5,
    UNUM_MINUS_SIGN_SYMBOL = 6,
    UNUM_PLUS_SIGN_SYMBOL = 7,
    UNUM_CURRENCY_SYMBOL = 8,
    UNUM_INTL_CURRENCY_SYMBOL = 9,
    UNUM_MONETARY_SEPARATOR_SYMBOL = 10,
    UNUM_EXPONENTIAL_SYMBOL = 11,
    UNUM_PERMILL_SYMBOL = 12,
    UNUM_PAD_ESCAPE_SYMBOL = 13,
    UNUM_INFINITY_SYMBOL = 14,
    UNUM_NAN_SYMBOL = 15,
    UNUM_SIGNIFICANT_DIGIT_SYMBOL = 16,
    UNUM_MONETARY_GROUPING_SEPARATOR_SYMBOL = 17,
    UNUM_ONE_DIGIT_SYMBOL = 18,
    UNUM_TWO_DIGIT_SYMBOL = 19,
    UNUM_THREE_DIGIT_SYMBOL = 20,
    UNUM_FOUR_DIGIT_SYMBOL = 21,
    UNUM_FIVE_DIGIT_SYMBOL = 22,
    UNUM_SIX_DIGIT_SYMBOL = 23,
    UNUM_SEVEN_DIGIT_SYMBOL = 24,
    UNUM_EIGHT_DIGIT_SYMBOL = 25,
    UNUM_NINE_DIGIT_SYMBOL = 26,
    UNUM_EXPONENT_MULTIPLICATION_SYMBOL = 27,
} UNumberFormatSymbol;

typedef enum UNumberFormatAttribute {
    UNUM_PARSE_INT_ONLY,
    UNUM_GROUPING_USED,
    UNUM_DECIMAL_ALWAYS_SHOWN,
    UNUM_MAX_INTEGER_DIGITS,
    UNUM_MIN_INTEGER_DIGITS,
    UNUM_INTEGER_DIGITS,
    UNUM_MAX_FRACTION_DIGITS,
    UNUM_MIN_FRACTION_DIGITS,
    UNUM_FRACTION_DIGITS,
    UNUM_MULTIPLIER,
    UNUM_GROUPING_SIZE,
    UNUM_ROUNDING_MODE,
    UNUM_ROUNDING_INCREMENT,
    UNUM_FORMAT_WIDTH,
    UNUM_PADDING_POSITION,
    UNUM_SECONDARY_GROUPING_SIZE,
    UNUM_SIGNIFICANT_DIGITS_USED,
    UNUM_MIN_SIGNIFICANT_DIGITS,
    UNUM_MAX_SIGNIFICANT_DIGITS,
    UNUM_LENIENT_PARSE,
    UNUM_SCALE = 21,
    UNUM_CURRENCY_USAGE = 23,
    UNUM_MAX_NONBOOLEAN_ATTRIBUTE = 0x0FFF,
    UNUM_FORMAT_FAIL_IF_MORE_THAN_MAX_DIGITS = 0x1000,
    UNUM_PARSE_NO_EXPONENT,
    UNUM_PARSE_DECIMAL_MARK_REQUIRED = 0x1002,
    UNUM_LIMIT_BOOLEAN_ATTRIBUTE = 0x1003
} UNumberFormatAttribute;

typedef enum UNumberFormatStyle {
    UNUM_PATTERN_DECIMAL = 0,
    UNUM_DECIMAL = 1,
    UNUM_CURRENCY = 2,
    UNUM_PERCENT = 3,
    UNUM_SCIENTIFIC = 4,
    UNUM_SPELLOUT = 5,
    UNUM_ORDINAL = 6,
    UNUM_DURATION = 7,
    UNUM_NUMBERING_SYSTEM = 8,
    UNUM_PATTERN_RULEBASED = 9,
    UNUM_CURRENCY_ISO = 10,
    UNUM_CURRENCY_PLURAL = 11,
    UNUM_CURRENCY_ACCOUNTING = 12,
    UNUM_CASH_CURRENCY = 13,
    UNUM_DECIMAL_COMPACT_SHORT = 14,
    UNUM_DECIMAL_COMPACT_LONG = 15,
    UNUM_CURRENCY_STANDARD = 16,
    UNUM_DEFAULT = UNUM_DECIMAL,
    UNUM_IGNORE = UNUM_PATTERN_DECIMAL
} UNumberFormatStyle;

typedef enum UScriptCode {
    USCRIPT_UNKNOWN                       = 103,
} UScriptCode;

typedef enum UColReorderCode {
    UCOL_REORDER_CODE_DEFAULT = -1,
    UCOL_REORDER_CODE_NONE = USCRIPT_UNKNOWN,
    UCOL_REORDER_CODE_OTHERS = USCRIPT_UNKNOWN,
    UCOL_REORDER_CODE_SPACE = 0x1000,
    UCOL_REORDER_CODE_FIRST = UCOL_REORDER_CODE_SPACE,
    UCOL_REORDER_CODE_PUNCTUATION = 0x1001,
    UCOL_REORDER_CODE_SYMBOL = 0x1002,
    UCOL_REORDER_CODE_CURRENCY = 0x1003,
    UCOL_REORDER_CODE_DIGIT = 0x1004,
} UColReorderCode;

typedef enum UDateFormatStyle {
    UDAT_FULL,
    UDAT_LONG,
    UDAT_MEDIUM,
    UDAT_SHORT,
    UDAT_DEFAULT = UDAT_MEDIUM,
    UDAT_RELATIVE = (1 << 7),
    UDAT_FULL_RELATIVE = UDAT_FULL | UDAT_RELATIVE,
    UDAT_LONG_RELATIVE = UDAT_LONG | UDAT_RELATIVE,
    UDAT_MEDIUM_RELATIVE = UDAT_MEDIUM | UDAT_RELATIVE,
    UDAT_SHORT_RELATIVE = UDAT_SHORT | UDAT_RELATIVE,
    UDAT_NONE = -1,
    UDAT_PATTERN = -2,
} UDateFormatStyle;

typedef enum UDateFormatSymbolType {
    UDAT_ERAS,
    UDAT_MONTHS,
    UDAT_SHORT_MONTHS,
    UDAT_WEEKDAYS,
    UDAT_SHORT_WEEKDAYS,
    UDAT_AM_PMS,
    UDAT_LOCALIZED_CHARS,
    UDAT_ERA_NAMES,
    UDAT_NARROW_MONTHS,
    UDAT_NARROW_WEEKDAYS,
    UDAT_STANDALONE_MONTHS,
    UDAT_STANDALONE_SHORT_MONTHS,
    UDAT_STANDALONE_NARROW_MONTHS,
    UDAT_STANDALONE_WEEKDAYS,
    UDAT_STANDALONE_SHORT_WEEKDAYS,
    UDAT_STANDALONE_NARROW_WEEKDAYS,
    UDAT_QUARTERS,
    UDAT_SHORT_QUARTERS,
    UDAT_STANDALONE_QUARTERS,
    UDAT_STANDALONE_SHORT_QUARTERS,
    UDAT_SHORTER_WEEKDAYS,
    UDAT_STANDALONE_SHORTER_WEEKDAYS,
    UDAT_CYCLIC_YEARS_WIDE,
    UDAT_CYCLIC_YEARS_ABBREVIATED,
    UDAT_CYCLIC_YEARS_NARROW,
    UDAT_ZODIAC_NAMES_WIDE,
    UDAT_ZODIAC_NAMES_ABBREVIATED,
    UDAT_ZODIAC_NAMES_NARROW
} UDateFormatSymbolType;

typedef enum UCurrNameStyle {
    UCURR_SYMBOL_NAME,
    UCURR_LONG_NAME
} UCurrNameStyle;

typedef enum UCalendarType {
    UCAL_TRADITIONAL,
    UCAL_DEFAULT = UCAL_TRADITIONAL,
    UCAL_GREGORIAN
} UCalendarType;

typedef enum UCollationResult {
    UCOL_EQUAL = 0,
    UCOL_GREATER = 1,
    UCOL_LESS = -1
} UCollationResult;

typedef enum USystemTimeZoneType {
    UCAL_ZONE_TYPE_ANY,
    UCAL_ZONE_TYPE_CANONICAL,
    UCAL_ZONE_TYPE_CANONICAL_LOCATION
} USystemTimeZoneType;

enum {
    UIDNA_ERROR_EMPTY_LABEL = 1,
    UIDNA_ERROR_LABEL_TOO_LONG = 2,
    UIDNA_ERROR_DOMAIN_NAME_TOO_LONG = 4,
    UIDNA_ERROR_LEADING_HYPHEN = 8,
    UIDNA_ERROR_TRAILING_HYPHEN = 0x10,
    UIDNA_ERROR_HYPHEN_3_4 = 0x20,
    UIDNA_ERROR_LEADING_COMBINING_MARK = 0x40,
    UIDNA_ERROR_DISALLOWED = 0x80,
    UIDNA_ERROR_PUNYCODE = 0x100,
    UIDNA_ERROR_LABEL_HAS_DOT = 0x200,
    UIDNA_ERROR_INVALID_ACE_LABEL = 0x400,
    UIDNA_ERROR_BIDI = 0x800,
    UIDNA_ERROR_CONTEXTJ = 0x1000,
    UIDNA_ERROR_CONTEXTO_PUNCTUATION = 0x2000,
    UIDNA_ERROR_CONTEXTO_DIGITS = 0x4000
};

enum {
    UIDNA_DEFAULT = 0,
    UIDNA_ALLOW_UNASSIGNED = 1,
    UIDNA_USE_STD3_RULES = 2,
    UIDNA_CHECK_BIDI = 4,
    UIDNA_CHECK_CONTEXTJ = 8,
    UIDNA_NONTRANSITIONAL_TO_ASCII = 0x10,
    UIDNA_NONTRANSITIONAL_TO_UNICODE = 0x20,
    UIDNA_CHECK_CONTEXTO = 0x40
};

enum {
    U_PARSE_CONTEXT_LEN = 16
};

typedef struct UParseError {
    int32_t line;
    int32_t offset;
    UChar preContext[U_PARSE_CONTEXT_LEN];
    UChar postContext[U_PARSE_CONTEXT_LEN];

} UParseError;

typedef struct UIDNAInfo {
    int16_t size;
    UBool isTransitionalDifferent;
    UBool reservedB3;
    uint32_t errors;
    int32_t reservedI2;
    int32_t reservedI3;
} UIDNAInfo;

typedef struct UFieldPosition {
    int32_t field;
    int32_t beginIndex;
    int32_t endIndex;
} UFieldPosition;

void u_charsToUChars(const char * cs, UChar * us, int32_t length);
void u_getVersion(UVersionInfo versionArray);
int32_t u_strlen(const UChar * s);
int32_t u_strcmp(const UChar * s1, const UChar * s2);
UChar * u_strcpy(UChar * dst, const UChar * src);
UChar * u_strncpy(UChar * dst, const UChar * src, int32_t n);
UChar32 u_tolower(UChar32 c);
UChar32 u_toupper(UChar32 c);
UChar* u_uastrncpy(UChar * dst, const char * src, int32_t n);
void ubrk_close(UBreakIterator * bi);
UBreakIterator* ubrk_openRules(const UChar * rules, int32_t rulesLength, const UChar * text, int32_t textLength, UParseError * parseErr, UErrorCode * status);
void ucal_add(UCalendar * cal, UCalendarDateFields field, int32_t amount, UErrorCode * status);
void ucal_close(UCalendar * cal);
int32_t ucal_get(const UCalendar * cal, UCalendarDateFields field, UErrorCode * status);
int32_t ucal_getAttribute(const UCalendar * cal, UCalendarAttribute attr);
UEnumeration * ucal_getKeywordValuesForLocale(const char * key, const char * locale, UBool commonlyUsed, UErrorCode * status);
int32_t ucal_getLimit(const UCalendar * cal, UCalendarDateFields field, UCalendarLimitType type, UErrorCode * status);
UDate ucal_getNow(void);
int32_t ucal_getTimeZoneDisplayName(const UCalendar * cal, UCalendarDisplayNameType type, const char * locale, UChar * result, int32_t resultLength, UErrorCode * status);
int32_t ucal_getTimeZoneIDForWindowsID(const UChar * winid, int32_t	len, const char * region, UChar * id, int32_t idCapacity, UErrorCode * status);
int32_t ucal_getWindowsTimeZoneID(const UChar *	id, int32_t	len, UChar * winid, int32_t	winidCapacity, UErrorCode * status);
UCalendar * ucal_open(const UChar * zoneID, int32_t len, const char * locale, UCalendarType type, UErrorCode * status);
UEnumeration * ucal_openTimeZoneIDEnumeration(USystemTimeZoneType zoneType, const char * region, const int32_t * rawOffset, UErrorCode * ec);
void ucal_set(UCalendar * cal, UCalendarDateFields field, int32_t value);
void ucal_setMillis(UCalendar * cal, UDate dateTime, UErrorCode * status);
void ucol_close(UCollator * coll);
void ucol_closeElements(UCollationElements * elems);
int32_t ucol_getOffset(const UCollationElements *elems);
const UChar * ucol_getRules(const UCollator * coll, int32_t * length);
int32_t ucol_getSortKey(const UCollator * coll, const UChar * source, int32_t sourceLength, uint8_t * result, int32_t resultLength);
UCollationStrength ucol_getStrength(const UCollator * coll);
void ucol_getVersion(const UCollator * coll, UVersionInfo info);
int32_t ucol_next(UCollationElements * elems, UErrorCode * status);
int32_t ucol_previous(UCollationElements * elems, UErrorCode * status);
UCollator * ucol_open(const char * loc, UErrorCode * status);
UCollationElements * ucol_openElements(const UCollator * coll, const UChar * text, int32_t textLength, UErrorCode * status);
UCollator * ucol_openRules(const UChar * rules, int32_t rulesLength, UColAttributeValue normalizationMode, UCollationStrength strength, UParseError * parseError, UErrorCode * status);
UCollator * ucol_safeClone(const UCollator * coll, void * stackBuffer, int32_t * pBufferSize, UErrorCode * status);
void ucol_setAttribute(UCollator * coll, UColAttribute attr, UColAttributeValue value, UErrorCode * status);
UCollationResult ucol_strcoll(const UCollator * coll, const UChar * source, int32_t sourceLength, const UChar * target, int32_t targetLength);
int32_t ucurr_forLocale(const char * locale, UChar * buff, int32_t buffCapacity, UErrorCode * ec);
const UChar * ucurr_getName(const UChar * currency, const char * locale, UCurrNameStyle nameStyle, UBool * isChoiceFormat, int32_t * len, UErrorCode * ec);
void udat_close(UDateFormat * format);
int32_t udat_countSymbols(const UDateFormat * fmt, UDateFormatSymbolType type);
int32_t udat_format(const UDateFormat * format, UDate dateToFormat, UChar * result, int32_t resultLength, UFieldPosition * position, UErrorCode * status);
int32_t udat_getSymbols(const UDateFormat * fmt, UDateFormatSymbolType type, int32_t symbolIndex, UChar * result, int32_t resultLength, UErrorCode * status);
UDateFormat * udat_open(UDateFormatStyle timeStyle, UDateFormatStyle dateStyle, const char * locale, const UChar * tzID, int32_t tzIDLength, const UChar * pattern, int32_t patternLength, UErrorCode * status);
void udat_setCalendar(UDateFormat * fmt, const UCalendar * calendarToSet);
int32_t udat_toPattern(const UDateFormat * fmt, UBool localized, UChar * result, int32_t resultLength, UErrorCode * status);
void udatpg_close(UDateTimePatternGenerator * dtpg);
int32_t udatpg_getBestPattern(UDateTimePatternGenerator * dtpg, const UChar * skeleton, int32_t length, UChar * bestPattern, int32_t capacity, UErrorCode * pErrorCode);
UDateTimePatternGenerator * udatpg_open(const char * locale, UErrorCode * pErrorCode);
void uenum_close(UEnumeration * en);
int32_t uenum_count(UEnumeration * en, UErrorCode * status);
const char * uenum_next(UEnumeration * en, int32_t * resultLength, UErrorCode * status);
void uidna_close(UIDNA * idna);
int32_t uidna_nameToASCII(const UIDNA * idna, const UChar * name, int32_t length, UChar * dest, int32_t capacity, UIDNAInfo * pInfo, UErrorCode * pErrorCode);
int32_t uidna_nameToUnicode(const UIDNA * idna, const UChar * name, int32_t length, UChar * dest, int32_t capacity, UIDNAInfo * pInfo, UErrorCode * pErrorCode);
UIDNA * uidna_openUTS46(uint32_t options, UErrorCode * pErrorCode);
void uldn_close(ULocaleDisplayNames * ldn);
int32_t uldn_keyValueDisplayName(const ULocaleDisplayNames * ldn, const char * key, const char * value, UChar * result, int32_t maxResultSize, UErrorCode * pErrorCode);
ULocaleDisplayNames * uldn_open(const char * locale, UDialectHandling dialectHandling, UErrorCode * pErrorCode);
int32_t uloc_canonicalize(const char * localeID, char * name, int32_t nameCapacity, UErrorCode * err);
int32_t uloc_countAvailable(void);
const char * uloc_getAvailable(int32_t n);
int32_t uloc_getBaseName(const char * localeID, char * name, int32_t nameCapacity, UErrorCode * err);
ULayoutType uloc_getCharacterOrientation(const char * localeId, UErrorCode * status);
int32_t uloc_getCountry(const char * localeID, char * country, int32_t countryCapacity, UErrorCode * err);
const char * uloc_getDefault(void);
int32_t uloc_getDisplayCountry(const char * locale, const char * displayLocale, UChar * country, int32_t countryCapacity, UErrorCode * status);
int32_t uloc_getDisplayLanguage(const char * locale, const char * displayLocale, UChar * language, int32_t languageCapacity, UErrorCode * status);
int32_t uloc_getDisplayName(const char * localeID, const char * inLocaleID, UChar * result, int32_t maxResultSize, UErrorCode * err);
const char * uloc_getISO3Country(const char * localeID);
const char * uloc_getISO3Language(const char * localeID);
int32_t uloc_getKeywordValue(const char * localeID, const char * keywordName, char * buffer, int32_t bufferCapacity, UErrorCode * status);
int32_t uloc_getLanguage(const char * localeID, char * language, int32_t languageCapacity, UErrorCode * err);
uint32_t uloc_getLCID(const char * localeID);
int32_t uloc_getName(const char * localeID, char * name, int32_t nameCapacity, UErrorCode * err);
int32_t uloc_getParent(const char * localeID, char * parent, int32_t parentCapacity, UErrorCode * err);
int32_t uloc_setKeywordValue(const char * keywordName, const char * keywordValue, char * buffer, int32_t bufferCapacity, UErrorCode * status);
void ulocdata_getCLDRVersion(UVersionInfo versionArray, UErrorCode * status);
UMeasurementSystem ulocdata_getMeasurementSystem(const char * localeID, UErrorCode * status);
const UNormalizer2 * unorm2_getNFCInstance(UErrorCode * pErrorCode);
const UNormalizer2 * unorm2_getNFDInstance(UErrorCode * pErrorCode);
const UNormalizer2 * unorm2_getNFKCInstance(UErrorCode * pErrorCode);
const UNormalizer2 * unorm2_getNFKDInstance(UErrorCode * pErrorCode);
UBool unorm2_isNormalized(const UNormalizer2 * norm2, const UChar * s, int32_t length, UErrorCode * pErrorCode);
int32_t unorm2_normalize(const UNormalizer2 * norm2, const UChar * src, int32_t length, UChar * dest, int32_t capacity, UErrorCode * pErrorCode);
void unum_close(UNumberFormat * fmt);
int32_t unum_getAttribute(const UNumberFormat * fmt, UNumberFormatAttribute attr);
int32_t unum_getSymbol(const UNumberFormat * fmt, UNumberFormatSymbol symbol, UChar * buffer, int32_t size, UErrorCode * status);
UNumberFormat * unum_open(UNumberFormatStyle style, const UChar * pattern, int32_t patternLength, const char * locale, UParseError * parseErr, UErrorCode * status);
int32_t unum_toPattern(const UNumberFormat * fmt, UBool isPatternLocalized, UChar * result, int32_t resultLength, UErrorCode * status);
void ures_close(UResourceBundle * resourceBundle);
UResourceBundle * ures_getByKey(const UResourceBundle * resourceBundle, const char * key, UResourceBundle * fillIn, UErrorCode * status);
int32_t ures_getSize(const UResourceBundle * resourceBundle);
const UChar * ures_getStringByIndex(const UResourceBundle * resourceBundle, int32_t indexS, int32_t * len, UErrorCode * status);
UResourceBundle * ures_open(const char * packageName, const char * locale, UErrorCode * status);
void usearch_close(UStringSearch * searchiter);
int32_t usearch_first(UStringSearch * strsrch, UErrorCode * status);
const UBreakIterator* usearch_getBreakIterator(const UStringSearch * strsrch);
int32_t usearch_getMatchedLength(const UStringSearch * strsrch);
int32_t usearch_last(UStringSearch * strsrch, UErrorCode * status);
UStringSearch * usearch_openFromCollator(const UChar * pattern, int32_t patternlength, const UChar * text, int32_t textlength, const UCollator * collator, UBreakIterator * breakiter, UErrorCode * status);
void usearch_setPattern(UStringSearch * strsrch, const UChar * pattern, int32_t patternlength, UErrorCode * status);
void usearch_setText(UStringSearch * strsrch, const UChar * text, int32_t textlength, UErrorCode * status);
void ucol_setMaxVariable(UCollator * coll, UColReorderCode group, UErrorCode * pErrorCode);
