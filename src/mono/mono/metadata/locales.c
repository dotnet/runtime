/**
 * \file
 * Culture-sensitive handling
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *	Mohammad DAMT (mdamt@cdl2000.com)
 *	Marek Safar (marek.safar@gmail.com)
 *
 * Copyright 2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * (C) 2003 PT Cakram Datalingga Duaribu  http://www.cdl2000.com
 * Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/locales.h>
#include <mono/metadata/culture-info.h>
#include <mono/metadata/culture-info-tables.h>
#include <mono/utils/bsearch.h>

#ifndef DISABLE_NORMALIZATION
#include <mono/metadata/normalization-tables.h>
#endif

#include <locale.h>
#if defined(__APPLE__)
#include <CoreFoundation/CoreFoundation.h>
#endif

#undef DEBUG

static gint32 string_invariant_compare_char (gunichar2 c1, gunichar2 c2,
					     gint32 options);
static gint32 string_invariant_compare (MonoString *str1, gint32 off1,
					gint32 len1, MonoString *str2,
					gint32 off2, gint32 len2,
					gint32 options);
static gint32 string_invariant_indexof (MonoString *source, gint32 sindex,
					gint32 count, MonoString *value,
					MonoBoolean first);
static gint32 string_invariant_indexof_char (MonoString *source, gint32 sindex,
					     gint32 count, gunichar2 value,
					     MonoBoolean first);

static const CultureInfoEntry* culture_info_entry_from_lcid (int lcid);

static const RegionInfoEntry* region_info_entry_from_lcid (int lcid);

/* Lazy class loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (culture_info, "System.Globalization", "CultureInfo")

static int
culture_lcid_locator (const void *a, const void *b)
{
	const int *lcid = (const int *)a;
	const CultureInfoEntry *bb = (const CultureInfoEntry *)b;

	return *lcid - bb->lcid;
}

static int
culture_name_locator (const void *a, const void *b)
{
	const char *aa = (const char *)a;
	const CultureInfoNameEntry *bb = (const CultureInfoNameEntry *)b;
	int ret;
	
	ret = strcmp (aa, idx2string (bb->name));

	return ret;
}

static int
region_name_locator (const void *a, const void *b)
{
	const char *aa = (const char *)a;
	const RegionInfoNameEntry *bb = (const RegionInfoNameEntry *)b;
	int ret;
	
	ret = strcmp (aa, idx2string (bb->name));

	return ret;
}

static MonoArray*
create_group_sizes_array (const gint *gs, gint ml, MonoError *error)
{
	MonoArray *ret;
	int i, len = 0;

	error_init (error);

	for (i = 0; i < ml; i++) {
		if (gs [i] == -1)
			break;
		len++;
	}
	
	ret = mono_array_new_cached (mono_domain_get (),
				     mono_get_int32_class (), len, error);
	return_val_if_nok (error, NULL);

	for(i = 0; i < len; i++)
		mono_array_set (ret, gint32, i, gs [i]);

	return ret;
}

static MonoArray*
create_names_array_idx (const guint16 *names, int ml, MonoError *error)
{
	MonoArray *ret;
	MonoDomain *domain;
	int i;

	error_init (error);

	if (names == NULL)
		return NULL;

	domain = mono_domain_get ();

	ret = mono_array_new_cached (mono_domain_get (), mono_get_string_class (), ml, error);
	return_val_if_nok (error, NULL);

	for(i = 0; i < ml; i++) {
		MonoString *s = mono_string_new_checked (domain, dtidx2string (names [i]), error);
		return_val_if_nok (error, NULL);
		mono_array_setref (ret, i, s);
	}

	return ret;
}

static MonoArray*
create_names_array_idx_dynamic (const guint16 *names, int ml, MonoError *error)
{
	MonoArray *ret;
	MonoDomain *domain;
	int i, len = 0;

	error_init (error);

	if (names == NULL)
		return NULL;

	domain = mono_domain_get ();

	for (i = 0; i < ml; i++) {
		if (names [i] == 0)
			break;
		len++;
	}

	ret = mono_array_new_cached (mono_domain_get (), mono_get_string_class (), len, error);
	return_val_if_nok (error, NULL);

	for(i = 0; i < len; i++) {
		MonoString *s = mono_string_new_checked (domain, pattern2string (names [i]), error);
		return_val_if_nok (error, NULL);
		mono_array_setref (ret, i, s);
	}

	return ret;
}

MonoBoolean
ves_icall_System_Globalization_CalendarData_fill_calendar_data (MonoCalendarData *this_obj, MonoString *name, gint32 calendar_index)
{
	MonoError error;
	MonoDomain *domain;
	const DateTimeFormatEntry *dfe;
	const CultureInfoNameEntry *ne;
	const CultureInfoEntry *ci;
	char *n;

	n = mono_string_to_utf8_checked (name, &error);
	if (mono_error_set_pending_exception (&error))
		return FALSE;
	ne = (const CultureInfoNameEntry *)mono_binary_search (n, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);
	g_free (n);
	if (ne == NULL) {
		return FALSE;
	}

	ci = &culture_entries [ne->culture_entry_index];
	dfe = &datetime_format_entries [ci->datetime_format_index];

	domain = mono_domain_get ();

	MonoString *native_name = mono_string_new_checked (domain, idx2string (ci->nativename), &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, NativeName, native_name);
	MonoArray *short_date_patterns = create_names_array_idx_dynamic (dfe->short_date_patterns,
									 NUM_SHORT_DATE_PATTERNS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, ShortDatePatterns, short_date_patterns);
	MonoArray *year_month_patterns =create_names_array_idx_dynamic (dfe->year_month_patterns,
									NUM_YEAR_MONTH_PATTERNS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, YearMonthPatterns, year_month_patterns);

	MonoArray *long_date_patterns = create_names_array_idx_dynamic (dfe->long_date_patterns,
									NUM_LONG_DATE_PATTERNS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, LongDatePatterns, long_date_patterns);

	MonoString *month_day_pattern = mono_string_new_checked (domain, pattern2string (dfe->month_day_pattern), &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, MonthDayPattern, month_day_pattern);

	MonoArray *day_names = create_names_array_idx (dfe->day_names, NUM_DAYS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, DayNames, day_names);

	MonoArray *abbr_day_names = create_names_array_idx (dfe->abbreviated_day_names, 
							    NUM_DAYS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, AbbreviatedDayNames, abbr_day_names);

	MonoArray *ss_day_names = create_names_array_idx (dfe->shortest_day_names, NUM_DAYS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, SuperShortDayNames, ss_day_names);

	MonoArray *month_names = create_names_array_idx (dfe->month_names, NUM_MONTHS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, MonthNames, month_names);

	MonoArray *abbr_mon_names = create_names_array_idx (dfe->abbreviated_month_names,
							    NUM_MONTHS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, AbbreviatedMonthNames, abbr_mon_names);

	
	MonoArray *gen_month_names = create_names_array_idx (dfe->month_genitive_names, NUM_MONTHS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, GenitiveMonthNames, gen_month_names);

	MonoArray *gen_abbr_mon_names = create_names_array_idx (dfe->abbreviated_month_genitive_names, NUM_MONTHS, &error);
	return_val_and_set_pending_if_nok (&error, FALSE);
	MONO_OBJECT_SETREF (this_obj, GenitiveAbbreviatedMonthNames, gen_abbr_mon_names);

	return TRUE;
}

void
ves_icall_System_Globalization_CultureData_fill_culture_data (MonoCultureData *this_obj, gint32 datetime_index)
{
	MonoError error;
	MonoDomain *domain;
	const DateTimeFormatEntry *dfe;

	g_assert (datetime_index >= 0);

	dfe = &datetime_format_entries [datetime_index];

	domain = mono_domain_get ();

#define SET_STR(obj,field,domain,expr,err) do {				\
		MonoString *_tmp_str = mono_string_new_checked ((domain), (expr), (err)); \
		if (mono_error_set_pending_exception ((err)))		\
			return;						\
		MONO_OBJECT_SETREF((obj), field, _tmp_str);		\
	} while (0)

	SET_STR (this_obj, AMDesignator, domain, idx2string (dfe->am_designator), &error);
	SET_STR (this_obj, PMDesignator, domain, idx2string (dfe->pm_designator), &error);
	SET_STR (this_obj, TimeSeparator, domain, idx2string (dfe->time_separator), &error);
#undef SET_STR

	MonoArray *long_time_patterns = create_names_array_idx_dynamic (dfe->long_time_patterns,
									NUM_LONG_TIME_PATTERNS, &error);
	if (mono_error_set_pending_exception (&error))
		return;
	MONO_OBJECT_SETREF (this_obj, LongTimePatterns, long_time_patterns);

	MonoArray *short_time_patterns = create_names_array_idx_dynamic (dfe->short_time_patterns,
									 NUM_SHORT_TIME_PATTERNS, &error);
	if (mono_error_set_pending_exception (&error))
		return;
	MONO_OBJECT_SETREF (this_obj, ShortTimePatterns, short_time_patterns);
	this_obj->FirstDayOfWeek = dfe->first_day_of_week;
	this_obj->CalendarWeekRule = dfe->calendar_week_rule;
}

void
ves_icall_System_Globalization_CultureData_fill_number_data (MonoNumberFormatInfo* number, gint32 number_index)
{
	MonoError error;
	MonoDomain *domain;
	const NumberFormatEntry *nfe;

	g_assert (number_index >= 0);

	nfe = &number_format_entries [number_index];

	domain = mono_domain_get ();

	number->currencyDecimalDigits = nfe->currency_decimal_digits;

#define SET_STR(obj,field,domain,expr,err) do {				\
		MonoString *_tmp_str = mono_string_new_checked ((domain), (expr), (err)); \
		if (mono_error_set_pending_exception ((err)))		\
			return;						\
		MONO_OBJECT_SETREF((obj), field, _tmp_str);		\
	} while (0)

	SET_STR (number, currencyDecimalSeparator, domain, idx2string (nfe->currency_decimal_separator), &error);
	SET_STR (number, currencyGroupSeparator, domain, idx2string (nfe->currency_group_separator), &error);

	MonoArray *currency_sizes_arr = create_group_sizes_array (nfe->currency_group_sizes,
								  GROUP_SIZE, &error);
	if (mono_error_set_pending_exception (&error))
		return;
	MONO_OBJECT_SETREF (number, currencyGroupSizes, currency_sizes_arr);
	number->currencyNegativePattern = nfe->currency_negative_pattern;
	number->currencyPositivePattern = nfe->currency_positive_pattern;

	SET_STR (number, currencySymbol, domain, idx2string (nfe->currency_symbol), &error);
	SET_STR (number, naNSymbol, domain, idx2string (nfe->nan_symbol), &error);
	SET_STR (number, negativeInfinitySymbol, domain, idx2string (nfe->negative_infinity_symbol), &error);
	SET_STR (number, negativeSign, domain, idx2string (nfe->negative_sign), &error);
	number->numberDecimalDigits = nfe->number_decimal_digits;
	SET_STR (number, numberDecimalSeparator, domain, idx2string (nfe->number_decimal_separator), &error);
	SET_STR (number, numberGroupSeparator, domain, idx2string (nfe->number_group_separator), &error);
	MonoArray *number_sizes_arr = create_group_sizes_array (nfe->number_group_sizes,
								GROUP_SIZE, &error);
	if (mono_error_set_pending_exception (&error))
		return;
	MONO_OBJECT_SETREF (number, numberGroupSizes, number_sizes_arr);
	number->numberNegativePattern = nfe->number_negative_pattern;
	number->percentNegativePattern = nfe->percent_negative_pattern;
	number->percentPositivePattern = nfe->percent_positive_pattern;
	SET_STR (number, percentSymbol, domain, idx2string (nfe->percent_symbol), &error);
	SET_STR (number, perMilleSymbol, domain, idx2string (nfe->per_mille_symbol), &error);
	SET_STR (number, positiveInfinitySymbol, domain, idx2string (nfe->positive_infinity_symbol), &error);
	SET_STR (number, positiveSign, domain, idx2string (nfe->positive_sign), &error);
#undef SET_STR
}

static MonoBoolean
construct_culture (MonoCultureInfo *this_obj, const CultureInfoEntry *ci, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();

	error_init (error);

	this_obj->lcid = ci->lcid;

#define SET_STR(obj,field,domain,expr,err) do {				\
		MonoString *_tmp_str = mono_string_new_checked ((domain), (expr), (err)); \
		return_val_if_nok (err, FALSE);				\
		MONO_OBJECT_SETREF((obj), field, _tmp_str);		\
	} while (0)

	SET_STR (this_obj, name, domain, idx2string (ci->name), error);
	SET_STR (this_obj, englishname, domain, idx2string (ci->englishname), error);
	SET_STR (this_obj, nativename, domain, idx2string (ci->nativename), error);
	SET_STR (this_obj, win3lang, domain, idx2string (ci->win3lang), error);
	SET_STR (this_obj, iso3lang, domain, idx2string (ci->iso3lang), error);
	SET_STR (this_obj, iso2lang, domain, idx2string (ci->iso2lang), error);

	// It's null for neutral cultures
	if (ci->territory > 0) {
		SET_STR (this_obj, territory, domain, idx2string (ci->territory), error);
	}

	MonoArray *native_calendar_names = create_names_array_idx (ci->native_calendar_names, NUM_CALENDARS, error);
	return_val_if_nok (error, FALSE);
	MONO_OBJECT_SETREF (this_obj, native_calendar_names, native_calendar_names);
	this_obj->parent_lcid = ci->parent_lcid;
	this_obj->datetime_index = ci->datetime_format_index;
	this_obj->number_index = ci->number_format_index;
	this_obj->calendar_type = ci->calendar_type;
	this_obj->text_info_data = &ci->text_info;
#undef SET_STR
	
	return TRUE;
}

static MonoBoolean
construct_region (MonoRegionInfo *this_obj, const RegionInfoEntry *ri, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();

	error_init (error);

#define SET_STR(obj,field,domain,expr,err) do {				\
		MonoString *_tmp_str = mono_string_new_checked ((domain), (expr), (err)); \
		return_val_if_nok (err, FALSE);				\
		MONO_OBJECT_SETREF((obj), field, _tmp_str);		\
	} while (0)

	this_obj->geo_id = ri->geo_id;
	SET_STR (this_obj, iso2name, domain, idx2string (ri->iso2name), error);
	SET_STR (this_obj, iso3name, domain, idx2string (ri->iso3name), error);
	SET_STR (this_obj, win3name, domain, idx2string (ri->win3name), error);
	SET_STR (this_obj, english_name, domain, idx2string (ri->english_name), error);
	SET_STR (this_obj, native_name, domain, idx2string (ri->native_name), error);
	SET_STR (this_obj, currency_symbol, domain, idx2string (ri->currency_symbol), error);
	SET_STR (this_obj, iso_currency_symbol, domain, idx2string (ri->iso_currency_symbol), error);
	SET_STR (this_obj, currency_english_name, domain, idx2string (ri->currency_english_name), error);
	SET_STR (this_obj, currency_native_name, domain, idx2string (ri->currency_native_name), error);
	
#undef SET_STR

	return TRUE;
}

static const CultureInfoEntry*
culture_info_entry_from_lcid (int lcid)
{
	const CultureInfoEntry *ci;

	ci = (const CultureInfoEntry *)mono_binary_search (&lcid, culture_entries, NUM_CULTURE_ENTRIES, sizeof (CultureInfoEntry), culture_lcid_locator);

	return ci;
}

static const RegionInfoEntry*
region_info_entry_from_lcid (int lcid)
{
	const RegionInfoEntry *entry;
	const CultureInfoEntry *ne;

	ne = (const CultureInfoEntry *)mono_binary_search (&lcid, culture_entries, NUM_CULTURE_ENTRIES, sizeof (CultureInfoEntry), culture_lcid_locator);

	if (ne == NULL)
		return FALSE;

	entry = &region_entries [ne->region_entry_index];

	return entry;
}

#if defined (__APPLE__)
static gchar*
get_darwin_locale (void)
{
	static gchar *cached_locale = NULL;
	gchar *darwin_locale = NULL;
	CFLocaleRef locale = NULL;
	CFStringRef locale_language = NULL;
	CFStringRef locale_country = NULL;
	CFStringRef locale_script = NULL;
	CFStringRef locale_cfstr = NULL;
	CFIndex bytes_converted;
	CFIndex bytes_written;
	CFIndex len;
	int i;

	if (cached_locale != NULL)
		return g_strdup (cached_locale);

	locale = CFLocaleCopyCurrent ();

	if (locale) {
		locale_language = CFLocaleGetValue (locale, kCFLocaleLanguageCode);
		if (locale_language != NULL && CFStringGetBytes(locale_language, CFRangeMake (0, CFStringGetLength (locale_language)), kCFStringEncodingMacRoman, 0, FALSE, NULL, 0, &bytes_converted) > 0) {
			len = bytes_converted + 1;

			locale_country = CFLocaleGetValue (locale, kCFLocaleCountryCode);
			if (locale_country != NULL && CFStringGetBytes (locale_country, CFRangeMake (0, CFStringGetLength (locale_country)), kCFStringEncodingMacRoman, 0, FALSE, NULL, 0, &bytes_converted) > 0) {
				len += bytes_converted + 1;

				locale_script = CFLocaleGetValue (locale, kCFLocaleScriptCode);
				if (locale_script != NULL && CFStringGetBytes (locale_script, CFRangeMake (0, CFStringGetLength (locale_script)), kCFStringEncodingMacRoman, 0, FALSE, NULL, 0, &bytes_converted) > 0) {
					len += bytes_converted + 1;
				}

				darwin_locale = (char *) g_malloc (len + 1);
				CFStringGetBytes (locale_language, CFRangeMake (0, CFStringGetLength (locale_language)), kCFStringEncodingMacRoman, 0, FALSE, (UInt8 *) darwin_locale, len, &bytes_converted);

				darwin_locale[bytes_converted] = '-';
				bytes_written = bytes_converted + 1;
				if (locale_script != NULL && CFStringGetBytes (locale_script, CFRangeMake (0, CFStringGetLength (locale_script)), kCFStringEncodingMacRoman, 0, FALSE, (UInt8 *) &darwin_locale[bytes_written], len - bytes_written, &bytes_converted) > 0) {
					darwin_locale[bytes_written + bytes_converted] = '-';
					bytes_written += bytes_converted + 1;
				}

				CFStringGetBytes (locale_country, CFRangeMake (0, CFStringGetLength (locale_country)), kCFStringEncodingMacRoman, 0, FALSE, (UInt8 *) &darwin_locale[bytes_written], len - bytes_written, &bytes_converted);
				darwin_locale[bytes_written + bytes_converted] = '\0';
			}
		}

		if (darwin_locale == NULL) {
			locale_cfstr = CFLocaleGetIdentifier (locale);

			if (locale_cfstr) {
				len = CFStringGetMaximumSizeForEncoding (CFStringGetLength (locale_cfstr), kCFStringEncodingMacRoman) + 1;
				darwin_locale = (char *) g_malloc (len);
				if (!CFStringGetCString (locale_cfstr, darwin_locale, len, kCFStringEncodingMacRoman)) {
					g_free (darwin_locale);
					CFRelease (locale);
					cached_locale = NULL;
					return NULL;
				}

				for (i = 0; i < strlen (darwin_locale); i++)
					if (darwin_locale [i] == '_')
						darwin_locale [i] = '-';
			}			
		}

		CFRelease (locale);
	}

	mono_memory_barrier ();
	cached_locale = darwin_locale;
	return g_strdup (cached_locale);
}
#endif

static char *
get_posix_locale (void)
{
	char *locale;

	locale = g_getenv ("LC_ALL");
	if (locale == NULL) {
		locale = g_getenv ("LANG");
		if (locale == NULL) {
			char *static_locale = setlocale (LC_ALL, NULL);
			if (static_locale)
				locale = g_strdup (static_locale);
		}
	}
	if (locale == NULL)
		return NULL;

	/* Skip English-only locale 'C' */
	if (strcmp (locale, "C") == 0) {
		g_free (locale);
		return NULL;
	}

	return locale;
}


static gchar *
get_current_locale_name (void)
{
	char *locale;
	char *p, *ret;
		
#ifdef HOST_WIN32
	locale = g_win32_getlocale ();
#elif defined (__APPLE__)	
	locale = get_darwin_locale ();
	if (!locale)
		locale = get_posix_locale ();
#else
	locale = get_posix_locale ();
#endif

	if (locale == NULL)
		return NULL;

	p = strchr (locale, '.');
	if (p != NULL)
		*p = 0;
	p = strchr (locale, '@');
	if (p != NULL)
		*p = 0;
	p = strchr (locale, '_');
	if (p != NULL)
		*p = '-';

	ret = g_ascii_strdown (locale, -1);
	g_free (locale);

	return ret;
}

MonoStringHandle
ves_icall_System_Globalization_CultureInfo_get_current_locale_name (MonoError *error)
{
	error_init (error);
	gchar *locale;
	MonoDomain *domain;

	locale = get_current_locale_name ();
	if (locale == NULL)
		return MONO_HANDLE_CAST (MonoString, NULL_HANDLE);

	domain = mono_domain_get ();
	MonoStringHandle ret = mono_string_new_handle (domain, locale, error);
	g_free (locale);

	return ret;
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid (MonoCultureInfo *this_obj,
		gint lcid)
{
	MonoError error;
	const CultureInfoEntry *ci;
	
	ci = culture_info_entry_from_lcid (lcid);
	if(ci == NULL)
		return FALSE;

	if (!construct_culture (this_obj, ci, &error)) {
		mono_error_set_pending_exception (&error);
		return FALSE;
	}
	return TRUE;
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name (MonoCultureInfo *this_obj,
		MonoString *name)
{
	MonoError error;
	const CultureInfoNameEntry *ne;
	char *n;
	
	n = mono_string_to_utf8_checked (name, &error);
	if (mono_error_set_pending_exception (&error))
		return FALSE;
	ne = (const CultureInfoNameEntry *)mono_binary_search (n, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);

	if (ne == NULL) {
		/*g_print ("ne (%s) is null\n", n);*/
		g_free (n);
		return FALSE;
	}
	g_free (n);

	if (!construct_culture (this_obj, &culture_entries [ne->culture_entry_index], &error)) {
		mono_error_set_pending_exception (&error);
		return FALSE;
	}
	return TRUE;
}
/*
MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_specific_name (MonoCultureInfo *ci,
		MonoString *name)
{
	gchar *locale;
	gboolean ret;

	locale = mono_string_to_utf8 (name);
	ret = construct_culture_from_specific_name (ci, locale);
	g_free (locale);

	return ret;
}
*/
MonoBoolean
ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_lcid (MonoRegionInfo *this_obj,
		gint lcid)
{
	MonoError error;
	const RegionInfoEntry *ri;
	
	ri = region_info_entry_from_lcid (lcid);
	if(ri == NULL)
		return FALSE;

	MonoBoolean result = construct_region (this_obj, ri, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

MonoBoolean
ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_name (MonoRegionInfo *this_obj,
		MonoString *name)
{
	MonoError error;
	const RegionInfoNameEntry *ne;
	char *n;
	
	n = mono_string_to_utf8_checked (name, &error);
	if (mono_error_set_pending_exception (&error))
		return FALSE;
	ne = (const RegionInfoNameEntry *)mono_binary_search (n, region_name_entries, NUM_REGION_ENTRIES,
		sizeof (RegionInfoNameEntry), region_name_locator);

	if (ne == NULL) {
		/*g_print ("ne (%s) is null\n", n);*/
		g_free (n);
		return FALSE;
	}
	g_free (n);

	MonoBoolean result = construct_region (this_obj, &region_entries [ne->region_entry_index], &error);
	mono_error_set_pending_exception (&error);
	return result;
}

MonoArray*
ves_icall_System_Globalization_CultureInfo_internal_get_cultures (MonoBoolean neutral,
		MonoBoolean specific, MonoBoolean installed)
{
	MonoError error;
	MonoArray *ret;
	MonoClass *klass;
	MonoCultureInfo *culture;
	MonoDomain *domain;
	const CultureInfoEntry *ci;
	gint i, len;
	gboolean is_neutral;

	domain = mono_domain_get ();

	len = 0;
	for (i = 0; i < NUM_CULTURE_ENTRIES; i++) {
		ci = &culture_entries [i];
		is_neutral = ci->territory == 0;
		if ((neutral && is_neutral) || (specific && !is_neutral))
			len++;
	}

	klass = mono_class_get_culture_info_class ();

	/* The InvariantCulture is not in culture_entries */
	/* We reserve the first slot in the array for it */
	if (neutral)
		len++;

	ret = mono_array_new_checked (domain, klass, len, &error);
	goto_if_nok (&error, fail);

	if (len == 0)
		return ret;

	len = 0;
	if (neutral)
		mono_array_setref (ret, len++, NULL);

	for (i = 0; i < NUM_CULTURE_ENTRIES; i++) {
		ci = &culture_entries [i];
		is_neutral = ci->territory == 0;
		if ((neutral && is_neutral) || (specific && !is_neutral)) {
			culture = (MonoCultureInfo *) mono_object_new_checked (domain, klass, &error);
			goto_if_nok (&error, fail);
			mono_runtime_object_init_checked ((MonoObject *) culture, &error);
			goto_if_nok (&error, fail);
			if (!construct_culture (culture, ci, &error))
				goto fail;
			culture->use_user_override = TRUE;
			mono_array_setref (ret, len++, culture);
		}
	}

	return ret;

fail:
	mono_error_set_pending_exception (&error);
	return ret;
}

int ves_icall_System_Globalization_CompareInfo_internal_compare (MonoCompareInfo *this_obj, MonoString *str1, gint32 off1, gint32 len1, MonoString *str2, gint32 off2, gint32 len2, gint32 options)
{
	/* Do a normal ascii string compare, as we only know the
	 * invariant locale if we dont have ICU
	 */
	return(string_invariant_compare (str1, off1, len1, str2, off2, len2,
					 options));
}

void ves_icall_System_Globalization_CompareInfo_assign_sortkey (MonoCompareInfo *this_obj, MonoSortKey *key, MonoString *source, gint32 options)
{
	MonoError error;
	MonoArray *arr;
	gint32 keylen, i;

	keylen=mono_string_length (source);
	
	arr=mono_array_new_checked (mono_domain_get (), mono_get_byte_class (),
				    keylen, &error);
	if (mono_error_set_pending_exception (&error))
		return;

	for(i=0; i<keylen; i++) {
		mono_array_set (arr, guint8, i, mono_string_chars (source)[i]);
	}
	
	MONO_OBJECT_SETREF (key, key, arr);
}

int ves_icall_System_Globalization_CompareInfo_internal_index (MonoCompareInfo *this_obj, MonoString *source, gint32 sindex, gint32 count, MonoString *value, gint32 options, MonoBoolean first)
{
	return(string_invariant_indexof (source, sindex, count, value, first));
}

int ves_icall_System_Globalization_CompareInfo_internal_index_char (MonoCompareInfo *this_obj, MonoString *source, gint32 sindex, gint32 count, gunichar2 value, gint32 options, MonoBoolean first)
{
	return(string_invariant_indexof_char (source, sindex, count, value,
					      first));
}

int ves_icall_System_Threading_Thread_current_lcid (void)
{
	/* Invariant */
	return(0x007F);
}

static gint32 string_invariant_compare_char (gunichar2 c1, gunichar2 c2,
					     gint32 options)
{
	gint32 result;

	/* Ordinal can not be mixed with other options, and must return the difference, not only -1, 0, 1 */
	if (options & CompareOptions_Ordinal) 
		return (gint32) c1 - c2;
	
	if (options & CompareOptions_IgnoreCase) {
		GUnicodeType c1type, c2type;

		c1type = g_unichar_type (c1);
		c2type = g_unichar_type (c2);
	
		result = (gint32) (c1type != G_UNICODE_LOWERCASE_LETTER ? g_unichar_tolower(c1) : c1) -
			(c2type != G_UNICODE_LOWERCASE_LETTER ? g_unichar_tolower(c2) : c2);
	} else {
		/*
		 * No options. Kana, symbol and spacing options don't
		 * apply to the invariant culture.
		 */

		/*
		 * FIXME: here we must use the information from c1type and c2type
		 * to find out the proper collation, even on the InvariantCulture, the
		 * sorting is not done by computing the unicode values, but their
		 * actual sort order.
		 */
		result = (gint32) c1 - c2;
	}

	return ((result < 0) ? -1 : (result > 0) ? 1 : 0);
}

static gint32 string_invariant_compare (MonoString *str1, gint32 off1,
					gint32 len1, MonoString *str2,
					gint32 off2, gint32 len2,
					gint32 options)
{
	/* c translation of C# code from old string.cs.. :) */
	gint32 length;
	gint32 charcmp;
	gunichar2 *ustr1;
	gunichar2 *ustr2;
	gint32 pos;

	if(len1 >= len2) {
		length=len1;
	} else {
		length=len2;
	}

	ustr1 = mono_string_chars(str1)+off1;
	ustr2 = mono_string_chars(str2)+off2;

	pos = 0;

	for (pos = 0; pos != length; pos++) {
		if (pos >= len1 || pos >= len2)
			break;

		charcmp = string_invariant_compare_char(ustr1[pos], ustr2[pos],
							options);
		if (charcmp != 0) {
			return(charcmp);
		}
	}

	/* the lesser wins, so if we have looped until length we just
	 * need to check the last char
	 */
	if (pos == length) {
		return(string_invariant_compare_char(ustr1[pos - 1],
						     ustr2[pos - 1], options));
	}

	/* Test if one of the strings has been compared to the end */
	if (pos >= len1) {
		if (pos >= len2) {
			return(0);
		} else {
			return(-1);
		}
	} else if (pos >= len2) {
		return(1);
	}

	/* if not, check our last char only.. (can this happen?) */
	return(string_invariant_compare_char(ustr1[pos], ustr2[pos], options));
}

static gint32 string_invariant_indexof (MonoString *source, gint32 sindex,
					gint32 count, MonoString *value,
					MonoBoolean first)
{
	gint32 lencmpstr;
	gunichar2 *src;
	gunichar2 *cmpstr;
	gint32 pos,i;
	
	lencmpstr = mono_string_length(value);
	
	src = mono_string_chars(source);
	cmpstr = mono_string_chars(value);

	if(first) {
		count -= lencmpstr;
		for(pos=sindex;pos <= sindex+count;pos++) {
			for(i=0;src[pos+i]==cmpstr[i];) {
				if(++i==lencmpstr) {
					return(pos);
				}
			}
		}
		
		return(-1);
	} else {
		for(pos=sindex-lencmpstr+1;pos>sindex-count;pos--) {
			if(memcmp (src+pos, cmpstr,
				   lencmpstr*sizeof(gunichar2))==0) {
				return(pos);
			}
		}
		
		return(-1);
	}
}

static gint32 string_invariant_indexof_char (MonoString *source, gint32 sindex,
					     gint32 count, gunichar2 value,
					     MonoBoolean first)
{
	gint32 pos;
	gunichar2 *src;

	src = mono_string_chars(source);
	if(first) {
		for (pos = sindex; pos != count + sindex; pos++) {
			if (src [pos] == value) {
				return(pos);
			}
		}

		return(-1);
	} else {
		for (pos = sindex; pos > sindex - count; pos--) {
			if (src [pos] == value)
				return(pos);
		}

		return(-1);
	}
}

void ves_icall_System_Text_Normalization_load_normalization_resource (guint8 **argProps,
								      guint8 **argMappedChars,
								      guint8 **argCharMapIndex,
								      guint8 **argHelperIndex,
								      guint8 **argMapIdxToComposite,
								      guint8 **argCombiningClass,
								      MonoError *error)
{
	error_init (error);
#ifdef DISABLE_NORMALIZATION
	mono_error_set_not_supported (error, "This runtime has been compiled without string normalization support.");
	return;
#else
	*argProps = (guint8*)props;
	*argMappedChars = (guint8*) mappedChars;
	*argCharMapIndex = (guint8*) charMapIndex;
	*argHelperIndex = (guint8*) helperIndex;
	*argMapIdxToComposite = (guint8*) mapIdxToComposite;
	*argCombiningClass = (guint8*)combiningClass;
#endif
}


