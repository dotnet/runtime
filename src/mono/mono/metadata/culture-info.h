
#ifndef _MONO_METADATA_CULTURE_INFO_H_
#define _MONO_METADATA_CULTURE_INFO_H_ 1

#include <glib.h>

#define NUM_DAYS 7
#define NUM_MONTHS 13
#define GROUP_SIZE 5
#define NUM_OPT_CALS 5

#define NUM_SHORT_DATE_PATTERNS 14
#define NUM_LONG_DATE_PATTERNS 8
#define NUM_SHORT_TIME_PATTERNS 5
#define NUM_LONG_TIME_PATTERNS 6

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

	gint calendar_week_rule;
	gint first_day_of_week;

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

	gint currency_negative_pattern;
	gint currency_positive_pattern;
	gint percent_negative_pattern;
	gint percent_positive_pattern;
	gint number_negative_pattern;

	gint currency_decimal_digits;
	gint percent_decimal_digits;
	gint number_decimal_digits;

	const gint currency_group_sizes [GROUP_SIZE];
	const gint percent_group_sizes [GROUP_SIZE];
	const gint number_group_sizes [GROUP_SIZE];	
} NumberFormatEntry;

typedef struct {
	gint lcid;
	gint parent_lcid;
	gint specific_lcid;
	const stridx_t name;
	const stridx_t icu_name;
	const stridx_t englishname;
	const stridx_t displayname;
	const stridx_t nativename;
	const stridx_t win3lang;
	const stridx_t iso3lang;
	const stridx_t iso2lang;

	gint calendar_data [NUM_OPT_CALS];

	gint16 datetime_format_index;
	gint16 number_format_index;
} CultureInfoEntry;

typedef struct {
	const stridx_t name;
	gint16 culture_entry_index;
} CultureInfoNameEntry;

#endif

