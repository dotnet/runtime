/*
 * locales.c: Culture-sensitive handling
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
static GENERATE_GET_CLASS_WITH_CACHE (culture_info, System.Globalization, CultureInfo)

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
create_group_sizes_array (const gint *gs, gint ml)
{
	MonoArray *ret;
	int i, len = 0;

	for (i = 0; i < ml; i++) {
		if (gs [i] == -1)
			break;
		len++;
	}
	
	ret = mono_array_new_cached (mono_domain_get (),
			mono_get_int32_class (), len);

	for(i = 0; i < len; i++)
		mono_array_set (ret, gint32, i, gs [i]);

	return ret;
}

static MonoArray*
create_names_array_idx (const guint16 *names, int ml)
{
	MonoArray *ret;
	MonoDomain *domain;
	int i;

	if (names == NULL)
		return NULL;

	domain = mono_domain_get ();

	ret = mono_array_new_cached (mono_domain_get (), mono_get_string_class (), ml);

	for(i = 0; i < ml; i++)
		mono_array_setref (ret, i, mono_string_new (domain, idx2string (names [i])));

	return ret;
}

static MonoArray*
create_names_array_idx_dynamic (const guint16 *names, int ml)
{
	MonoArray *ret;
	MonoDomain *domain;
	int i, len = 0;

	if (names == NULL)
		return NULL;

	domain = mono_domain_get ();

	for (i = 0; i < ml; i++) {
		if (names [i] == 0)
			break;
		len++;
	}

	ret = mono_array_new_cached (mono_domain_get (), mono_get_string_class (), len);

	for(i = 0; i < len; i++)
		mono_array_setref (ret, i, mono_string_new (domain, idx2string (names [i])));

	return ret;
}

MonoBoolean
ves_icall_System_Globalization_CalendarData_fill_calendar_data (MonoCalendarData *this_obj, MonoString *name, gint32 calendar_index)
{
	MonoDomain *domain;
	const DateTimeFormatEntry *dfe;
	const CultureInfoNameEntry *ne;
	const CultureInfoEntry *ci;
	char *n;

	n = mono_string_to_utf8 (name);
	ne = (const CultureInfoNameEntry *)mono_binary_search (n, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);
	g_free (n);
	if (ne == NULL) {
		return FALSE;
	}

	ci = &culture_entries [ne->culture_entry_index];
	dfe = &datetime_format_entries [ci->datetime_format_index];

	domain = mono_domain_get ();

	MONO_OBJECT_SETREF (this_obj, NativeName, mono_string_new (domain, idx2string (ci->nativename)));
	MONO_OBJECT_SETREF (this_obj, ShortDatePatterns, create_names_array_idx_dynamic (dfe->short_date_patterns,
			NUM_SHORT_DATE_PATTERNS));
	MONO_OBJECT_SETREF (this_obj, YearMonthPatterns, create_names_array_idx_dynamic (dfe->year_month_patterns,
			NUM_YEAR_MONTH_PATTERNS));

	MONO_OBJECT_SETREF (this_obj, LongDatePatterns, create_names_array_idx_dynamic (dfe->long_date_patterns,
			NUM_LONG_DATE_PATTERNS));
	MONO_OBJECT_SETREF (this_obj, MonthDayPattern, mono_string_new (domain, idx2string (dfe->month_day_pattern)));

	MONO_OBJECT_SETREF (this_obj, DayNames, create_names_array_idx (dfe->day_names, NUM_DAYS));
	MONO_OBJECT_SETREF (this_obj, AbbreviatedDayNames, create_names_array_idx (dfe->abbreviated_day_names, 
			NUM_DAYS));
	MONO_OBJECT_SETREF (this_obj, SuperShortDayNames, create_names_array_idx (dfe->shortest_day_names, NUM_DAYS));
	MONO_OBJECT_SETREF (this_obj, MonthNames, create_names_array_idx (dfe->month_names, NUM_MONTHS));
	MONO_OBJECT_SETREF (this_obj, AbbreviatedMonthNames, create_names_array_idx (dfe->abbreviated_month_names,
			NUM_MONTHS));
	MONO_OBJECT_SETREF (this_obj, GenitiveMonthNames, create_names_array_idx (dfe->month_genitive_names, NUM_MONTHS));
	MONO_OBJECT_SETREF (this_obj, GenitiveAbbreviatedMonthNames, create_names_array_idx (dfe->abbreviated_month_genitive_names, NUM_MONTHS));

	return TRUE;
}

void
ves_icall_System_Globalization_CultureData_fill_culture_data (MonoCultureData *this_obj, gint32 datetime_index)
{
	MonoDomain *domain;
	const DateTimeFormatEntry *dfe;

	g_assert (datetime_index >= 0);

	dfe = &datetime_format_entries [datetime_index];

	domain = mono_domain_get ();

	MONO_OBJECT_SETREF (this_obj, AMDesignator, mono_string_new (domain, idx2string (dfe->am_designator)));
	MONO_OBJECT_SETREF (this_obj, PMDesignator, mono_string_new (domain, idx2string (dfe->pm_designator)));
	MONO_OBJECT_SETREF (this_obj, TimeSeparator, mono_string_new (domain, idx2string (dfe->time_separator)));
	MONO_OBJECT_SETREF (this_obj, LongTimePatterns, create_names_array_idx_dynamic (dfe->long_time_patterns,
			NUM_LONG_TIME_PATTERNS));
	MONO_OBJECT_SETREF (this_obj, ShortTimePatterns, create_names_array_idx_dynamic (dfe->short_time_patterns,
			NUM_SHORT_TIME_PATTERNS));
	this_obj->FirstDayOfWeek = dfe->first_day_of_week;
	this_obj->CalendarWeekRule = dfe->calendar_week_rule;
}

void
ves_icall_System_Globalization_CultureData_fill_number_data (MonoNumberFormatInfo* number, gint32 number_index)
{
	MonoDomain *domain;
	const NumberFormatEntry *nfe;

	g_assert (number_index >= 0);

	nfe = &number_format_entries [number_index];

	domain = mono_domain_get ();

	number->currencyDecimalDigits = nfe->currency_decimal_digits;
	MONO_OBJECT_SETREF (number, currencyDecimalSeparator, mono_string_new (domain,
			idx2string (nfe->currency_decimal_separator)));
	MONO_OBJECT_SETREF (number, currencyGroupSeparator, mono_string_new (domain,
			idx2string (nfe->currency_group_separator)));
	MONO_OBJECT_SETREF (number, currencyGroupSizes, create_group_sizes_array (nfe->currency_group_sizes,
			GROUP_SIZE));
	number->currencyNegativePattern = nfe->currency_negative_pattern;
	number->currencyPositivePattern = nfe->currency_positive_pattern;
	MONO_OBJECT_SETREF (number, currencySymbol, mono_string_new (domain, idx2string (nfe->currency_symbol)));
	MONO_OBJECT_SETREF (number, naNSymbol, mono_string_new (domain, idx2string (nfe->nan_symbol)));
	MONO_OBJECT_SETREF (number, negativeInfinitySymbol, mono_string_new (domain,
			idx2string (nfe->negative_infinity_symbol)));
	MONO_OBJECT_SETREF (number, negativeSign, mono_string_new (domain, idx2string (nfe->negative_sign)));
	number->numberDecimalDigits = nfe->number_decimal_digits;
	MONO_OBJECT_SETREF (number, numberDecimalSeparator, mono_string_new (domain,
			idx2string (nfe->number_decimal_separator)));
	MONO_OBJECT_SETREF (number, numberGroupSeparator, mono_string_new (domain, idx2string (nfe->number_group_separator)));
	MONO_OBJECT_SETREF (number, numberGroupSizes, create_group_sizes_array (nfe->number_group_sizes,
			GROUP_SIZE));
	number->numberNegativePattern = nfe->number_negative_pattern;
	number->percentNegativePattern = nfe->percent_negative_pattern;
	number->percentPositivePattern = nfe->percent_positive_pattern;
	MONO_OBJECT_SETREF (number, percentSymbol, mono_string_new (domain, idx2string (nfe->percent_symbol)));
	MONO_OBJECT_SETREF (number, perMilleSymbol, mono_string_new (domain, idx2string (nfe->per_mille_symbol)));
	MONO_OBJECT_SETREF (number, positiveInfinitySymbol, mono_string_new (domain,
			idx2string (nfe->positive_infinity_symbol)));
	MONO_OBJECT_SETREF (number, positiveSign, mono_string_new (domain, idx2string (nfe->positive_sign)));
}

static MonoBoolean
construct_culture (MonoCultureInfo *this_obj, const CultureInfoEntry *ci)
{
	MonoDomain *domain = mono_domain_get ();

	this_obj->lcid = ci->lcid;
	MONO_OBJECT_SETREF (this_obj, name, mono_string_new (domain, idx2string (ci->name)));
	MONO_OBJECT_SETREF (this_obj, englishname, mono_string_new (domain, idx2string (ci->englishname)));
	MONO_OBJECT_SETREF (this_obj, nativename, mono_string_new (domain, idx2string (ci->nativename)));
	MONO_OBJECT_SETREF (this_obj, win3lang, mono_string_new (domain, idx2string (ci->win3lang)));
	MONO_OBJECT_SETREF (this_obj, iso3lang, mono_string_new (domain, idx2string (ci->iso3lang)));
	MONO_OBJECT_SETREF (this_obj, iso2lang, mono_string_new (domain, idx2string (ci->iso2lang)));

	// It's null for neutral cultures
	if (ci->territory > 0)
		MONO_OBJECT_SETREF (this_obj, territory, mono_string_new (domain, idx2string (ci->territory)));
	MONO_OBJECT_SETREF (this_obj, native_calendar_names, create_names_array_idx (ci->native_calendar_names, NUM_CALENDARS));
	this_obj->parent_lcid = ci->parent_lcid;
	this_obj->datetime_index = ci->datetime_format_index;
	this_obj->number_index = ci->number_format_index;
	this_obj->calendar_type = ci->calendar_type;
	this_obj->text_info_data = &ci->text_info;
	
	return TRUE;
}

static MonoBoolean
construct_region (MonoRegionInfo *this_obj, const RegionInfoEntry *ri)
{
	MonoDomain *domain = mono_domain_get ();

	this_obj->geo_id = ri->geo_id;
	MONO_OBJECT_SETREF (this_obj, iso2name, mono_string_new (domain, idx2string (ri->iso2name)));
	MONO_OBJECT_SETREF (this_obj, iso3name, mono_string_new (domain, idx2string (ri->iso3name)));
	MONO_OBJECT_SETREF (this_obj, win3name, mono_string_new (domain, idx2string (ri->win3name)));
	MONO_OBJECT_SETREF (this_obj, english_name, mono_string_new (domain, idx2string (ri->english_name)));
	MONO_OBJECT_SETREF (this_obj, native_name, mono_string_new (domain, idx2string (ri->native_name)));
	MONO_OBJECT_SETREF (this_obj, currency_symbol, mono_string_new (domain, idx2string (ri->currency_symbol)));
	MONO_OBJECT_SETREF (this_obj, iso_currency_symbol, mono_string_new (domain, idx2string (ri->iso_currency_symbol)));
	MONO_OBJECT_SETREF (this_obj, currency_english_name, mono_string_new (domain, idx2string (ri->currency_english_name)));
	MONO_OBJECT_SETREF (this_obj, currency_native_name, mono_string_new (domain, idx2string (ri->currency_native_name)));
	
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
	static gchar *darwin_locale = NULL;
	CFLocaleRef locale = NULL;
	CFStringRef locale_language = NULL;
	CFStringRef locale_country = NULL;
	CFStringRef locale_script = NULL;
	CFStringRef locale_cfstr = NULL;
	CFIndex bytes_converted;
	CFIndex bytes_written;
	CFIndex len;
	int i;

	if (darwin_locale != NULL)
		return g_strdup (darwin_locale);

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

				darwin_locale = (char *) malloc (len + 1);
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
				darwin_locale = (char *) malloc (len);
				if (!CFStringGetCString (locale_cfstr, darwin_locale, len, kCFStringEncodingMacRoman)) {
					free (darwin_locale);
					CFRelease (locale);
					darwin_locale = NULL;
					return NULL;
				}

				for (i = 0; i < strlen (darwin_locale); i++)
					if (darwin_locale [i] == '_')
						darwin_locale [i] = '-';
			}			
		}

		CFRelease (locale);
	}

	return g_strdup (darwin_locale);
}
#endif

static char *
get_posix_locale (void)
{
	const char *locale;

	locale = g_getenv ("LC_ALL");
	if (locale == NULL) {
		locale = g_getenv ("LANG");
		if (locale == NULL)
			locale = setlocale (LC_ALL, NULL);
	}
	if (locale == NULL)
		return NULL;

	/* Skip English-only locale 'C' */
	if (strcmp (locale, "C") == 0)
		return NULL;

	return g_strdup (locale);
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

MonoString*
ves_icall_System_Globalization_CultureInfo_get_current_locale_name (void)
{
	gchar *locale;
	MonoString* ret;
	MonoDomain *domain;

	locale = get_current_locale_name ();
	if (locale == NULL)
		return NULL;

	domain = mono_domain_get ();
	ret = mono_string_new (domain, locale);
	g_free (locale);

	return ret;
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid (MonoCultureInfo *this_obj,
		gint lcid)
{
	const CultureInfoEntry *ci;
	
	ci = culture_info_entry_from_lcid (lcid);
	if(ci == NULL)
		return FALSE;

	return construct_culture (this_obj, ci);
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name (MonoCultureInfo *this_obj,
		MonoString *name)
{
	const CultureInfoNameEntry *ne;
	char *n;
	
	n = mono_string_to_utf8 (name);
	ne = (const CultureInfoNameEntry *)mono_binary_search (n, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);

	if (ne == NULL) {
		/*g_print ("ne (%s) is null\n", n);*/
		g_free (n);
		return FALSE;
	}
	g_free (n);

	return construct_culture (this_obj, &culture_entries [ne->culture_entry_index]);
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
	const RegionInfoEntry *ri;
	
	ri = region_info_entry_from_lcid (lcid);
	if(ri == NULL)
		return FALSE;

	return construct_region (this_obj, ri);
}

MonoBoolean
ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_name (MonoRegionInfo *this_obj,
		MonoString *name)
{
	const RegionInfoNameEntry *ne;
	char *n;
	
	n = mono_string_to_utf8 (name);
	ne = (const RegionInfoNameEntry *)mono_binary_search (n, region_name_entries, NUM_REGION_ENTRIES,
		sizeof (RegionInfoNameEntry), region_name_locator);

	if (ne == NULL) {
		/*g_print ("ne (%s) is null\n", n);*/
		g_free (n);
		return FALSE;
	}
	g_free (n);

	return construct_region (this_obj, &region_entries [ne->region_entry_index]);
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

	ret = mono_array_new (domain, klass, len);

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
			mono_error_raise_exception (&error);
			mono_runtime_object_init ((MonoObject *) culture);
			construct_culture (culture, ci);
			culture->use_user_override = TRUE;
			mono_array_setref (ret, len++, culture);
		}
	}

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
	MonoArray *arr;
	gint32 keylen, i;

	keylen=mono_string_length (source);
	
	arr=mono_array_new (mono_domain_get (), mono_get_byte_class (),
			    keylen);
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
																	  guint8 **argCombiningClass)
{
#ifdef DISABLE_NORMALIZATION
	mono_set_pending_exception (mono_get_exception_not_supported ("This runtime has been compiled without string normalization support."));
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


