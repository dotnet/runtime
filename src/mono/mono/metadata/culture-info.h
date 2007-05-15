
#ifndef _MONO_METADATA_CULTURE_INFO_H_
#define _MONO_METADATA_CULTURE_INFO_H_ 1

#include <glib.h>

#define NUM_DAYS 7
#define NUM_MONTHS 13
#define GROUP_SIZE 5
#define NUM_OPT_CALS 5

#define NUM_SHORT_DATE_PATTERNS 14
#define NUM_LONG_DATE_PATTERNS 8
#define NUM_SHORT_TIME_PATTERNS 11
#define NUM_LONG_TIME_PATTERNS 10

#define idx2string(idx) (locale_strings + (idx))

/* need to change this if the string data ends up to not fit in a 64KB array. */
typedef guint16 stridx_t;

typedef struct {
	const stridx_t full_date_time_pattern;
	const stridx_t long_date_pattern;
	const stridx_t short_date_pattern;
	const stridx_t long_time_pattern;
	const stridx_t short_time_pattern;
	const stridx_t year_month_pattern;
	const stridx_t month_day_pattern;

	const stridx_t am_designator;
	const stridx_t pm_designator;

	const stridx_t day_names [NUM_DAYS]; 
	const stridx_t abbreviated_day_names [NUM_DAYS];
	const stridx_t month_names [NUM_MONTHS];
	const stridx_t abbreviated_month_names [NUM_MONTHS];

	gint8 calendar_week_rule;
	gint8 first_day_of_week;

	const stridx_t date_separator;
	const stridx_t time_separator;	

	const stridx_t short_date_patterns [NUM_SHORT_DATE_PATTERNS];
	const stridx_t long_date_patterns [NUM_LONG_DATE_PATTERNS];
	const stridx_t short_time_patterns [NUM_SHORT_TIME_PATTERNS];
	const stridx_t long_time_patterns [NUM_LONG_TIME_PATTERNS];
} DateTimeFormatEntry;

typedef struct {
	const stridx_t currency_decimal_separator;
	const stridx_t currency_group_separator;
	const stridx_t percent_decimal_separator;
	const stridx_t percent_group_separator;
	const stridx_t number_decimal_separator;
	const stridx_t number_group_separator;

	const stridx_t currency_symbol;
	const stridx_t percent_symbol;
	const stridx_t nan_symbol;
	const stridx_t per_mille_symbol;
	const stridx_t negative_infinity_symbol;
	const stridx_t positive_infinity_symbol;

	const stridx_t negative_sign;
	const stridx_t positive_sign;

	gint8 currency_negative_pattern;
	gint8 currency_positive_pattern;
	gint8 percent_negative_pattern;
	gint8 percent_positive_pattern;
	gint8 number_negative_pattern;

	gint8 currency_decimal_digits;
	gint8 percent_decimal_digits;
	gint8 number_decimal_digits;

	const gint currency_group_sizes [GROUP_SIZE];
	const gint percent_group_sizes [GROUP_SIZE];
	const gint number_group_sizes [GROUP_SIZE];	
} NumberFormatEntry;

typedef struct {
	const gint ansi;
	const gint ebcdic;
	const gint mac;
	const gint oem;
	const char list_sep;
} TextInfoEntry;

typedef struct {
	gint16 lcid;
	gint16 parent_lcid;
	gint16 specific_lcid;
	gint16 region_entry_index;
	const stridx_t name;
	const stridx_t icu_name;
	const stridx_t englishname;
	const stridx_t displayname;
	const stridx_t nativename;
	const stridx_t win3lang;
	const stridx_t iso3lang;
	const stridx_t iso2lang;
	const stridx_t territory;

	gint calendar_data [NUM_OPT_CALS];

	gint16 datetime_format_index;
	gint16 number_format_index;
	
	TextInfoEntry text_info;
} CultureInfoEntry;

typedef struct {
	const stridx_t name;
	gint16 culture_entry_index;
} CultureInfoNameEntry;

typedef struct {
	gint16 lcid;
	gint16 region_id; /* it also works as geoId in 2.0 */
	/* gint8 measurement_system; // 0:metric 1:US 2:UK */
	const stridx_t iso2name;
	const stridx_t iso3name;
	const stridx_t win3name;
	const stridx_t english_name;
	const stridx_t currency_symbol;
	const stridx_t iso_currency_symbol;
	const stridx_t currency_english_name;
} RegionInfoEntry;

typedef struct {
	const stridx_t name;
	gint16 region_entry_index;
} RegionInfoNameEntry;

#endif

