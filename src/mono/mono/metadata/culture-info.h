
#ifndef _MONO_METADATA_CULTURE_INFO_H_
#define _MONO_METADATA_CULTURE_INFO_H_ 1

#include <glib.h>

#define NUM_DAYS 7
#define NUM_MONTHS 13
#define GROUP_SIZE 5
#define NUM_OPT_CALS 5

typedef struct {
	const gchar *full_date_time_pattern;
	const gchar *long_date_pattern;
	const gchar *short_date_pattern;
	const gchar *long_time_pattern;
	const gchar *short_time_pattern;
	const gchar *year_month_pattern;
	const gchar *month_day_pattern;

	const gchar *am_designator;
	const gchar *pm_designator;

	const gchar *day_names [NUM_DAYS]; 
	const gchar *abbreviated_day_names [NUM_DAYS];
	const gchar *month_names [NUM_MONTHS];
	const gchar *abbreviated_month_names [NUM_MONTHS];

	gint calendar_week_rule;
	gint first_day_of_week;

	const gchar *date_separator;
	const gchar *time_separator;	
} DateTimeFormatEntry;

typedef struct {
	const gchar *currency_decimal_separator;
	const gchar *currency_group_separator;
	const gchar *percent_decimal_separator;
	const gchar *percent_group_separator;
	const gchar *number_decimal_separator;
	const gchar *number_group_separator;

	const gchar *currency_symbol;
	const gchar *percent_symbol;
	const gchar *nan_symbol;
	const gchar *per_mille_symbol;
	const gchar *negative_infinity_symbol;
	const gchar *positive_infinity_symbol;

	const gchar *negative_sign;
	const gchar *positive_sign;

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
	const gchar *name;
	const gchar *icu_name;
	const gchar *englishname;
	const gchar *displayname;
	const gchar *nativename;
	const gchar *win3lang;
	const gchar *iso3lang;
	const gchar *iso2lang;

	gint calendar_data [NUM_OPT_CALS];

	gint datetime_format_index;
	gint number_format_index;
} CultureInfoEntry;

typedef struct {
	const gchar *name;
	gint culture_entry_index;
} CultureInfoNameEntry;

#endif

