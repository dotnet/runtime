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

#if !ENABLE_NETCORE

#include <glib.h>
#include <string.h>

#include <mono/metadata/class-init.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/locales.h>
#include <mono/culture/culture-info.h>
#include <mono/culture/culture-info-tables.h>
#include <mono/utils/bsearch.h>

#ifndef DISABLE_NORMALIZATION
#include <mono/metadata/normalization-tables.h>
#endif

#include <locale.h>
#if defined(__APPLE__)
#include <CoreFoundation/CoreFoundation.h>
#endif
#include <mono/metadata/icall-decl.h>

#undef DEBUG

/* Make sure computing VALUE doesn't cause a GC */
/* Don't move to handles.h */
#define mono_handle_setval(handle,field,value) MONO_HANDLE_RAW (handle)->field = (value)

#define mono_handle_setref(handle,field,value) MONO_HANDLE_SET((handle),field,(value))

#define mono_array_handle_setref(array,index,value) MONO_HANDLE_ARRAY_SETREF ((array), (index), (value))

static gint32 string_invariant_compare_char (gunichar2 c1, gunichar2 c2,
					     gint32 options);

static const CultureInfoEntry* culture_info_entry_from_lcid (int lcid);

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

static MonoArrayHandle
create_names_array_idx (const guint16 *names, int ml, MonoError *error)
{
	MonoArrayHandle ret;
	MonoDomain *domain;

	if (names == NULL)
		return NULL_HANDLE_ARRAY;

	domain = mono_domain_get ();

	ret = mono_array_new_handle (domain, mono_get_string_class (), ml, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	MonoStringHandle s_h = MONO_HANDLE_NEW (MonoString, NULL);
	for (int i = 0; i < ml; i++) {
		MonoString *s = mono_string_new_checked (domain, dtidx2string (names [i]), error);
		return_val_if_nok (error, NULL_HANDLE_ARRAY);
		MONO_HANDLE_ASSIGN_RAW (s_h, s);
		mono_array_handle_setref (ret, i, s_h);
	}

	return ret;
}

static MonoArrayHandle
create_names_array_idx_dynamic (const guint16 *names, int ml, MonoError *error)
{
	MonoArrayHandle ret;
	MonoDomain *domain;
	int i, len = 0;

	error_init (error);

	if (names == NULL)
		return NULL_HANDLE_ARRAY;

	domain = mono_domain_get ();

	for (i = 0; i < ml; i++) {
		if (names [i] == 0)
			break;
		len++;
	}

	ret = mono_array_new_handle (domain, mono_get_string_class (), len, error);
	return_val_if_nok (error, NULL_HANDLE_ARRAY);

	MonoStringHandle s_h = MONO_HANDLE_NEW (MonoString, NULL);
	for(i = 0; i < len; i++) {
		MONO_HANDLE_ASSIGN_RAW (s_h, mono_string_new_checked (domain, pattern2string (names [i]), error));
		return_val_if_nok (error, NULL_HANDLE_ARRAY);
		mono_array_handle_setref (ret, i, s_h);
	}

	return ret;
}

MonoBoolean
ves_icall_System_Globalization_CalendarData_fill_calendar_data (MonoCalendarDataHandle this_obj, MonoStringHandle name, gint32 calendar_index, MonoError *error)
{
	MonoDomain *domain;
	MonoArrayHandle arr;
	MonoStringHandle s;
	const DateTimeFormatEntry *dfe;
	const CultureInfoNameEntry *ne;
	const CultureInfoEntry *ci;
	char *n;

	n = mono_string_handle_to_utf8 (name, error);
	return_val_if_nok (error, FALSE);
	ne = (const CultureInfoNameEntry *)mono_binary_search (n, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);
	g_free (n);
	if (ne == NULL)
		return FALSE;

	ci = &culture_entries [ne->culture_entry_index];
	dfe = &datetime_format_entries [ci->datetime_format_index];

	domain = mono_domain_get ();

	s = mono_string_new_handle (domain, idx2string (ci->nativename), error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, NativeName, s);

	arr = create_names_array_idx_dynamic (dfe->short_date_patterns, NUM_SHORT_DATE_PATTERNS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, ShortDatePatterns, arr);
	arr = create_names_array_idx_dynamic (dfe->year_month_patterns,
										  NUM_YEAR_MONTH_PATTERNS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, YearMonthPatterns, arr);

	arr = create_names_array_idx_dynamic (dfe->long_date_patterns,
										  NUM_LONG_DATE_PATTERNS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, LongDatePatterns, arr);

	MonoStringHandle month_day_pattern = mono_string_new_handle (domain, pattern2string (dfe->month_day_pattern), error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, MonthDayPattern, month_day_pattern);

	arr = create_names_array_idx (dfe->day_names, NUM_DAYS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, DayNames, arr);

	arr = create_names_array_idx (dfe->abbreviated_day_names, NUM_DAYS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, AbbreviatedDayNames, arr);

	arr = create_names_array_idx (dfe->shortest_day_names, NUM_DAYS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, SuperShortDayNames, arr);

	arr = create_names_array_idx (dfe->month_names, NUM_MONTHS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, MonthNames, arr);

	arr = create_names_array_idx (dfe->abbreviated_month_names, NUM_MONTHS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, AbbreviatedMonthNames, arr);
	
	arr = create_names_array_idx (dfe->month_genitive_names, NUM_MONTHS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, GenitiveMonthNames, arr);

	arr = create_names_array_idx (dfe->abbreviated_month_genitive_names, NUM_MONTHS, error);
	return_val_if_nok (error, FALSE);
	MONO_HANDLE_SET (this_obj, GenitiveAbbreviatedMonthNames, arr);

	return TRUE;
}

void
ves_icall_System_Globalization_CultureData_fill_culture_data (MonoCultureDataHandle this_obj, gint32 datetime_index, MonoError *error)
{
	MonoDomain *domain;
	const DateTimeFormatEntry *dfe;
	MonoArrayHandle arr;

	g_assert (datetime_index >= 0);

	dfe = &datetime_format_entries [datetime_index];

	domain = mono_domain_get ();

#define SET_STR_FIELD(obj,field,domain,expr,error) do {					\
	MonoStringHandle _str = mono_string_new_handle ((domain), (expr), (error));	\
	return_if_nok (error); \
	MONO_HANDLE_SET ((obj), field, _str);	\
	} while (0)

	SET_STR_FIELD (this_obj, AMDesignator, domain, idx2string (dfe->am_designator), error);
	SET_STR_FIELD (this_obj, PMDesignator, domain, idx2string (dfe->pm_designator), error);
	SET_STR_FIELD (this_obj, TimeSeparator, domain, idx2string (dfe->time_separator), error);

#undef SET_STR_FIELD

	arr = create_names_array_idx_dynamic (dfe->long_time_patterns,
										  NUM_LONG_TIME_PATTERNS, error);
	return_if_nok (error);
	MONO_HANDLE_SET (this_obj, LongTimePatterns, arr);
	arr = create_names_array_idx_dynamic (dfe->short_time_patterns,
										  NUM_SHORT_TIME_PATTERNS, error);
	return_if_nok (error);
	MONO_HANDLE_SET (this_obj, ShortTimePatterns, arr);

	MONO_HANDLE_SETVAL (this_obj, FirstDayOfWeek, guint32, dfe->first_day_of_week);
	MONO_HANDLE_SETVAL (this_obj, CalendarWeekRule, guint32, dfe->calendar_week_rule);
}

gconstpointer
ves_icall_System_Globalization_CultureData_fill_number_data (gint32 number_index,
	NumberFormatEntryManaged *managed)
{
	g_assertf (number_index >= 0 && number_index < G_N_ELEMENTS (number_format_entries), "%d", number_index);
	NumberFormatEntry const * const native = &number_format_entries [number_index];
	// We could return the pointer directly, but I'm leary of the 7x byte layout.
	// Does C# match C exactly?
	// If the data is regenerated, suggest a byte of padding there.
	managed->currency_decimal_digits = native->currency_decimal_digits;
	managed->currency_decimal_separator = native->currency_decimal_separator;
	managed->currency_group_separator = native->currency_group_separator;
	managed->currency_group_sizes0 = native->currency_group_sizes [0];
	managed->currency_group_sizes1 = native->currency_group_sizes [1];
	managed->currency_negative_pattern = native->currency_negative_pattern;
	managed->currency_positive_pattern = native->currency_positive_pattern;
	managed->currency_symbol = native->currency_symbol;
	managed->nan_symbol = native->nan_symbol;
	managed->negative_infinity_symbol = native->negative_infinity_symbol;
	managed->negative_sign = native->negative_sign;
	managed->number_decimal_digits = native->number_decimal_digits;
	managed->number_decimal_separator = native->number_decimal_separator;
	managed->number_group_separator = native->number_group_separator;
	managed->number_group_sizes0 = native->number_group_sizes [0];
	managed->number_group_sizes1  = native->number_group_sizes [1];
	managed->number_negative_pattern = native->number_negative_pattern;
	managed->per_mille_symbol = native->per_mille_symbol;
	managed->percent_negative_pattern = native->percent_negative_pattern;
	managed->percent_positive_pattern = native->percent_positive_pattern;
	managed->percent_symbol = native->percent_symbol;
	managed->positive_infinity_symbol = native->positive_infinity_symbol;
	managed->positive_sign = native->positive_sign;
	return locale_strings;
}

static MonoBoolean
construct_culture (MonoCultureInfoHandle this_obj, const CultureInfoEntry *ci, MonoError *error)
{
	MonoBoolean result = FALSE;
	HANDLE_FUNCTION_ENTER ();
	MonoDomain *domain = mono_domain_get ();

	mono_handle_setval (this_obj, lcid, ci->lcid);

#define SET_STR_FIELD(obj,field,domain,expr,error) do {					\
	MonoString *_str = mono_string_new_checked ((domain), (expr), (error));	\
	goto_if_nok (error, leave);										\
	MONO_HANDLE_SETRAW ((obj), field, _str);	\
	} while (0)

	SET_STR_FIELD (this_obj, name, domain, idx2string (ci->name), error);
	SET_STR_FIELD (this_obj, englishname, domain, idx2string (ci->englishname), error);
	SET_STR_FIELD (this_obj, nativename, domain, idx2string (ci->nativename), error);
	SET_STR_FIELD (this_obj, win3lang, domain, idx2string (ci->win3lang), error);
	SET_STR_FIELD (this_obj, iso3lang, domain, idx2string (ci->iso3lang), error);
	SET_STR_FIELD (this_obj, iso2lang, domain, idx2string (ci->iso2lang), error);

	// It's null for neutral cultures
	if (ci->territory > 0)
		SET_STR_FIELD (this_obj, territory, domain, idx2string (ci->territory), error);

#undef SET_STR_FIELD

	MonoArrayHandle native_calendar_names; native_calendar_names = create_names_array_idx (ci->native_calendar_names, NUM_CALENDARS, error);
	goto_if_nok (error, leave);
	mono_handle_setref (this_obj, native_calendar_names, native_calendar_names);
	mono_handle_setval (this_obj, parent_lcid, ci->parent_lcid);
	mono_handle_setval (this_obj, datetime_index, ci->datetime_format_index);
	mono_handle_setval (this_obj, number_index, ci->number_format_index);
	mono_handle_setval (this_obj, calendar_type, ci->calendar_type);
	mono_handle_setval (this_obj, text_info_data, &ci->text_info);
	result = TRUE;
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoBoolean
construct_region (MonoRegionInfoHandle this_obj, const RegionInfoEntry *ri, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();

	mono_handle_setval (this_obj, geo_id, ri->geo_id);

#define SET_STR_FIELD(obj,field,domain,expr,error) do {					\
	MonoStringHandle _str = mono_string_new_handle ((domain), (expr), (error));	\
	return_val_if_nok (error, FALSE);										\
	MONO_HANDLE_SET ((obj), field, _str);	\
	} while (0)

	SET_STR_FIELD (this_obj, iso2name, domain, idx2string (ri->iso2name), error);
	SET_STR_FIELD (this_obj, iso3name, domain, idx2string (ri->iso3name), error);
	SET_STR_FIELD (this_obj, win3name, domain, idx2string (ri->win3name), error);
	SET_STR_FIELD (this_obj, english_name, domain, idx2string (ri->english_name), error);
	SET_STR_FIELD (this_obj, native_name, domain, idx2string (ri->native_name), error);
	SET_STR_FIELD (this_obj, currency_symbol, domain, idx2string (ri->currency_symbol), error);
	SET_STR_FIELD (this_obj, iso_currency_symbol, domain, idx2string (ri->iso_currency_symbol), error);
	SET_STR_FIELD (this_obj, currency_english_name, domain, idx2string (ri->currency_english_name), error);
	SET_STR_FIELD (this_obj, currency_native_name, domain, idx2string (ri->currency_native_name), error);

#undef SET_STR_FIELD

	return TRUE;
}

static const CultureInfoEntry*
culture_info_entry_from_lcid (int lcid)
{
	const CultureInfoEntry *ci;

	ci = (const CultureInfoEntry *)mono_binary_search (&lcid, culture_entries, NUM_CULTURE_ENTRIES, sizeof (CultureInfoEntry), culture_lcid_locator);

	return ci;
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
		locale_language = (CFStringRef)CFLocaleGetValue (locale, kCFLocaleLanguageCode);
		if (locale_language != NULL && CFStringGetBytes(locale_language, CFRangeMake (0, CFStringGetLength (locale_language)), kCFStringEncodingMacRoman, 0, FALSE, NULL, 0, &bytes_converted) > 0) {
			len = bytes_converted + 1;

			locale_country = (CFStringRef)CFLocaleGetValue (locale, kCFLocaleCountryCode);
			if (locale_country != NULL && CFStringGetBytes (locale_country, CFRangeMake (0, CFStringGetLength (locale_country)), kCFStringEncodingMacRoman, 0, FALSE, NULL, 0, &bytes_converted) > 0) {
				len += bytes_converted + 1;

				locale_script = (CFStringRef)CFLocaleGetValue (locale, kCFLocaleScriptCode);
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
	gchar *locale;
	MonoDomain *domain;

	locale = get_current_locale_name ();
	if (locale == NULL)
		return NULL_HANDLE_STRING;

	domain = mono_domain_get ();
	MonoStringHandle ret = mono_string_new_handle (domain, locale, error);
	g_free (locale);

	return ret;
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid (MonoCultureInfoHandle this_obj,
																				int lcid, MonoError *error)
{
	const CultureInfoEntry *ci;
	
	ci = culture_info_entry_from_lcid (lcid);
	if(ci == NULL)
		return FALSE;

	return construct_culture (this_obj, ci, error);
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name (MonoCultureInfoHandle this_obj,
																				MonoStringHandle name, MonoError *error)
{
	const CultureInfoNameEntry *ne;
	char *n;
	
	n = mono_string_handle_to_utf8 (name, error);
	return_val_if_nok (error, FALSE);
	ne = (const CultureInfoNameEntry *)mono_binary_search (n, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);

	if (ne == NULL) {
		/*g_print ("ne (%s) is null\n", n);*/
		g_free (n);
		return FALSE;
	}
	g_free (n);

	return construct_culture (this_obj, &culture_entries [ne->culture_entry_index], error);
}

MonoBoolean
ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_name (MonoRegionInfoHandle this_obj,
																			   MonoStringHandle name, MonoError *error)
{
	const RegionInfoNameEntry *ne;
	char *n;
	
	n = mono_string_handle_to_utf8 (name, error);
	return_val_if_nok (error, FALSE);
	ne = (const RegionInfoNameEntry *)mono_binary_search (n, region_name_entries, NUM_REGION_ENTRIES,
		sizeof (RegionInfoNameEntry), region_name_locator);

	if (ne == NULL) {
		/*g_print ("ne (%s) is null\n", n);*/
		g_free (n);
		return FALSE;
	}
	g_free (n);

	return construct_region (this_obj, &region_entries [ne->region_entry_index], error);
}

MonoArrayHandle
ves_icall_System_Globalization_CultureInfo_internal_get_cultures (MonoBoolean neutral,
																  MonoBoolean specific, MonoBoolean installed,
																  MonoError *error)
{
	MonoArrayHandle ret;
	MonoClass *klass;
	MonoDomain *domain;
	MonoCultureInfoHandle culture_h;
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

	ret = mono_array_new_handle (domain, klass, len, error);
	goto_if_nok (error, fail);

	if (len == 0)
		return ret;

	len = 0;
	if (neutral)
		len ++;

	culture_h = MONO_HANDLE_NEW (MonoCultureInfo, NULL);
	for (i = 0; i < NUM_CULTURE_ENTRIES; i++) {
		ci = &culture_entries [i];
		is_neutral = ci->territory == 0;
		if ((neutral && is_neutral) || (specific && !is_neutral)) {
			MONO_HANDLE_ASSIGN_RAW (culture_h, mono_object_new_checked (domain, klass, error));
			goto_if_nok (error, fail);
			mono_runtime_object_init_handle (MONO_HANDLE_CAST (MonoObject, culture_h), error);
			goto_if_nok (error, fail);
			if (!construct_culture (culture_h, ci, error))
				goto fail;
			mono_handle_setval (culture_h, use_user_override, TRUE);
			mono_array_handle_setref (ret, len, culture_h);
			len ++;
		}
	}

	return ret;

fail:
	mono_error_set_pending_exception (error);
	return ret;
}

static gint32
string_invariant_compare_char (gunichar2 c1, gunichar2 c2,
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

gint32
ves_icall_System_Globalization_CompareInfo_internal_compare (const gunichar2 *ustr1, gint32 len1,
	const gunichar2 *ustr2, gint32 len2, gint32 options)
{
	/* Do a normal ascii string compare, as we only know the
	 * invariant locale if we dont have ICU
	 */

	/* c translation of C# code from old string.cs.. :) */
	const gint32 length = MAX (len1, len2);
	gint32 charcmp;
	gint32 pos = 0;

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

gint32
ves_icall_System_Globalization_CompareInfo_internal_index (const gunichar2 *src, gint32 sindex,
	gint32 count, const gunichar2 *cmpstr, gint32 lencmpstr, MonoBoolean first)
{
	gint32 pos,i;
	
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

void
ves_icall_System_Text_Normalization_load_normalization_resource (guint8 **argProps,
								      guint8 **argMappedChars,
								      guint8 **argCharMapIndex,
								      guint8 **argHelperIndex,
								      guint8 **argMapIdxToComposite,
								      guint8 **argCombiningClass,
								      MonoError *error)
{
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

#endif /* !ENABLE_NETCORE */
