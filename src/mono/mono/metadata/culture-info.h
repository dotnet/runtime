/**
 * \file
 */

#ifndef _MONO_METADATA_CULTURE_INFO_H_
#define _MONO_METADATA_CULTURE_INFO_H_ 1

#include <glib.h>
#include <mono/metadata/object.h>

#define NUM_DAYS 7
#define NUM_MONTHS 13
#define GROUP_SIZE 2
#define NUM_CALENDARS 4

#define NUM_SHORT_DATE_PATTERNS 14
#define NUM_LONG_DATE_PATTERNS 10
#define NUM_SHORT_TIME_PATTERNS 12
#define NUM_LONG_TIME_PATTERNS 9
#define NUM_YEAR_MONTH_PATTERNS 8

#define idx2string(idx) (locale_strings + (idx))
#define pattern2string(idx) (patterns + (idx))
#define dtidx2string(idx) (datetime_strings + (idx))

/* need to change this if the string data ends up to not fit in a 64KB array. */
typedef guint16 stridx_t;

typedef struct {
	stridx_t month_day_pattern;
	stridx_t am_designator;
	stridx_t pm_designator;

	stridx_t day_names [NUM_DAYS];
	stridx_t abbreviated_day_names [NUM_DAYS];
	stridx_t shortest_day_names [NUM_DAYS];
	stridx_t month_names [NUM_MONTHS];
	stridx_t month_genitive_names [NUM_MONTHS];
	stridx_t abbreviated_month_names [NUM_MONTHS];
	stridx_t abbreviated_month_genitive_names [NUM_MONTHS];

	gint8 calendar_week_rule;
	gint8 first_day_of_week;

	stridx_t date_separator;
	stridx_t time_separator;

	stridx_t short_date_patterns [NUM_SHORT_DATE_PATTERNS];
	stridx_t long_date_patterns [NUM_LONG_DATE_PATTERNS];
	stridx_t short_time_patterns [NUM_SHORT_TIME_PATTERNS];
	stridx_t long_time_patterns [NUM_LONG_TIME_PATTERNS];
	stridx_t year_month_patterns [NUM_YEAR_MONTH_PATTERNS];
} DateTimeFormatEntry;

typedef struct {
	// 12x ushort -- 6 ints
	stridx_t currency_decimal_separator;
	stridx_t currency_group_separator;
	stridx_t number_decimal_separator;
	stridx_t number_group_separator;

	stridx_t currency_symbol;
	stridx_t percent_symbol;
	stridx_t nan_symbol;
	stridx_t per_mille_symbol;
	stridx_t negative_infinity_symbol;
	stridx_t positive_infinity_symbol;

	stridx_t negative_sign;
	stridx_t positive_sign;

	// 7x gint8 -- FIXME expand to 8, or sort by size.
	// For this reason, copy the data to a simpler "managed" form.
	gint8 currency_negative_pattern;
	gint8 currency_positive_pattern;
	gint8 percent_negative_pattern;
	gint8 percent_positive_pattern;
	gint8 number_negative_pattern;

	gint8 currency_decimal_digits;
	gint8 number_decimal_digits;

	gint currency_group_sizes [2];
	gint number_group_sizes [2];
} NumberFormatEntry;

// Due to the questionable layout of NumberFormatEntry, in particular
// 7x byte, make something more guaranteed to match between native and managed.
// mono/metadta/culture-info.h NumberFormatEntryManaged must match
// mcs/class/corlib/ReferenceSources/CultureData.cs NumberFormatEntryManaged.
// This is sorted alphabetically.
struct NumberFormatEntryManaged {
	gint32 currency_decimal_digits;
	gint32 currency_decimal_separator;
	gint32 currency_group_separator;
	gint32 currency_group_sizes0;
	gint32 currency_group_sizes1;
	gint32 currency_negative_pattern;
	gint32 currency_positive_pattern;
	gint32 currency_symbol;
	gint32 nan_symbol;
	gint32 negative_infinity_symbol;
	gint32 negative_sign;
	gint32 number_decimal_digits;
	gint32 number_decimal_separator;
	gint32 number_group_separator;
	gint32 number_group_sizes0;
	gint32 number_group_sizes1;
	gint32 number_negative_pattern;
	gint32 per_mille_symbol;
	gint32 percent_negative_pattern;
	gint32 percent_positive_pattern;
	gint32 percent_symbol;
	gint32 positive_infinity_symbol;
	gint32 positive_sign;
};

typedef struct {
	gint ansi;
	gint ebcdic;
	gint mac;
	gint oem;
	MonoBoolean is_right_to_left;
	char list_sep;
} TextInfoEntry;

typedef struct {
	gint16 lcid;
	gint16 parent_lcid;
	gint16 calendar_type;
	gint16 region_entry_index;
	stridx_t name;
	stridx_t englishname;
	stridx_t nativename;
	stridx_t win3lang;
	stridx_t iso3lang;
	stridx_t iso2lang;
	stridx_t territory;
	stridx_t native_calendar_names [NUM_CALENDARS];

	gint16 datetime_format_index;
	gint16 number_format_index;

	TextInfoEntry text_info;
} CultureInfoEntry;

typedef struct {
	stridx_t name;
	gint16 culture_entry_index;
} CultureInfoNameEntry;

typedef struct {
	gint16 geo_id;
	stridx_t iso2name;
	stridx_t iso3name;
	stridx_t win3name;
	stridx_t english_name;
	stridx_t native_name;
	stridx_t currency_symbol;
	stridx_t iso_currency_symbol;
	stridx_t currency_english_name;
	stridx_t currency_native_name;
} RegionInfoEntry;

typedef struct {
	stridx_t name;
	gint16 region_entry_index;
} RegionInfoNameEntry;

#endif

