/*
 * locales.c: Culture-sensitive handling
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Mohammad DAMT (mdamt@cdl2000.com)
 *
 * (C) 2003 Ximian, Inc.
 * (C) 2003 PT Cakram Datalingga Duaribu  http://www.cdl2000.com
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


#include <locale.h>

#undef DEBUG

static gint32 string_invariant_compare_char (gunichar2 c1, gunichar2 c2,
					     gint32 options);
static gint32 string_invariant_compare (MonoString *str1, gint32 off1,
					gint32 len1, MonoString *str2,
					gint32 off2, gint32 len2,
					gint32 options);
static MonoString *string_invariant_replace (MonoString *me,
					     MonoString *oldValue,
					     MonoString *newValue);
static gint32 string_invariant_indexof (MonoString *source, gint32 sindex,
					gint32 count, MonoString *value,
					MonoBoolean first);
static gint32 string_invariant_indexof_char (MonoString *source, gint32 sindex,
					     gint32 count, gunichar2 value,
					     MonoBoolean first);

static const CultureInfoEntry* culture_info_entry_from_lcid (int lcid);

static int
culture_lcid_locator (const void *a, const void *b)
{
	const CultureInfoEntry *aa = a;
	const CultureInfoEntry *bb = b;

	return (aa->lcid - bb->lcid);
}

static int
culture_name_locator (const void *a, const void *b)
{
	const char *aa = a;
	const CultureInfoNameEntry *bb = b;
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
	
	ret = mono_array_new (mono_domain_get (),
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
	int i, len = 0;

	if (names == NULL)
		return NULL;

	domain = mono_domain_get ();

	for (i = 0; i < ml; i++) {
		if (names [i] == 0)
			break;
		len++;
	}

	ret = mono_array_new (mono_domain_get (), mono_get_string_class (), len);

	for(i = 0; i < len; i++)
		mono_array_set (ret, MonoString *, i, mono_string_new (domain, idx2string (names [i])));

	return ret;
}

void
ves_icall_System_Globalization_CultureInfo_construct_datetime_format (MonoCultureInfo *this)
{
	MonoDomain *domain;
	MonoDateTimeFormatInfo *datetime;
	const DateTimeFormatEntry *dfe;

	MONO_ARCH_SAVE_REGS;

	g_assert (this->datetime_index >= 0);

	datetime = this->datetime_format;
	dfe = &datetime_format_entries [this->datetime_index];

	domain = mono_domain_get ();

	datetime->AbbreviatedDayNames = create_names_array_idx (dfe->abbreviated_day_names,
			NUM_DAYS);
	datetime->AbbreviatedMonthNames = create_names_array_idx (dfe->abbreviated_month_names,
			NUM_MONTHS);
	datetime->AMDesignator = mono_string_new (domain, idx2string (dfe->am_designator));
	datetime->CalendarWeekRule = dfe->calendar_week_rule;
	datetime->DateSeparator = mono_string_new (domain, idx2string (dfe->date_separator));
	datetime->DayNames = create_names_array_idx (dfe->day_names, NUM_DAYS);
	datetime->FirstDayOfWeek = dfe->first_day_of_week;
	datetime->FullDateTimePattern = mono_string_new (domain, idx2string (dfe->full_date_time_pattern));
	datetime->LongDatePattern = mono_string_new (domain, idx2string (dfe->long_date_pattern));
	datetime->LongTimePattern = mono_string_new (domain, idx2string (dfe->long_time_pattern));
	datetime->MonthDayPattern = mono_string_new (domain, idx2string (dfe->month_day_pattern));
	datetime->MonthNames = create_names_array_idx (dfe->month_names, NUM_MONTHS);
	datetime->PMDesignator = mono_string_new (domain, idx2string (dfe->pm_designator));
	datetime->ShortDatePattern = mono_string_new (domain, idx2string (dfe->short_date_pattern));
	datetime->ShortTimePattern = mono_string_new (domain, idx2string (dfe->short_time_pattern));
	datetime->TimeSeparator = mono_string_new (domain, idx2string (dfe->time_separator));
	datetime->YearMonthPattern = mono_string_new (domain, idx2string (dfe->year_month_pattern));
	datetime->ShortDatePatterns = create_names_array_idx (dfe->short_date_patterns,
			NUM_SHORT_DATE_PATTERNS);
	datetime->LongDatePatterns = create_names_array_idx (dfe->long_date_patterns,
			NUM_LONG_DATE_PATTERNS);
	datetime->ShortTimePatterns = create_names_array_idx (dfe->short_time_patterns,
			NUM_SHORT_TIME_PATTERNS);
	datetime->LongTimePatterns = create_names_array_idx (dfe->long_time_patterns,
			NUM_LONG_TIME_PATTERNS);

}

void
ves_icall_System_Globalization_CultureInfo_construct_number_format (MonoCultureInfo *this)
{
	MonoDomain *domain;
	MonoNumberFormatInfo *number;
	const NumberFormatEntry *nfe;

	MONO_ARCH_SAVE_REGS;

	g_assert (this->number_format != 0);

	number = this->number_format;
	nfe = &number_format_entries [this->number_index];

	domain = mono_domain_get ();

	number->currencyDecimalDigits = nfe->currency_decimal_digits;
	number->currencyDecimalSeparator = mono_string_new (domain,
			idx2string (nfe->currency_decimal_separator));
	number->currencyGroupSeparator = mono_string_new (domain,
			idx2string (nfe->currency_group_separator));
	number->currencyGroupSizes = create_group_sizes_array (nfe->currency_group_sizes,
			GROUP_SIZE);
	number->currencyNegativePattern = nfe->currency_negative_pattern;
	number->currencyPositivePattern = nfe->currency_positive_pattern;
	number->currencySymbol = mono_string_new (domain, idx2string (nfe->currency_symbol));
	number->naNSymbol = mono_string_new (domain, idx2string (nfe->nan_symbol));
	number->negativeInfinitySymbol = mono_string_new (domain,
			idx2string (nfe->negative_infinity_symbol));
	number->negativeSign = mono_string_new (domain, idx2string (nfe->negative_sign));
	number->numberDecimalDigits = nfe->number_decimal_digits;
	number->numberDecimalSeparator = mono_string_new (domain,
			idx2string (nfe->number_decimal_separator));
	number->numberGroupSeparator = mono_string_new (domain, idx2string (nfe->number_group_separator));
	number->numberGroupSizes = create_group_sizes_array (nfe->number_group_sizes,
			GROUP_SIZE);
	number->numberNegativePattern = nfe->number_negative_pattern;
	number->percentDecimalDigits = nfe->percent_decimal_digits;
	number->percentDecimalSeparator = mono_string_new (domain,
			idx2string (nfe->percent_decimal_separator));
	number->percentGroupSeparator = mono_string_new (domain,
			idx2string (nfe->percent_group_separator));
	number->percentGroupSizes = create_group_sizes_array (nfe->percent_group_sizes,
			GROUP_SIZE);
	number->percentNegativePattern = nfe->percent_negative_pattern;
	number->percentPositivePattern = nfe->percent_positive_pattern;
	number->percentSymbol = mono_string_new (domain, idx2string (nfe->percent_symbol));
	number->perMilleSymbol = mono_string_new (domain, idx2string (nfe->per_mille_symbol));
	number->positiveInfinitySymbol = mono_string_new (domain,
			idx2string (nfe->positive_infinity_symbol));
	number->positiveSign = mono_string_new (domain, idx2string (nfe->positive_sign));
}

static MonoBoolean
construct_culture (MonoCultureInfo *this, const CultureInfoEntry *ci)
{
	MonoDomain *domain = mono_domain_get ();

	this->lcid = ci->lcid;
	this->name = mono_string_new (domain, idx2string (ci->name));
	this->icu_name = mono_string_new (domain, idx2string (ci->icu_name));
	this->displayname = mono_string_new (domain, idx2string (ci->displayname));
	this->englishname = mono_string_new (domain, idx2string (ci->englishname));
	this->nativename = mono_string_new (domain, idx2string (ci->nativename));
	this->win3lang = mono_string_new (domain, idx2string (ci->win3lang));
	this->iso3lang = mono_string_new (domain, idx2string (ci->iso3lang));
	this->iso2lang = mono_string_new (domain, idx2string (ci->iso2lang));
	this->parent_lcid = ci->parent_lcid;
	this->specific_lcid = ci->specific_lcid;
	this->datetime_index = ci->datetime_format_index;
	this->number_index = ci->number_format_index;
	this->calendar_data = ci->calendar_data;
	
	return TRUE;
}

static gboolean
construct_culture_from_specific_name (MonoCultureInfo *ci, gchar *name)
{
	const CultureInfoEntry *entry;
	const CultureInfoNameEntry *ne;

	MONO_ARCH_SAVE_REGS;

	ne = bsearch (name, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);

	if (ne == NULL)
		return FALSE;

	entry = &culture_entries [ne->culture_entry_index];

	/* try avoiding another lookup, often the culture is its own specific culture */
	if (entry->lcid != entry->specific_lcid)
		entry = culture_info_entry_from_lcid (entry->specific_lcid);

	return construct_culture (ci, entry);
}

static const CultureInfoEntry*
culture_info_entry_from_lcid (int lcid)
{
	const CultureInfoEntry *ci;
	CultureInfoEntry key;

	key.lcid = lcid;
	ci = bsearch (&key, culture_entries, NUM_CULTURE_ENTRIES, sizeof (CultureInfoEntry), culture_lcid_locator);

	return ci;
}

/**
 * The following two methods are modified from the ICU source code. (http://oss.software.ibm.com/icu)
 * Copyright (c) 1995-2003 International Business Machines Corporation and others
 * All rights reserved.
 */
static gchar*
get_posix_locale (void)
{
	const gchar* posix_locale = NULL;

	posix_locale = g_getenv("LC_ALL");
	if (posix_locale == 0) {
		posix_locale = g_getenv("LANG");
		if (posix_locale == 0) {
			posix_locale = setlocale(LC_ALL, NULL);
		}
	}

	if (posix_locale == NULL)
		return NULL;

	if ((strcmp ("C", posix_locale) == 0) || (strchr (posix_locale, ' ') != NULL)
			|| (strchr (posix_locale, '/') != NULL)) {
		/**
		 * HPUX returns 'C C C C C C C'
		 * Solaris can return /en_US/C/C/C/C/C on the second try.
		 * Maybe we got some garbage.
		 */
		return NULL;
	}

	return g_strdup (posix_locale);
}

static gchar*
get_current_locale_name (void)
{
	gchar *locale;
	gchar *corrected = NULL;
	const gchar *p;
        gchar *c;

#ifdef PLATFORM_WIN32
	locale = g_win32_getlocale ();
#else	
	locale = get_posix_locale ();
#endif	

	if (locale == NULL)
		return NULL;

	if ((p = strchr (locale, '.')) != NULL) {
		/* assume new locale can't be larger than old one? */
		corrected = malloc (strlen (locale));
		strncpy (corrected, locale, p - locale);
		corrected [p - locale] = 0;

		/* do not copy after the @ */
		if ((p = strchr (corrected, '@')) != NULL)
			corrected [p - corrected] = 0;
	}

	/* Note that we scan the *uncorrected* ID. */
	if ((p = strrchr (locale, '@')) != NULL) {

		/**
		 * In Mono we dont handle the '@' modifier because we do
		 * not have any cultures that use it. We just trim it
		 * off of the end of the name.
		 */

		if (corrected == NULL) {
			corrected = malloc (strlen (locale));
			strncpy (corrected, locale, p - locale);
			corrected [p - locale] = 0;
		}
	}

	if (corrected == NULL)
		corrected = locale;
	else
		g_free (locale);

	if ((c = strchr (corrected, '_')) != NULL)
		*c = '-';

	g_strdown (corrected);

	return corrected;
}	 

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_current_locale (MonoCultureInfo *ci)
{
	gchar *locale;
	gboolean ret;

	MONO_ARCH_SAVE_REGS;

	locale = get_current_locale_name ();
	if (locale == NULL)
		return FALSE;

	ret = construct_culture_from_specific_name (ci, locale);
	g_free (locale);

	return ret;
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid (MonoCultureInfo *this,
		gint lcid)
{
	const CultureInfoEntry *ci;
	
	MONO_ARCH_SAVE_REGS;

	ci = culture_info_entry_from_lcid (lcid);
	if(ci == NULL)
		return FALSE;

	return construct_culture (this, ci);
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name (MonoCultureInfo *this,
		MonoString *name)
{
	const CultureInfoNameEntry *ne;
	char *n;
	
	MONO_ARCH_SAVE_REGS;

	n = mono_string_to_utf8 (name);
	ne = bsearch (n, culture_name_entries, NUM_CULTURE_ENTRIES,
			sizeof (CultureInfoNameEntry), culture_name_locator);

	if (ne == NULL) {
                g_print ("ne (%s) is null\n", n);
        	g_free (n);
		return FALSE;
        }
        g_free (n);

	return construct_culture (this, &culture_entries [ne->culture_entry_index]);
}

MonoBoolean
ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_specific_name (MonoCultureInfo *ci,
		MonoString *name)
{
	gchar *locale;
	gboolean ret;

	MONO_ARCH_SAVE_REGS;

	locale = mono_string_to_utf8 (name);
	ret = construct_culture_from_specific_name (ci, locale);
	g_free (locale);

	return ret;
}

MonoArray*
ves_icall_System_Globalization_CultureInfo_internal_get_cultures (MonoBoolean neutral,
		MonoBoolean specific, MonoBoolean installed)
{
	MonoArray *ret;
	MonoClass *class;
	MonoCultureInfo *culture;
	MonoDomain *domain;
	const CultureInfoEntry *ci;
	gint i, len;
	gboolean is_neutral;

	MONO_ARCH_SAVE_REGS;

	domain = mono_domain_get ();

	len = 0;
	for (i = 0; i < NUM_CULTURE_ENTRIES; i++) {
		ci = &culture_entries [i];
		is_neutral = ((ci->lcid & 0xff00) == 0 || ci->specific_lcid == 0);
		if ((neutral && is_neutral) || (specific && !is_neutral))
			len++;
	}

	class = mono_class_from_name (mono_get_corlib (),
			"System.Globalization", "CultureInfo");
	ret = mono_array_new (domain, class, len);

	if (len == 0)
		return ret;

	len = 0;
	for (i = 0; i < NUM_CULTURE_ENTRIES; i++) {
		ci = &culture_entries [i];
		is_neutral = ((ci->lcid & 0xff00) == 0 || ci->specific_lcid == 0);
		if ((neutral && is_neutral) || (specific && !is_neutral)) {
			culture = (MonoCultureInfo *) mono_object_new (domain, class);
			mono_runtime_object_init ((MonoObject *) culture);
			construct_culture (culture, ci);
			mono_array_set (ret, MonoCultureInfo *, len++, culture);
		}
	}

	return ret;
}

/**
 * Set is_neutral and return TRUE if the culture is found. If it is not found return FALSE.
 */
MonoBoolean
ves_icall_System_Globalization_CultureInfo_internal_is_lcid_neutral (gint lcid, MonoBoolean *is_neutral)
{
	const CultureInfoEntry *entry;

	MONO_ARCH_SAVE_REGS;

	entry = culture_info_entry_from_lcid (lcid);

	if (entry == NULL)
		return FALSE;

	*is_neutral = (entry->specific_lcid == 0);

	return TRUE;
}

#ifdef HAVE_ICU

#include <unicode/utypes.h>
#include <unicode/ustring.h>
#include <unicode/ures.h>
#include <unicode/ucol.h>
#include <unicode/usearch.h>

static MonoString *monostring_from_resource_index (const UResourceBundle *bundle, int32_t idx)
{
	gunichar2 *res_str;
	int32_t res_strlen;
	UErrorCode ec;
	
	ec=U_ZERO_ERROR;
	res_str=(gunichar2 *)ures_getStringByIndex (bundle, idx, &res_strlen,
						   &ec);
	if(U_FAILURE (ec)) {
		return(NULL);
	}

	return(mono_string_from_utf16 (res_str));
}

static UResourceBundle *open_subbundle (const UResourceBundle *bundle,
					const char *name, int32_t req_count)
{
	UResourceBundle *subbundle;
	UErrorCode ec;
	int32_t count;
	
	ec=U_ZERO_ERROR;
	subbundle=ures_getByKey (bundle, name, NULL, &ec);
	if(U_FAILURE (ec)) {
		/* Couldn't find the subbundle */
		return(NULL);
	}
	
	count=ures_countArrayItems (bundle, name, &ec);
	if(U_FAILURE (ec)) {
		/* Couldn't count the subbundle */
		ures_close (subbundle);
		return(NULL);
	}
	
	if(count!=req_count) {
		/* Bummer */
		ures_close (subbundle);
		return(NULL);
	}

	return(subbundle);
}

static MonoArray *build_array (const UResourceBundle *bundle,
			       const char *resname, int32_t req_count)
{
	MonoArray *arr=NULL;
	UResourceBundle *subbundle;
	int i;
	
	subbundle=open_subbundle (bundle, resname, req_count);
	if(subbundle!=NULL) {
		arr=mono_array_new(mono_domain_get (),
				   mono_get_string_class (), req_count);
		
		for(i=0; i<req_count; i++) {
			mono_array_set(arr, MonoString *, i, monostring_from_resource_index (subbundle, i));
		}

		ures_close (subbundle);
	}

	return(arr);
}

static MonoDateTimeFormatInfo *create_DateTimeFormat (const char *locale)
{
	MonoDateTimeFormatInfo *new_dtf;
	MonoClass *class;
	UResourceBundle *bundle, *subbundle;
	UErrorCode ec;
	
	class=mono_class_from_name (mono_get_corlib (),
				    "System.Globalization",
				    "DateTimeFormatInfo");
	new_dtf=(MonoDateTimeFormatInfo *)mono_object_new (mono_domain_get (),
							   class);
	mono_runtime_object_init ((MonoObject *)new_dtf);
	
	ec=U_ZERO_ERROR;

	bundle=ures_open (NULL, locale, &ec);
	if(U_FAILURE (ec)) {
		goto error1;
	}
	
	/* AM/PM markers */
	subbundle=open_subbundle (bundle, "AmPmMarkers", 2);
	if(subbundle!=NULL) {
		new_dtf->AMDesignator=monostring_from_resource_index (subbundle, 0);
		new_dtf->PMDesignator=monostring_from_resource_index (subbundle, 1);
		
		ures_close (subbundle);
	}
	
	/* Date/Time patterns.	Don't set FullDateTimePattern.	As it
	 * seems to always default to LongDatePattern + " " +
	 * LongTimePattern, let the property accessor deal with it.
	 */
	subbundle=open_subbundle (bundle, "DateTimePatterns", 9);
	if(subbundle!=NULL) {
		new_dtf->ShortDatePattern=monostring_from_resource_index (subbundle, 7);
		new_dtf->LongDatePattern=monostring_from_resource_index (subbundle, 5);
		new_dtf->ShortTimePattern=monostring_from_resource_index (subbundle, 3);
		new_dtf->LongTimePattern=monostring_from_resource_index (subbundle, 2);

		/* RFC1123Pattern, SortableDateTimePattern and
		 * UniversalSortableDateTimePattern all seem to be
		 * constant, and all the same as the invariant default
		 * set in the ctor
		 */
	
		ures_close (subbundle);
	}
	
#if 0
	/* Not sure what to do with these yet, so leave them set to
	 * the invariant default
	 */
	set_field_string (new_dtf, "_DateSeparator", str);
	set_field_string (new_dtf, "_TimeSeparator", str);
	set_field_string (new_dtf, "_MonthDayPattern", str);
	set_field_string (new_dtf, "_YearMonthPattern", str);
#endif

	/* Day names.  Luckily both ICU and .net start Sunday at index 0 */
	new_dtf->DayNames=build_array (bundle, "DayNames", 7);

	/* Abbreviated day names */
	new_dtf->AbbreviatedDayNames=build_array (bundle, "DayAbbreviations",
						  7);

	/* Month names */
	new_dtf->MonthNames=build_array (bundle, "MonthNames", 12);
	
	/* Abbreviated month names */
	new_dtf->AbbreviatedMonthNames=build_array (bundle,
						    "MonthAbbreviations", 12);

	/* TODO: DayOfWeek _FirstDayOfWeek, Calendar _Calendar, CalendarWeekRule _CalendarWeekRule */

	ures_close (bundle);
error1:
	return(new_dtf);
}

static MonoNumberFormatInfo *create_NumberFormat (const char *locale)
{
	MonoNumberFormatInfo *new_nf;
	MonoClass *class;
	MonoMethodDesc* methodDesc;
	MonoMethod *method;
	UResourceBundle *bundle, *subbundle, *table_entries;
	UErrorCode ec;
	int32_t count;
	static char country [7]; /* FIXME */
	const UChar *res_str;
	int32_t res_strlen;

	class=mono_class_from_name (mono_get_corlib (),
				    "System.Globalization",
				    "NumberFormatInfo");
	new_nf=(MonoNumberFormatInfo *)mono_object_new (mono_domain_get (),
							class);
	mono_runtime_object_init ((MonoObject *)new_nf);

	ec=U_ZERO_ERROR;

	bundle=ures_open (NULL, locale, &ec);
	if(U_FAILURE (ec)) {
		goto error1;
	}

	/* Number Elements */
	ec=U_ZERO_ERROR;
	subbundle=ures_getByKey (bundle, "NumberElements", NULL, &ec);
	if(U_FAILURE (ec)) {
		/* Couldn't find the subbundle */
		goto error1;
	}
		
	count=ures_countArrayItems (bundle, "NumberElements", &ec);
	if(U_FAILURE (ec)) {
		/* Couldn't count the subbundle */
		ures_close (subbundle);
		goto error1;
	}

	if(subbundle!=NULL) {
		new_nf->numberDecimalSeparator=monostring_from_resource_index (subbundle, 0);
		new_nf->numberGroupSeparator=monostring_from_resource_index (subbundle, 1);
		new_nf->percentDecimalSeparator=monostring_from_resource_index (subbundle, 0);
		new_nf->percentGroupSeparator=monostring_from_resource_index (subbundle, 1);
		new_nf->percentSymbol=monostring_from_resource_index (subbundle, 3);
		new_nf->zeroPattern=monostring_from_resource_index (subbundle, 4);
		new_nf->digitPattern=monostring_from_resource_index (subbundle, 5);
		new_nf->negativeSign=monostring_from_resource_index (subbundle, 6);
		new_nf->perMilleSymbol=monostring_from_resource_index (subbundle, 8);
		new_nf->positiveInfinitySymbol=monostring_from_resource_index (subbundle, 9);
		/* we dont have this in CLDR, so copy it from positiveInfinitySymbol */
		new_nf->negativeInfinitySymbol=monostring_from_resource_index (subbundle, 9);
		new_nf->naNSymbol=monostring_from_resource_index (subbundle, 10);
		new_nf->currencyDecimalSeparator=monostring_from_resource_index (subbundle, 0);
		new_nf->currencyGroupSeparator=monostring_from_resource_index (subbundle, 1);

		ures_close (subbundle);
	}
 
	/* get country name */
	ec = U_ZERO_ERROR;
	uloc_getCountry (locale, country, sizeof (country), &ec);
	if (U_SUCCESS (ec)) {						
		ec = U_ZERO_ERROR;
		/* find country name in root.CurrencyMap */
		subbundle = ures_getByKey (bundle, "CurrencyMap", NULL, &ec);
		if (U_SUCCESS (ec)) {
			ec = U_ZERO_ERROR;
			/* get currency id for specified country */
			table_entries = ures_getByKey (subbundle, country, NULL, &ec);
			if (U_SUCCESS (ec)) {
				ures_close (subbundle);
				ec = U_ZERO_ERROR;
				
				res_str = ures_getStringByIndex (
					table_entries, 0, &res_strlen, &ec);				
				if(U_SUCCESS (ec)) {
					/* now we have currency id string */
					ures_close (table_entries);
					ec = U_ZERO_ERROR;
					u_UCharsToChars (res_str, country,
							 sizeof (country));
					if(U_SUCCESS (ec)) {
						ec = U_ZERO_ERROR;
						/* find currency string in locale data */
						subbundle = ures_getByKey (
							bundle, "Currencies",
							NULL, &ec);
							
						if (U_SUCCESS (ec)) {
							ec = U_ZERO_ERROR;
							/* find currency symbol under specified currency id */
							table_entries = ures_getByKey (subbundle, country, NULL, &ec);
							if (U_SUCCESS (ec)) {
								/* get the first string only, 
								 * the second is international currency symbol (not used)*/
								new_nf->currencySymbol=monostring_from_resource_index (table_entries, 0);
								ures_close (table_entries);
							}
							ures_close (subbundle);
						}		
					}
				}
			}
		}
	}

	subbundle=open_subbundle (bundle, "NumberPatterns", 4);
	if(subbundle!=NULL) {
		new_nf->decimalFormats=monostring_from_resource_index (subbundle, 0);
		new_nf->currencyFormats=monostring_from_resource_index (subbundle, 1);
		new_nf->percentFormats=monostring_from_resource_index (subbundle, 2);
		ures_close (subbundle);
		
		/* calls InitPatterns to parse the patterns
		 */
		methodDesc = mono_method_desc_new (
			"System.Globalization.NumberFormatInfo:InitPatterns()",
			TRUE);
		method = mono_method_desc_search_in_class (methodDesc, class);
		if(method!=NULL) {
			mono_runtime_invoke (method, new_nf, NULL, NULL);
		} else {
			g_warning (G_GNUC_PRETTY_FUNCTION ": Runtime mismatch with class lib! (Looking for System.Globalization.NumberFormatInfo:InitPatterns())");
		}
	}

	ures_close (bundle);
error1:
	return(new_nf);
}

static char *mono_string_to_icu_locale (MonoString *locale)
{
	UErrorCode ec;
	char *passed_locale, *icu_locale=NULL;
	int32_t loc_len, ret;

	passed_locale=mono_string_to_utf8 (locale);
	
	ec=U_ZERO_ERROR;
	ret=uloc_getName (passed_locale, NULL, 0, &ec);
	if(ec==U_BUFFER_OVERFLOW_ERROR) {
		ec=U_ZERO_ERROR;
		loc_len=ret+1;
		icu_locale=(char *)g_malloc0 (sizeof(char)*loc_len);
		ret=uloc_getName (passed_locale, icu_locale, loc_len, &ec);
	}
	g_free (passed_locale);
	
	return(icu_locale);
}

void ves_icall_System_Globalization_CultureInfo_construct_internal_locale (MonoCultureInfo *this, MonoString *locale)
{
	UChar *ustr;
	char *str;
	UErrorCode ec;
	char *icu_locale;
	int32_t str_len, ret;
	
	MONO_ARCH_SAVE_REGS;

	icu_locale=mono_string_to_icu_locale (locale);
	if(icu_locale==NULL) {
		/* Something went wrong */
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_get_corlib (), "System", "SystemException"));
		return;
	}
	
	/* Fill in the static fields */

	/* TODO: Calendar, InstalledUICulture, OptionalCalendars,
	 * TextInfo
	 */

	str_len=256;	/* Should be big enough for anything */
	str=(char *)g_malloc0 (sizeof(char)*str_len);
	ustr=(UChar *)g_malloc0 (sizeof(UChar)*str_len);
	
	ec=U_ZERO_ERROR;
	
	ret=uloc_getDisplayName (icu_locale, "en", ustr, str_len, &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		this->englishname=mono_string_from_utf16 ((gunichar2 *)ustr);
	}
	
	ret=uloc_getDisplayName (icu_locale, uloc_getDefault (), ustr, str_len,
				 &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		this->displayname=mono_string_from_utf16 ((gunichar2 *)ustr);
	}
	
	ret=uloc_getDisplayName (icu_locale, icu_locale, ustr, str_len, &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		this->nativename=mono_string_from_utf16 ((gunichar2 *)ustr);
	}

	this->iso3lang=mono_string_new_wrapper (uloc_getISO3Language (icu_locale));

	ret=uloc_getLanguage (icu_locale, str, str_len, &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		this->iso2lang=mono_string_new_wrapper (str);
	}

	this->datetime_format=create_DateTimeFormat (icu_locale);
	this->number_format=create_NumberFormat (icu_locale);
 
	g_free (str);
	g_free (ustr);
	g_free (icu_locale);
}

void ves_icall_System_Globalization_CompareInfo_construct_compareinfo (MonoCompareInfo *comp, MonoString *locale)
{
	UCollator *coll;
	UErrorCode ec;
	char *icu_locale;
	
	MONO_ARCH_SAVE_REGS;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Constructing collator for locale [%s]", mono_string_to_utf8 (locale));
#endif

	icu_locale=mono_string_to_icu_locale (locale);
	if(icu_locale==NULL) {
		/* Something went wrong */
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_get_corlib (), "System", "SystemException"));
		return;
	}

	ec=U_ZERO_ERROR;
	coll=ucol_open (icu_locale, &ec);
	if(U_SUCCESS (ec)) {
		comp->ICU_collator=coll;
	} else {
		comp->ICU_collator=NULL;
	}

	g_free (icu_locale);
}

/* Set up the collator to reflect the options required.	 Some of these
 * options clash, as they adjust the collator strength level.  Try to
 * make later checks reduce the strength level, and attempt to take
 * previous options into account.
 *
 * Don't bother to check the error returns when setting the
 * attributes, as a failure here is hardly grounds to error out.
 */
static void set_collator_options (UCollator *coll, gint32 options)
{
	UErrorCode ec=U_ZERO_ERROR;
	
	/* Set up the defaults */
	ucol_setAttribute (coll, UCOL_ALTERNATE_HANDLING, UCOL_NON_IGNORABLE,
			   &ec);
	ucol_setAttribute (coll, UCOL_CASE_LEVEL, UCOL_OFF, &ec);
	
	/* Do this first so other options will override the quaternary
	 * level strength setting if necessary
	 */
	if(!(options & CompareOptions_IgnoreKanaType)) {
		ucol_setAttribute (coll, UCOL_HIRAGANA_QUATERNARY_MODE,
				   UCOL_ON, &ec);
		ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_QUATERNARY, &ec);
	}

	/* Word sort, the default */
	if(!(options & CompareOptions_StringSort)) {
		ucol_setAttribute (coll, UCOL_ALTERNATE_HANDLING,
				   UCOL_SHIFTED, &ec);
		/* Tertiary strength is the default, but it might have
		 * been set to quaternary above.  (We don't want that
		 * here, because that will order all the punctuation
		 * first instead of just ignoring it.)
		 *
		 * Unfortunately, tertiary strength with
		 * ALTERNATE_HANDLING==SHIFTED means that '/' and '@'
		 * compare to equal, which has the nasty side effect
		 * of killing mcs :-( (We can't specify a
		 * culture-insensitive compare, because
		 * String.StartsWith doesn't have that option.)
		 *
		 * ALTERNATE_HANDLING==SHIFTED is needed to accomplish
		 * the word-sorting-ignoring-punctuation feature.  So
		 * we have to live with the slightly mis-ordered
		 * punctuation and a working mcs...
		 */
		ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_QUATERNARY, &ec);
	}

	if(options & CompareOptions_IgnoreCase) {
		ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_SECONDARY, &ec);
		ucol_setAttribute (coll, UCOL_ALTERNATE_HANDLING, UCOL_NON_IGNORABLE, &ec);
	}

	if(options & CompareOptions_IgnoreWidth) {
		/* Kana width is a tertiary strength difference.  This
		 * will totally break the !IgnoreKanaType option
		 */
		ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_SECONDARY, &ec);
	}
		
	if(options & CompareOptions_IgnoreNonSpace) {
		ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_PRIMARY, &ec);
		/* We can still compare case even when just checking
		 * primary strength
		 */
		if(!(options & CompareOptions_IgnoreCase) ||
		   !(options & CompareOptions_IgnoreWidth)) {
			/* Not sure if CASE_LEVEL handles kana width
			 */
			ucol_setAttribute (coll, UCOL_CASE_LEVEL, UCOL_ON,
					   &ec);
		}
	}

	if(options & CompareOptions_IgnoreSymbols) {
		/* Don't know what to do here */
	}

	if(options == CompareOptions_Ordinal) {
		/* This one is handled elsewhere */
	}
}

gint32 ves_icall_System_Globalization_CompareInfo_internal_compare (MonoCompareInfo *this, MonoString *str1, gint32 off1, gint32 len1, MonoString *str2, gint32 off2, gint32 len2, gint32 options)
{
	UCollator *coll;
	UCollationResult result;
	
	MONO_ARCH_SAVE_REGS;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Comparing [%s] and [%s]", mono_string_to_utf8 (str1), mono_string_to_utf8 (str2));
#endif

	coll=this->ICU_collator;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", this->lcid);
#endif
	
	if(coll==NULL || this->lcid==0x007F ||
	   options & CompareOptions_Ordinal) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": No collator or invariant, using shortcut");
#endif

		return(string_invariant_compare (str1, off1, len1, str2, off2,
						 len2, options));
	}
	
	if (!mono_monitor_enter ((MonoObject *)this))
		return(-1);
	
	set_collator_options (coll, options);
			
	result=ucol_strcoll (coll, mono_string_chars (str1)+off1, len1,
			     mono_string_chars (str2)+off2, len2);

	mono_monitor_exit ((MonoObject *)this);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Comparison of [%s] and [%s] returning %d", mono_string_to_utf8 (str1), mono_string_to_utf8 (str2), result);
#endif
	
	return(result);
}

void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoCompareInfo *this)
{
	UCollator *coll;
	
	MONO_ARCH_SAVE_REGS;
	
	coll=this->ICU_collator;
	if(coll!=NULL) {
		this->ICU_collator = NULL;
		ucol_close (coll);
	}
}

void ves_icall_System_Globalization_CompareInfo_assign_sortkey (MonoCompareInfo *this, MonoSortKey *key, MonoString *source, gint32 options)
{
	UCollator *coll;
	MonoArray *arr;
	char *keybuf;
	int32_t keylen, i;
	
	MONO_ARCH_SAVE_REGS;
	
	coll=this->ICU_collator;
	if(coll==NULL) {
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_get_corlib (), "System", "SystemException"));
		return;
	}
	
	if (!mono_monitor_enter ((MonoObject *)this))
		return;
	
	set_collator_options (coll, options);

	keylen=ucol_getSortKey (coll, mono_string_chars (source),
				mono_string_length (source), NULL, 0);
	keybuf=g_malloc (sizeof(char)* keylen);
	ucol_getSortKey (coll, mono_string_chars (source),
			 mono_string_length (source), keybuf, keylen);

	mono_monitor_exit ((MonoObject *)this);
	
	arr=mono_array_new (mono_domain_get (), mono_get_byte_class (),
			    keylen);
	for(i=0; i<keylen; i++) {
		mono_array_set (arr, guint8, i, keybuf[i]);
	}
	
	key->key=arr;

	g_free (keybuf);
}

int ves_icall_System_Globalization_CompareInfo_internal_index (MonoCompareInfo *this, MonoString *source, gint32 sindex, gint32 count, MonoString *value, gint32 options, MonoBoolean first)
{
	UCollator *coll;
	UChar *usrcstr;
	UErrorCode ec;
	UStringSearch *search;
	int32_t pos= -1;
	
	MONO_ARCH_SAVE_REGS;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Finding %s [%s] in [%s] (sindex %d,count %d)", first?"first":"last", mono_string_to_utf8 (value), mono_string_to_utf8 (source), sindex, count);
#endif

	coll=this->ICU_collator;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", this->lcid);
#endif

	if(coll==NULL || this->lcid==0x007F ||
	   options & CompareOptions_Ordinal) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": No collator or invariant, using shortcut");
#endif

		return(string_invariant_indexof (source, sindex, count, value,
						 first));
	}
	
	usrcstr=g_malloc0 (sizeof(UChar)*(count+1));
	if(first) {
		memcpy (usrcstr, mono_string_chars (source)+sindex,
			sizeof(UChar)*count);
	} else {
		memcpy (usrcstr, mono_string_chars (source)+sindex-count+1,
			sizeof(UChar)*count);
	}
	
	if (!mono_monitor_enter ((MonoObject *)this))
		return(-1);
	
	ec=U_ZERO_ERROR;
	
	/* Need to set the collator to a fairly weak level, so that it
	 * treats characters that can be written differently as
	 * identical (eg "ß" and "ss", "æ" and "ae" or "ä" etc.)  Note
	 * that this means that the search string and the original
	 * text might have differing lengths.
	 */
	ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_PRIMARY, &ec);

	/* Still notice case differences though (normally a tertiary
	 * difference)
	 */
	ucol_setAttribute (coll, UCOL_CASE_LEVEL, UCOL_ON, &ec);

	/* Don't ignore some codepoints */
	ucol_setAttribute (coll, UCOL_ALTERNATE_HANDLING, UCOL_NON_IGNORABLE,
			   &ec);
	
	search=usearch_openFromCollator (mono_string_chars (value),
					 mono_string_length (value),
					 usrcstr, count, coll, NULL, &ec);
	if(U_SUCCESS (ec)) {
		if(first) {
			pos=usearch_first (search, &ec);
		} else {
			pos=usearch_last (search, &ec);
		}

		while (pos!=USEARCH_DONE) {
			int32_t match_len;
			UChar *match;
			UCollationResult uret;
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Got potential match at %d (sindex %d) len %d", pos, sindex, usearch_getMatchedLength (search));
#endif

			/* ICU usearch currently ignores most of the
			 * collator attributes :-(
			 *
			 * Check the returned match to see if it
			 * really does match properly...
			 */
			match_len = usearch_getMatchedLength (search);
			match=(UChar *)g_malloc0 (sizeof(UChar) * (match_len + 1));
			usearch_getMatchedText (search, match, match_len, &ec);

			uret = ucol_strcoll (coll, match, match_len,
					     mono_string_chars (value),
					     mono_string_length (value));
			g_free (match);
			
			if (uret == UCOL_EQUAL) {
				/* OK, we really did get a match */
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": Got match at %d len %d", pos,
					   match_len);
#endif

				if(sindex>0) {
					if(first) {
						pos+=sindex;
					} else {
						pos+=(sindex-count+1);
					}
				}

				break;
			}

			/* False alarm, keep looking */
			if(first) {
				pos=usearch_next (search, &ec);
			} else {
				pos=usearch_previous (search, &ec);
			}	
		}
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": usearch_open error: %s",
			   u_errorName (ec));
	}

	usearch_close (search);
	
	mono_monitor_exit ((MonoObject *)this);
	
	g_free (usrcstr);

	return(pos);
}

int ves_icall_System_Globalization_CompareInfo_internal_index_char (MonoCompareInfo *this, MonoString *source, gint32 sindex, gint32 count, gunichar2 value, gint32 options, MonoBoolean first)
{
	UCollator *coll;
	UChar *usrcstr, uvalstr[2]={0, 0};
	UErrorCode ec;
	UStringSearch *search;
	int32_t pos= -1;
	
	MONO_ARCH_SAVE_REGS;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Finding %s 0x%0x in [%s] (sindex %d,count %d)", first?"first":"last", value, mono_string_to_utf8 (source), sindex, count);
#endif

	coll=this->ICU_collator;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", this->lcid);
#endif
	
	if(coll==NULL || this->lcid==0x007F ||
	   options & CompareOptions_Ordinal) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": No collator or invariant, using shortcut");
#endif

		return(string_invariant_indexof_char (source, sindex, count,
						      value, first));
	}
	
	usrcstr=g_malloc0 (sizeof(UChar)*(count+1));
	if(first) {
		memcpy (usrcstr, mono_string_chars (source)+sindex,
			sizeof(UChar)*count);
	} else {
		memcpy (usrcstr, mono_string_chars (source)+sindex-count+1,
			sizeof(UChar)*count);
	}
	uvalstr[0]=value;
	
	if (!mono_monitor_enter ((MonoObject *)this))
		return(-1);
	
	ec=U_ZERO_ERROR;
	
	/* Need to set the collator to a fairly weak level, so that it
	 * treats characters that can be written differently as
	 * identical (eg "ß" and "ss", "æ" and "ae" or "ä" etc.)  Note
	 * that this means that the search string and the original
	 * text might have differing lengths.
	 */
	ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_PRIMARY, &ec);

	/* Still notice case differences though (normally a tertiary
	 * difference)
	 */
	ucol_setAttribute (coll, UCOL_CASE_LEVEL, UCOL_ON, &ec);

	/* Don't ignore some codepoints */
	ucol_setAttribute (coll, UCOL_ALTERNATE_HANDLING, UCOL_NON_IGNORABLE,
			   &ec);
			
	search=usearch_openFromCollator (uvalstr, 1, usrcstr, count, coll,
					 NULL, &ec);
	if(U_SUCCESS (ec)) {
		if(first) {
			pos=usearch_first (search, &ec);
		} else {
			pos=usearch_last (search, &ec);
		}

		while (pos!=USEARCH_DONE) {
			int32_t match_len;
			UChar *match;
			UCollationResult uret;
			
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Got potential match at %d (sindex %d) len %d", pos, sindex, usearch_getMatchedLength (search));
#endif

			/* ICU usearch currently ignores most of the
			 * collator attributes :-(
			 *
			 * Check the returned match to see if it
			 * really does match properly...
			 */
			match_len = usearch_getMatchedLength (search);
			match=(UChar *)g_malloc0 (sizeof(UChar) * (match_len + 1));
			usearch_getMatchedText (search, match, match_len, &ec);

			uret = ucol_strcoll (coll, match, match_len, uvalstr,
					     1);
			g_free (match);
			
			if (uret == UCOL_EQUAL) {
				/* OK, we really did get a match */
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": Got match at %d len %d", pos,
					   match_len);
#endif

				if(sindex>0) {
					if(first) {
						pos+=sindex;
					} else {
						pos+=(sindex-count+1);
					}
				}

				break;
			}

			/* False alarm, keep looking */
			if(first) {
				pos=usearch_next (search, &ec);
			} else {
				pos=usearch_previous (search, &ec);
			}	
		}
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": usearch_open error: %s",
			   u_errorName (ec));
	}

	usearch_close (search);
	
	mono_monitor_exit ((MonoObject *)this);
	
	g_free (usrcstr);

	return(pos);
}

int ves_icall_System_Threading_Thread_current_lcid (void)
{
	MONO_ARCH_SAVE_REGS;

	return(uloc_getLCID (uloc_getDefault ()));
}

MonoString *ves_icall_System_String_InternalReplace_Str_Comp (MonoString *this, MonoString *old, MonoString *new, MonoCompareInfo *comp)
{
	MonoString *ret=NULL;
	UCollator *coll;
	UErrorCode ec;
	UStringSearch *search;
	
	MONO_ARCH_SAVE_REGS;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Replacing [%s] with [%s] in [%s]", mono_string_to_utf8 (old), mono_string_to_utf8 (new), mono_string_to_utf8 (this));
#endif

	coll=comp->ICU_collator;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", comp->lcid);
#endif
	
	if(coll==NULL || comp->lcid==0x007F) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": No collator or invariant, using shortcut");
#endif

		return(string_invariant_replace (this, old, new));
	}
	
	if (!mono_monitor_enter ((MonoObject *)comp))
		return(NULL);
	
	ec=U_ZERO_ERROR;
	
	/* Need to set the collator to a fairly weak level, so that it
	 * treats characters that can be written differently as
	 * identical (eg "ß" and "ss", "æ" and "ae" or "ä" etc.)  Note
	 * that this means that the search string and the original
	 * text might have differing lengths.
	 */
	ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_PRIMARY, &ec);

	/* Still notice case differences though (normally a tertiary
	 * difference)
	 */
	ucol_setAttribute (coll, UCOL_CASE_LEVEL, UCOL_ON, &ec);

	/* Don't ignore some codepoints */
	ucol_setAttribute (coll, UCOL_ALTERNATE_HANDLING, UCOL_NON_IGNORABLE,
			   &ec);
			
	search=usearch_openFromCollator (mono_string_chars (old),
					 mono_string_length (old),
					 mono_string_chars (this),
					 mono_string_length (this),
					 coll, NULL, &ec);
	if(U_SUCCESS (ec)) {
		int pos, oldpos, len_delta=0;
		int32_t newstr_len=mono_string_length (new), match_len;
		UChar *uret, *match;
		
		for(pos=usearch_first (search, &ec);
		    pos!=USEARCH_DONE;
		    pos=usearch_next (search, &ec)) {
			/* ICU usearch currently ignores most of the collator
			 * attributes :-(
			 *
			 * Check the returned match to see if it really
			 * does match properly...
			 */
			match_len = usearch_getMatchedLength (search);

			if(match_len == 0) {
				continue;
			}
			
			match=(UChar *)g_malloc0 (sizeof(UChar) * (match_len + 1));
			usearch_getMatchedText (search, match, match_len, &ec);

			if (ucol_strcoll (coll, match, match_len,
					  mono_string_chars (old),
					  mono_string_length (old)) == UCOL_EQUAL) {
				/* OK, we really did get a match */
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": Got match at %d len %d", pos,
					   match_len);
#endif

				len_delta += (newstr_len - match_len);
			} else {
				/* False alarm */
#ifdef DEBUG
				g_message (G_GNUC_PRETTY_FUNCTION
					   ": Got false match at %d len %d",
					   pos, match_len);
#endif
			}
			g_free (match);
		}
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": New string length is %d (delta %d)",
			   mono_string_length (this)+len_delta, len_delta);
#endif
		
		uret=(UChar *)g_malloc0 (sizeof(UChar) * (mono_string_length (this)+len_delta+2));
		
		for(oldpos=0, pos=usearch_first (search, &ec);
		    pos!=USEARCH_DONE;
		    pos=usearch_next (search, &ec)) {
			match_len = usearch_getMatchedLength (search);

			if (match_len == 0) {
				continue;
			}
			
			match=(UChar *)g_malloc0 (sizeof(UChar) * (match_len + 1));
			usearch_getMatchedText (search, match, match_len, &ec);

			/* Add the unmatched text */
			u_strncat (uret, mono_string_chars (this)+oldpos,
				   pos-oldpos);
			if (ucol_strcoll (coll, match, match_len,
					  mono_string_chars (old),
					  mono_string_length (old)) == UCOL_EQUAL) {
				/* Then the replacement */
				u_strcat (uret, mono_string_chars (new));
			} else {
				/* Then the original, because this is a
				 * false match
				 */
				u_strncat (uret, mono_string_chars (this)+pos,
					   match_len);
			}
			oldpos=pos+match_len;
			g_free (match);
		}
		
		/* Finish off with the trailing unmatched text */
		u_strcat (uret, mono_string_chars (this)+oldpos);

		ret=mono_string_from_utf16 ((gunichar2 *)uret);
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": usearch_open error: %s",
			   u_errorName (ec));
	}

	usearch_close (search);
	
	mono_monitor_exit ((MonoObject *)comp);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Replacing [%s] with [%s] in [%s] returns [%s]", mono_string_to_utf8 (old), mono_string_to_utf8 (new), mono_string_to_utf8 (this), mono_string_to_utf8 (ret));
#endif
	
	return(ret);
}

MonoString *ves_icall_System_String_InternalToLower_Comp (MonoString *this, MonoCultureInfo *cult)
{
	MonoString *ret;
	UChar *udest;
	UErrorCode ec;
	char *icu_loc;
	int32_t len;

	MONO_ARCH_SAVE_REGS;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": [%s]",
		   mono_string_to_utf8 (this));
#endif

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", cult->lcid);
#endif

	icu_loc=mono_string_to_icu_locale (cult->icu_name);
	if(icu_loc==NULL) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
		return(NULL);
	}
	
	udest=(UChar *)g_malloc0 (sizeof(UChar)*(mono_string_length (this)+1));
	
	/* According to the docs, this might result in a longer or
	 * shorter string than we started with...
	 */

	ec=U_ZERO_ERROR;
	len=u_strToLower (udest, mono_string_length (this)+1,
			  mono_string_chars (this), mono_string_length (this),
			  icu_loc, &ec);
	if(ec==U_BUFFER_OVERFLOW_ERROR ||
	   ec==U_STRING_NOT_TERMINATED_WARNING) {
		g_free (udest);
		udest=(UChar *)g_malloc0 (sizeof(UChar)*(len+1));
		len=u_strToLower (udest, len+1, mono_string_chars (this),
				  mono_string_length (this), icu_loc, &ec);
	}

	if(U_SUCCESS (ec)) {
		ret=mono_string_from_utf16 ((gunichar2 *)udest);
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": u_strToLower error: %s",
			   u_errorName (ec));
		/* return something */
		ret=this;
	}
	
	g_free (icu_loc);
	g_free (udest);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning [%s]",
		   mono_string_to_utf8 (ret));
#endif

	return(ret);
}

MonoString *ves_icall_System_String_InternalToUpper_Comp (MonoString *this, MonoCultureInfo *cult)
{
	MonoString *ret;
	UChar *udest;
	UErrorCode ec;
	char *icu_loc;
	int32_t len;

	MONO_ARCH_SAVE_REGS;

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": [%s]",
		   mono_string_to_utf8 (this));
#endif

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", cult->lcid);
#endif

	icu_loc=mono_string_to_icu_locale (cult->icu_name);
	if(icu_loc==NULL) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
		return(NULL);
	}
	
	udest=(UChar *)g_malloc0 (sizeof(UChar)*(mono_string_length (this)+1));
	
	/* According to the docs, this might result in a longer or
	 * shorter string than we started with...
	 */

	ec=U_ZERO_ERROR;
	len=u_strToUpper (udest, mono_string_length (this)+1,
			  mono_string_chars (this), mono_string_length (this),
			  icu_loc, &ec);
	if(ec==U_BUFFER_OVERFLOW_ERROR ||
	   ec==U_STRING_NOT_TERMINATED_WARNING) {
		g_free (udest);
		udest=(UChar *)g_malloc0 (sizeof(UChar)*(len+1));
		len=u_strToUpper (udest, len+1, mono_string_chars (this),
				  mono_string_length (this), icu_loc, &ec);
	}

	if(U_SUCCESS (ec)) {
		ret=mono_string_from_utf16 ((gunichar2 *)udest);
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": u_strToUpper error: %s",
			   u_errorName (ec));
		/* return something */
		ret=this;
	}
	
	g_free (icu_loc);
	g_free (udest);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning [%s]",
		   mono_string_to_utf8 (ret));
#endif
	
	return(ret);
}

gunichar2 ves_icall_System_Char_InternalToUpper_Comp (gunichar2 c, MonoCultureInfo *cult)
{
	UChar udest;
	UErrorCode ec;
	char *icu_loc;
	int32_t len;
	
	MONO_ARCH_SAVE_REGS;

	icu_loc=mono_string_to_icu_locale (cult->icu_name);
	if(icu_loc==NULL) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
		return(0);
	}
	
	ec=U_ZERO_ERROR;
	len=u_strToUpper (&udest, 1, &c, 1, icu_loc, &ec);
	g_free (icu_loc);

	if(U_SUCCESS (ec) && len==1) {
		return udest;
	} else {
		/* return something */
		return c;
	}
}


gunichar2 ves_icall_System_Char_InternalToLower_Comp (gunichar2 c, MonoCultureInfo *cult)
{
	UChar udest;
	UErrorCode ec;
	char *icu_loc;
	int32_t len;
	
	MONO_ARCH_SAVE_REGS;

	icu_loc=mono_string_to_icu_locale (cult->icu_name);
	if(icu_loc==NULL) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_get_corlib (), "System", "SystemException"));
		return(0);
	}
	
	ec=U_ZERO_ERROR;
	len=u_strToLower (&udest, 1, &c, 1, icu_loc, &ec);
	g_free (icu_loc);

	if(U_SUCCESS (ec) && len==1) {
		return udest;
	} else {
		/* return something */
		return c;
	}
}

#else /* HAVE_ICU */
static MonoString *string_invariant_tolower (MonoString *this);
static MonoString *string_invariant_toupper (MonoString *this);

void ves_icall_System_Globalization_CultureInfo_construct_internal_locale (MonoCultureInfo *this, MonoString *locale)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Always claim "unknown locale" if we don't have ICU (only
	 * called for non-invariant locales)
	 */
	mono_raise_exception((MonoException *)mono_exception_from_name(mono_get_corlib (), "System", "ArgumentException"));
}

void ves_icall_System_Globalization_CompareInfo_construct_compareinfo (MonoCompareInfo *comp, MonoString *locale)
{
	/* Nothing to do here */
}

int ves_icall_System_Globalization_CompareInfo_internal_compare (MonoCompareInfo *this, MonoString *str1, gint32 off1, gint32 len1, MonoString *str2, gint32 off2, gint32 len2, gint32 options)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Do a normal ascii string compare, as we only know the
	 * invariant locale if we dont have ICU
	 */
	return(string_invariant_compare (str1, off1, len1, str2, off2, len2,
					 options));
}

void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoCompareInfo *this)
{
	/* Nothing to do here */
}

void ves_icall_System_Globalization_CompareInfo_assign_sortkey (MonoCompareInfo *this, MonoSortKey *key, MonoString *source, gint32 options)
{
	MonoArray *arr;
	gint32 keylen, i;

	MONO_ARCH_SAVE_REGS;
	
	keylen=mono_string_length (source);
	
	arr=mono_array_new (mono_domain_get (), mono_get_byte_class (),
			    keylen);
	for(i=0; i<keylen; i++) {
		mono_array_set (arr, guint8, i, mono_string_chars (source)[i]);
	}
	
	key->key=arr;
}

int ves_icall_System_Globalization_CompareInfo_internal_index (MonoCompareInfo *this, MonoString *source, gint32 sindex, gint32 count, MonoString *value, gint32 options, MonoBoolean first)
{
	MONO_ARCH_SAVE_REGS;
	
	return(string_invariant_indexof (source, sindex, count, value, first));
}

int ves_icall_System_Globalization_CompareInfo_internal_index_char (MonoCompareInfo *this, MonoString *source, gint32 sindex, gint32 count, gunichar2 value, gint32 options, MonoBoolean first)
{
	MONO_ARCH_SAVE_REGS;
	
	return(string_invariant_indexof_char (source, sindex, count, value,
					      first));
}

int ves_icall_System_Threading_Thread_current_lcid (void)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Invariant */
	return(0x007F);
}

MonoString *ves_icall_System_String_InternalReplace_Str_Comp (MonoString *this, MonoString *old, MonoString *new, MonoCompareInfo *comp)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Do a normal ascii string compare and replace, as we only
	 * know the invariant locale if we dont have ICU
	 */
	return(string_invariant_replace (this, old, new));
}

MonoString *ves_icall_System_String_InternalToLower_Comp (MonoString *this, MonoCultureInfo *cult)
{
	MONO_ARCH_SAVE_REGS;
	
	return(string_invariant_tolower (this));
}

MonoString *ves_icall_System_String_InternalToUpper_Comp (MonoString *this, MonoCultureInfo *cult)
{
	MONO_ARCH_SAVE_REGS;
	
	return(string_invariant_toupper (this));
}

gunichar2 ves_icall_System_Char_InternalToUpper_Comp (gunichar2 c, MonoCultureInfo *cult)
{
	MONO_ARCH_SAVE_REGS;

	return g_unichar_toupper (c);
}


gunichar2 ves_icall_System_Char_InternalToLower_Comp (gunichar2 c, MonoCultureInfo *cult)
{
	MONO_ARCH_SAVE_REGS;

	return g_unichar_tolower (c);
}

static MonoString *string_invariant_tolower (MonoString *this)
{
	MonoString *ret;
	gunichar2 *src; 
	gunichar2 *dest;
	gint32 i;

	ret = mono_string_new_size(mono_domain_get (),
				   mono_string_length(this));

	src = mono_string_chars (this);
	dest = mono_string_chars (ret);

	for (i = 0; i < mono_string_length (this); ++i) {
		dest[i] = g_unichar_tolower(src[i]);
	}

	return(ret);
}

static MonoString *string_invariant_toupper (MonoString *this)
{
	MonoString *ret;
	gunichar2 *src; 
	gunichar2 *dest;
	guint32 i;

	ret = mono_string_new_size(mono_domain_get (),
				   mono_string_length(this));

	src = mono_string_chars (this);
	dest = mono_string_chars (ret);

	for (i = 0; i < mono_string_length (this); ++i) {
		dest[i] = g_unichar_toupper(src[i]);
	}

	return(ret);
}

#endif /* HAVE_ICU */

static gint32 string_invariant_compare_char (gunichar2 c1, gunichar2 c2,
					     gint32 options)
{
	gint32 result;
	GUnicodeType c1type, c2type;

	c1type = g_unichar_type (c1);
	c2type = g_unichar_type (c2);

	if (options & CompareOptions_IgnoreCase) {
		result = (gint32) (c1type != G_UNICODE_LOWERCASE_LETTER ? g_unichar_tolower(c1) : c1) - (c2type != G_UNICODE_LOWERCASE_LETTER ? g_unichar_tolower(c2) : c2);
	} else if (options & CompareOptions_Ordinal) {
		/*  Rotor/ms return the full value just not -1 and 1 */
		return (gint32) c1 - c2;
	} else {
		/* No options. Kana, symbol and spacing options don't
		 * apply to the invariant culture.
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

static MonoString *string_invariant_replace (MonoString *me,
					     MonoString *oldValue,
					     MonoString *newValue)
{
	MonoString *ret;
	gunichar2 *src;
	gunichar2 *dest=NULL; /* shut gcc up */
	gunichar2 *oldstr;
	gunichar2 *newstr=NULL; /* shut gcc up here too */
	gint32 i, destpos;
	gint32 occurr;
	gint32 newsize;
	gint32 oldstrlen;
	gint32 newstrlen;
	gint32 srclen;

	occurr = 0;
	destpos = 0;

	oldstr = mono_string_chars(oldValue);
	oldstrlen = mono_string_length(oldValue);

	if (NULL != newValue) {
		newstr = mono_string_chars(newValue);
		newstrlen = mono_string_length(newValue);
	} else
		newstrlen = 0;

	src = mono_string_chars(me);
	srclen = mono_string_length(me);

	if (oldstrlen != newstrlen) {
		i = 0;
		while (i <= srclen - oldstrlen) {
			if (0 == memcmp(src + i, oldstr, oldstrlen * sizeof(gunichar2))) {
				occurr++;
				i += oldstrlen;
			}
			else
				i ++;
		}
		if (occurr == 0)
			return me;
		newsize = srclen + ((newstrlen - oldstrlen) * occurr);
	} else
		newsize = srclen;

	ret = NULL;
	i = 0;
	while (i < srclen) {
		if (0 == memcmp(src + i, oldstr, oldstrlen * sizeof(gunichar2))) {
			if (ret == NULL) {
				ret = mono_string_new_size( mono_domain_get (), newsize);
				dest = mono_string_chars(ret);
				memcpy (dest, src, i * sizeof(gunichar2));
			}
			if (newstrlen > 0) {
				memcpy(dest + destpos, newstr, newstrlen * sizeof(gunichar2));
				destpos += newstrlen;
			}
			i += oldstrlen;
			continue;
		} else if (ret != NULL) {
			dest[destpos] = src[i];
		}
		destpos++;
		i++;
	}
	
	if (ret == NULL)
		return me;

	return ret;
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

