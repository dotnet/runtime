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

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/locales.h>

#undef DEBUG

static gint32 string_invariant_compare_char (gunichar2 c1, gunichar2 c2,
					     gint32 options);
static gint32 string_invariant_compare (MonoString *str1, MonoString *str2,
					gint32 options);
static MonoString *string_invariant_replace (MonoString *me,
					     MonoString *oldValue,
					     MonoString *newValue);
static gint32 string_invariant_indexof (MonoString *source, gint32 sindex,
					gint32 count, MonoString *value,
					MonoBoolean first);
static MonoString *string_invariant_tolower (MonoString *this);
static MonoString *string_invariant_toupper (MonoString *this);

static void set_field_by_name (MonoObject *obj, const guchar *fieldname,
			       gpointer value)
{
	MonoClassField *field;

	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	if(field!=NULL) {
		mono_field_set_value (obj, field, value);
	} else {
		g_warning (G_GNUC_PRETTY_FUNCTION ": Runtime mismatch with class lib! (Looking for field [%s] in %s.%s)", fieldname, mono_object_class (obj)->name_space, mono_object_class (obj)->name);
	}
}

static gpointer get_field_by_name (MonoObject *obj, const guchar *fieldname)
{
	MonoClassField *field;
	gpointer ret;
	
	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);

	if(field==NULL) {
		g_warning (G_GNUC_PRETTY_FUNCTION ": Runtime mismatch with class lib! (Looking for field [%s] in %s.%s)", fieldname, mono_object_class (obj)->name_space, mono_object_class (obj)->name);
		return(NULL);
	}
	
	mono_field_get_value (obj, field, &ret);
	return(ret);
}

#ifdef HAVE_ICU

#include <unicode/utypes.h>
#include <unicode/ustring.h>
#include <unicode/ures.h>
#include <unicode/ucnv.h>
#include <unicode/ucol.h>
#include <unicode/usearch.h>

static MonoString *monostring_from_UChars (const UChar *res_str,
					   UConverter *conv)
{
	MonoString *str;
	UErrorCode ec;
	char *utf16_str;
	int32_t ret, utf16_strlen;
	
	utf16_strlen=u_strlen (res_str)*ucnv_getMaxCharSize (conv)+2;
	utf16_str=(char *)g_malloc0 (sizeof(char)*utf16_strlen);
	
	ec=U_ZERO_ERROR;
	ret=ucnv_fromUChars (conv, utf16_str, utf16_strlen, res_str, -1, &ec);
	if(ec==U_BUFFER_OVERFLOW_ERROR ||
	   ec==U_STRING_NOT_TERMINATED_WARNING) {
		/* This should never happen, cos we gave ourselves the
		 * maximum length needed above
		 */
		g_assert_not_reached ();
	}
	
	str=mono_string_from_utf16 ((gunichar2 *)utf16_str);
	
	g_free (utf16_str);
	
	return(str);
}

static UChar *monostring_to_UChars (const MonoString *str, gint32 sindex,
				    gint32 count, UConverter *conv)
{
	UErrorCode ec;
	UChar *dest;
	int32_t ret;
	
	if(count<0) {
		count=mono_string_length (str);
	}
	if(sindex<0) {
		sindex=0;
	}
	if(sindex+count > mono_string_length (str)) {
		count=mono_string_length (str)-sindex;
	}
	

	/* Add 1 for the trailing NULL */
	dest=(UChar *)g_malloc0 (sizeof(UChar)*(count+1));
	
	/* count*2 because its counting bytes not chars */
	ec=U_ZERO_ERROR;
	ret=ucnv_toUChars (conv, dest, count+1, (const char *)(mono_string_chars (str)+sindex), count*2, &ec);
	if(ec==U_BUFFER_OVERFLOW_ERROR ||
	   ec==U_STRING_NOT_TERMINATED_WARNING) {
		/* This should never happen, cos we gave ourselves the
		 * length needed above
		 */
		g_assert_not_reached ();
	}
	
	return(dest);
}

static MonoString *monostring_from_resource_index (const UResourceBundle *bundle, UConverter *conv, int32_t idx)
{
	const UChar *res_str;
	int32_t res_strlen;
	UErrorCode ec;
	
	ec=U_ZERO_ERROR;
	res_str=ures_getStringByIndex (bundle, idx, &res_strlen, &ec);
	if(U_FAILURE (ec)) {
		return(NULL);
	}

	return(monostring_from_UChars (res_str, conv));
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

static void set_array (MonoObject *obj, const guchar *fieldname,
		       const UResourceBundle *bundle, const char *resname,
		       int32_t req_count, UConverter *conv)
{
	MonoArray *arr;
	UResourceBundle *subbundle;
	int i;
	
	subbundle=open_subbundle (bundle, resname, req_count);
	if(subbundle!=NULL) {
		arr=mono_array_new(mono_domain_get (),
				   mono_defaults.string_class, req_count);
		
		for(i=0; i<req_count; i++) {
			mono_array_set(arr, MonoString *, i, monostring_from_resource_index (subbundle, conv, i));
		}
		set_field_by_name (obj, fieldname, arr);

		ures_close (subbundle);
	}
}


static MonoObject *create_DateTimeFormat (const char *locale)
{
	MonoObject *new_dtf;
	MonoClass *class;
	UConverter *conv;
	UResourceBundle *bundle, *subbundle;
	UErrorCode ec;
	
	class=mono_class_from_name (mono_defaults.corlib,
				    "System.Globalization",
				    "DateTimeFormatInfo");
	new_dtf=mono_object_new (mono_domain_get (), class);
	mono_runtime_object_init (new_dtf);
	
	ec=U_ZERO_ERROR;

	/* Plain "UTF-16" adds a BOM, which confuses other stuff */
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		goto error0;
	}
	
	bundle=ures_open (NULL, locale, &ec);
	if(U_FAILURE (ec)) {
		goto error1;
	}
	
	/* AM/PM markers */
	subbundle=open_subbundle (bundle, "AmPmMarkers", 2);
	if(subbundle!=NULL) {
		set_field_by_name (new_dtf, "_AMDesignator",
				   monostring_from_resource_index (subbundle,
								   conv, 0));
		set_field_by_name (new_dtf, "_PMDesignator",
				   monostring_from_resource_index (subbundle,
								   conv, 1));
		
		ures_close (subbundle);
	}
	
	/* Date/Time patterns.  Don't set FullDateTimePattern.  As it
	 * seems to always default to LongDatePattern + " " +
	 * LongTimePattern, let the property accessor deal with it.
	 */
	subbundle=open_subbundle (bundle, "DateTimePatterns", 9);
	if(subbundle!=NULL) {
		set_field_by_name (new_dtf, "_ShortDatePattern",
				   monostring_from_resource_index (subbundle,
								   conv, 7));
		set_field_by_name (new_dtf, "_LongDatePattern",
				   monostring_from_resource_index (subbundle,
								   conv, 5));
		set_field_by_name (new_dtf, "_ShortTimePattern",
				   monostring_from_resource_index (subbundle,
								   conv, 3));
		set_field_by_name (new_dtf, "_LongTimePattern",
				   monostring_from_resource_index (subbundle,
								   conv, 2));

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
	set_array (new_dtf, "_DayNames", bundle, "DayNames", 7, conv);

	/* Abbreviated day names */
	set_array (new_dtf, "_AbbreviatedDayNames", bundle,
		   "DayAbbreviations", 7, conv);

	/* Month names */
	set_array (new_dtf, "_MonthNames", bundle, "MonthNames", 12, conv);
	
	/* Abbreviated month names */
	set_array (new_dtf, "_AbbreviatedMonthNames", bundle,
		   "MonthAbbreviations", 12, conv);

	/* TODO: DayOfWeek _FirstDayOfWeek, Calendar _Calendar, CalendarWeekRule _CalendarWeekRule */

	ures_close (bundle);
error1:
	ucnv_close (conv);
error0:
	return(new_dtf);
}

static MonoObject *create_NumberFormat (const char *locale)
{
	MonoObject *new_nf;
	MonoClass *class;
	MonoMethodDesc* methodDesc;
	MonoMethod *method;
	UConverter *conv;
	UResourceBundle *bundle, *subbundle, *table_entries;
	UErrorCode ec;
	int32_t count;
	static char country [7]; //FIXME
	const UChar *res_str;
	int32_t res_strlen;

	class=mono_class_from_name (mono_defaults.corlib,
				    "System.Globalization",
				    "NumberFormatInfo");
	new_nf=mono_object_new (mono_domain_get (), class);
	mono_runtime_object_init (new_nf);

	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		goto error0;
	}

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
		set_field_by_name (new_nf, "numberDecimalSeparator",
				   monostring_from_resource_index (subbundle,
								   conv, 0));
		set_field_by_name (new_nf, "numberGroupSeparator",
				   monostring_from_resource_index (subbundle,
								   conv, 1));
		set_field_by_name (new_nf, "percentDecimalSeparator",
				   monostring_from_resource_index (subbundle,
								   conv, 0));
		set_field_by_name (new_nf, "percentGroupSeparator",
				   monostring_from_resource_index (subbundle,
								   conv, 1));
		set_field_by_name (new_nf, "percentSymbol",
				   monostring_from_resource_index (subbundle,
								   conv, 3));
		set_field_by_name (new_nf, "zeroPattern",
				   monostring_from_resource_index (subbundle,
								   conv, 4));
		set_field_by_name (new_nf, "digitPattern",
				   monostring_from_resource_index (subbundle,
								   conv, 5));
		set_field_by_name (new_nf, "negativeSign",
				   monostring_from_resource_index (subbundle,
								   conv, 6));
		set_field_by_name (new_nf, "perMilleSymbol",
				   monostring_from_resource_index (subbundle,
								   conv, 8));
		set_field_by_name (new_nf, "positiveInfinitySymbol",
				   monostring_from_resource_index (subbundle,
								   conv, 9));
		/* we dont have this in CLDR, so copy it from positiveInfinitySymbol */
		set_field_by_name (new_nf, "negativeInfinitySymbol",
				   monostring_from_resource_index (subbundle,
								   conv, 9));
		set_field_by_name (new_nf, "naNSymbol",
				   monostring_from_resource_index (subbundle,
								   conv, 10)); 
		set_field_by_name (new_nf, "currencyDecimalSeparator",
				   monostring_from_resource_index (subbundle,
								   conv, 0));
		set_field_by_name (new_nf, "currencyGroupSeparator",
				   monostring_from_resource_index (subbundle,
								   conv, 1));

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
								set_field_by_name (new_nf, "currencySymbol",
										   monostring_from_resource_index (table_entries,
														   conv, 0));
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
		set_field_by_name (new_nf, "decimalFormats",
				   monostring_from_resource_index (subbundle,
								   conv, 0));
		set_field_by_name (new_nf, "currencyFormats",
				   monostring_from_resource_index (subbundle,
								   conv, 1));
		set_field_by_name (new_nf, "percentFormats",
				   monostring_from_resource_index (subbundle,
								   conv, 2));			
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
	ucnv_close (conv);
error0:
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

void ves_icall_System_Globalization_CultureInfo_construct_internal_locale (MonoObject *this, MonoString *locale)
{
	UConverter *conv;
	UChar *ustr;
	char *str;
	UErrorCode ec;
	char *icu_locale;
	int32_t str_len, ret;
	
	MONO_ARCH_SAVE_REGS;
	
	icu_locale=mono_string_to_icu_locale (locale);
	if(icu_locale==NULL) {
		/* Something went wrong */
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		return;
	}
	
	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		g_free (icu_locale);
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		return;
	}
	
	/* Fill in the static fields */

	/* TODO: Calendar, InstalledUICulture, OptionalCalendars,
	 * TextInfo
	 */

	str_len=256;	/* Should be big enough for anything */
	str=(char *)g_malloc0 (sizeof(char)*str_len);
	ustr=(UChar *)g_malloc0 (sizeof(UChar)*str_len);
	
	ret=uloc_getDisplayName (icu_locale, "en", ustr, str_len, &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		set_field_by_name (this, "englishname",
				   monostring_from_UChars (ustr, conv));
	}
	
	ret=uloc_getDisplayName (icu_locale, uloc_getDefault (), ustr, str_len,
				 &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		set_field_by_name (this, "displayname",
				   monostring_from_UChars (ustr, conv));
	}
	
	ret=uloc_getDisplayName (icu_locale, icu_locale, ustr, str_len, &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		set_field_by_name (this, "nativename",
				   monostring_from_UChars (ustr, conv));
	}

	set_field_by_name (this, "iso3lang", mono_string_new_wrapper (uloc_getISO3Language (icu_locale)));

	ret=uloc_getLanguage (icu_locale, str, str_len, &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		set_field_by_name (this, "iso2lang",
				   mono_string_new_wrapper (str));
	}

	set_field_by_name (this, "datetime_format",
			   create_DateTimeFormat (icu_locale));
	
	set_field_by_name (this, "number_format",
			   create_NumberFormat (icu_locale));
 
	g_free (str);
	g_free (ustr);
	g_free (icu_locale);
	ucnv_close (conv);
}

void ves_icall_System_Globalization_CompareInfo_construct_compareinfo (MonoObject *comp, MonoString *locale)
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
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		return;
	}

	ec=U_ZERO_ERROR;
	coll=ucol_open (icu_locale, &ec);
	if(U_SUCCESS (ec)) {
		set_field_by_name (comp, "ICU_collator", &coll);
	}

	g_free (icu_locale);
}

/* Set up the collator to reflect the options required.  Some of these
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

gint32 ves_icall_System_Globalization_CompareInfo_internal_compare (MonoObject *this, MonoString *str1, MonoString *str2, gint32 options)
{
	UConverter *conv;
	UCollator *coll;
	UChar *ustr1, *ustr2;
	UCollationResult result;
	UErrorCode ec;
	guint32 coll_lcid;
	
	MONO_ARCH_SAVE_REGS;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Comparing [%s] and [%s]", mono_string_to_utf8 (str1), mono_string_to_utf8 (str2));
#endif

	coll=get_field_by_name (this, "ICU_collator");
	coll_lcid=GPOINTER_TO_UINT (get_field_by_name (this, "lcid"));

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", coll_lcid);
#endif
	
	if(coll==NULL || coll_lcid==0x007F ||
	   options & CompareOptions_Ordinal) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": No collator or invariant, using shortcut");
#endif

		return(string_invariant_compare (str1, str2, options));
	}
	
	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		return(0);
	}
	
	ustr1=monostring_to_UChars (str1, -1, -1, conv);
	ustr2=monostring_to_UChars (str2, -1, -1, conv);
	
	ucnv_close (conv);
	
	mono_monitor_try_enter (this, INFINITE);
	
	set_collator_options (coll, options);
			
	result=ucol_strcoll (coll, ustr1, -1, ustr2, -1);

	mono_monitor_exit (this);
	
	g_free (ustr1);
	g_free (ustr2);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Comparison of [%s] and [%s] returning %d", mono_string_to_utf8 (str1), mono_string_to_utf8 (str2), result);
#endif
	
	return(result);
}

void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoObject *this)
{
	UCollator *coll;
	
	MONO_ARCH_SAVE_REGS;
	
	coll=get_field_by_name (this, "ICU_collator");
	if(coll!=NULL) {
		ucol_close (coll);
	}
}

void ves_icall_System_Globalization_CompareInfo_assign_sortkey (MonoObject *this, MonoObject *key, MonoString *source, gint32 options)
{
	UCollator *coll;
	UConverter *conv;
	UChar *ustr;
	UErrorCode ec;
	MonoArray *arr;
	char *keybuf;
	int32_t keylen, i;
	
	MONO_ARCH_SAVE_REGS;
	
	coll=get_field_by_name (this, "ICU_collator");
	if(coll==NULL) {
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		return;
	}
	
	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		return;
	}
	ustr=monostring_to_UChars (source, -1, -1, conv);
	ucnv_close (conv);
	
	mono_monitor_try_enter (this, INFINITE);
	
	set_collator_options (coll, options);

	keylen=ucol_getSortKey (coll, ustr, -1, NULL, 0);
	keybuf=g_malloc (sizeof(char)* keylen);
	ucol_getSortKey (coll, ustr, -1, keybuf, keylen);

	mono_monitor_exit (this);
	
	arr=mono_array_new (mono_domain_get (), mono_defaults.byte_class,
			    keylen);
	for(i=0; i<keylen; i++) {
		mono_array_set (arr, guint8, i, keybuf[i]);
	}
	
	set_field_by_name (key, "key", arr);

	g_free (ustr);
	g_free (keybuf);
}

int ves_icall_System_Globalization_CompareInfo_internal_index (MonoObject *this, MonoString *source, gint32 sindex, gint32 count, MonoString *value, gint32 options, MonoBoolean first)
{
	UConverter *conv;
	UCollator *coll;
	UChar *usrcstr, *uvalstr;
	UErrorCode ec;
	UStringSearch *search;
	guint32 coll_lcid;
	int32_t pos= -1;
	
	MONO_ARCH_SAVE_REGS;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Finding %s [%s] in [%s] (sindex %d,count %d)", first?"first":"last", mono_string_to_utf8 (value), mono_string_to_utf8 (source), sindex, count);
#endif

	coll=get_field_by_name (this, "ICU_collator");
	coll_lcid=GPOINTER_TO_UINT (get_field_by_name (this, "lcid"));

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", coll_lcid);
#endif
	
	if(coll==NULL || coll_lcid==0x007F ||
	   options & CompareOptions_Ordinal) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": No collator or invariant, using shortcut");
#endif

		return(string_invariant_indexof (source, sindex, count, value,
						 first));
	}
	
	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		return(-1);
	}
	
	if(first) {
		usrcstr=monostring_to_UChars (source, sindex, count, conv);
	} else {
		usrcstr=monostring_to_UChars (source, sindex-count+1, count,
					       conv);
	}
	uvalstr=monostring_to_UChars (value, -1, -1, conv);

	ucnv_close (conv);
	
	mono_monitor_try_enter (this, INFINITE);
	
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
			
	search=usearch_openFromCollator (uvalstr, -1, usrcstr, -1, coll, NULL,
					 &ec);
	if(U_SUCCESS (ec)) {
		if(first) {
			pos=usearch_first (search, &ec);
		} else {
			pos=usearch_last (search, &ec);
		}

		if(pos!=USEARCH_DONE) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Got match at %d (sindex %d) len %d", pos,
				   sindex, usearch_getMatchedLength (search));
#endif
			if(sindex>0) {
				if(first) {
					pos+=sindex;
				} else {
					pos+=(sindex-count+1);
				}
			}
		}
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": usearch_open error: %s",
			   u_errorName (ec));
	}

	usearch_close (search);
	
	mono_monitor_exit (this);
	
	g_free (usrcstr);
	g_free (uvalstr);

	return(pos);
}

int ves_icall_System_Threading_Thread_current_lcid (void)
{
	MONO_ARCH_SAVE_REGS;
	
	return(uloc_getLCID (uloc_getDefault ()));
}

MonoString *ves_icall_System_String_InternalReplace_Str_Comp (MonoString *this, MonoString *old, MonoString *new, MonoObject *comp)
{
	MonoString *ret=NULL;
	UConverter *conv;
	UCollator *coll;
	UChar *utgtstr, *uoldstr, *unewstr;
	UErrorCode ec;
	UStringSearch *search;
	guint32 coll_lcid;
	
	MONO_ARCH_SAVE_REGS;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Replacing [%s] with [%s] in [%s]", mono_string_to_utf8 (old), mono_string_to_utf8 (new), mono_string_to_utf8 (this));
#endif

	coll=get_field_by_name (comp, "ICU_collator");
	coll_lcid=GPOINTER_TO_UINT (get_field_by_name (comp, "lcid"));

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", coll_lcid);
#endif
	
	if(coll==NULL || coll_lcid==0x007F) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": No collator or invariant, using shortcut");
#endif

		return(string_invariant_replace (this, old, new));
	}
	
	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		return(NULL);
	}
	
	utgtstr=monostring_to_UChars (this, -1, -1, conv);
	uoldstr=monostring_to_UChars (old, -1, -1, conv);
	unewstr=monostring_to_UChars (new, -1, -1, conv);
	
	mono_monitor_try_enter (comp, INFINITE);
	
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
			
	search=usearch_openFromCollator (uoldstr, -1, utgtstr, -1, coll, NULL,
					 &ec);
	if(U_SUCCESS (ec)) {
		int pos, oldpos, len_delta=0;
		int32_t newstr_len=u_strlen (unewstr);
		UChar *uret;
		
		for(pos=usearch_first (search, &ec);
		    pos!=USEARCH_DONE;
		    pos=usearch_next (search, &ec)) {
#ifdef DEBUG
			g_message (G_GNUC_PRETTY_FUNCTION
				   ": Got match at %d len %d", pos,
				   usearch_getMatchedLength (search));
#endif

			len_delta += (newstr_len -
				      usearch_getMatchedLength (search));
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
			/* Add the unmatched text */
			u_strncat (uret, utgtstr+oldpos, pos-oldpos);
			/* Then the replacement */
			u_strcat (uret, unewstr);
			oldpos=pos+usearch_getMatchedLength (search);
		}
		
		/* Finish off with the trailing unmatched text */
		u_strcat (uret, utgtstr+oldpos);

		ret=monostring_from_UChars (uret, conv);
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": usearch_open error: %s",
			   u_errorName (ec));
	}

	usearch_close (search);
	ucnv_close (conv);
	
	mono_monitor_exit (comp);
	
	g_free (utgtstr);
	g_free (uoldstr);
	g_free (unewstr);

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": Replacing [%s] with [%s] in [%s] returns [%s]", mono_string_to_utf8 (old), mono_string_to_utf8 (new), mono_string_to_utf8 (this), mono_string_to_utf8 (ret));
#endif
	
	return(ret);
}

MonoString *ves_icall_System_String_InternalToLower_Comp (MonoString *this,
							  MonoObject *cult)
{
	MonoString *locale, *ret;
	UConverter *conv;
	UChar *usrc, *udest;
	UErrorCode ec;
	char *icu_loc;
	guint32 lcid;
	int32_t len;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": [%s]",
		   mono_string_to_utf8 (this));
#endif

	lcid=GPOINTER_TO_UINT (get_field_by_name (cult, "lcid"));

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", lcid);
#endif

	if(lcid==0x007F) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Invariant, using shortcut");
#endif

		return(string_invariant_tolower (this));
	}

	locale=get_field_by_name (cult, "icu_name");
	icu_loc=mono_string_to_icu_locale (locale);
	if(icu_loc==NULL) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_defaults.corlib, "System", "SystemException"));
		return(NULL);
	}

	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_defaults.corlib, "System", "SystemException"));
		return(NULL);
	}
	
	usrc=monostring_to_UChars (this, -1, -1, conv);
	udest=(UChar *)g_malloc0 (sizeof(UChar)*(mono_string_length (this)+1));
	
	/* According to the docs, this might result in a longer or
	 * shorter string than we started with...
	 */
	len=u_strToLower (udest, mono_string_length (this)+1, usrc, -1,
			  icu_loc, &ec);
	if(ec==U_BUFFER_OVERFLOW_ERROR ||
	   ec==U_STRING_NOT_TERMINATED_WARNING) {
		g_free (udest);
		udest=(UChar *)g_malloc0 (sizeof(UChar)*(len+1));
		len=u_strToLower (udest, len+1, usrc, -1, icu_loc, &ec);
	}

	if(U_SUCCESS (ec)) {
		ret=monostring_from_UChars (udest, conv);
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": u_strToLower error: %s",
			   u_errorName (ec));
		/* return something */
		ret=this;
	}

	ucnv_close (conv);
	
	g_free (icu_loc);
	g_free (usrc);
	g_free (udest);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning [%s]",
		   mono_string_to_utf8 (ret));
#endif

	return(ret);
}

MonoString *ves_icall_System_String_InternalToUpper_Comp (MonoString *this,
							  MonoObject *cult)
{
	MonoString *locale, *ret;
	UConverter *conv;
	UChar *usrc, *udest;
	UErrorCode ec;
	char *icu_loc;
	guint32 lcid;
	int32_t len;
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": [%s]",
		   mono_string_to_utf8 (this));
#endif

	lcid=GPOINTER_TO_UINT (get_field_by_name (cult, "lcid"));

#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": LCID is %d", lcid);
#endif

	if(lcid==0x007F) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION
			   ": Invariant, using shortcut");
#endif

		return(string_invariant_toupper (this));
	}

	locale=get_field_by_name (cult, "icu_name");
	icu_loc=mono_string_to_icu_locale (locale);
	if(icu_loc==NULL) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_defaults.corlib, "System", "SystemException"));
		return(NULL);
	}

	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF16_PlatformEndian", &ec);
	if(U_FAILURE (ec)) {
		mono_raise_exception ((MonoException *)mono_exception_from_name (mono_defaults.corlib, "System", "SystemException"));
		return(NULL);
	}
	
	usrc=monostring_to_UChars (this, -1, -1, conv);
	udest=(UChar *)g_malloc0 (sizeof(UChar)*(mono_string_length (this)+1));
	
	/* According to the docs, this might result in a longer or
	 * shorter string than we started with...
	 */
	len=u_strToUpper (udest, mono_string_length (this)+1, usrc, -1,
			  icu_loc, &ec);
	if(ec==U_BUFFER_OVERFLOW_ERROR ||
	   ec==U_STRING_NOT_TERMINATED_WARNING) {
		g_free (udest);
		udest=(UChar *)g_malloc0 (sizeof(UChar)*(len+1));
		len=u_strToUpper (udest, len+1, usrc, -1, icu_loc, &ec);
	}

	if(U_SUCCESS (ec)) {
		ret=monostring_from_UChars (udest, conv);
	} else {
		g_message (G_GNUC_PRETTY_FUNCTION ": u_strToUpper error: %s",
			   u_errorName (ec));
		/* return something */
		ret=this;
	}

	ucnv_close (conv);
	
	g_free (icu_loc);
	g_free (usrc);
	g_free (udest);
	
#ifdef DEBUG
	g_message (G_GNUC_PRETTY_FUNCTION ": returning [%s]",
		   mono_string_to_utf8 (ret));
#endif
	
	return(ret);
}

#else /* HAVE_ICU */
void ves_icall_System_Globalization_CultureInfo_construct_internal_locale (MonoObject *this, MonoString *locale)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Always claim "unknown locale" if we don't have ICU (only
	 * called for non-invariant locales)
	 */
	mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "ArgumentException"));
}

void ves_icall_System_Globalization_CultureInfo_construct_compareinfo (MonoObject *comp, MonoString *locale)
{
	/* Nothing to do here */
}

int ves_icall_System_Globalization_CompareInfo_internal_compare (MonoObject *this, MonoString *str1, MonoString *str2, gint32 options)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Do a normal ascii string compare, as we only know the
	 * invariant locale if we dont have ICU
	 */
	return(string_invariant_compare (str1, str2, options));
}

void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoObject *this)
{
	/* Nothing to do here */
}

void ves_icall_System_Globalization_CompareInfo_assign_sortkey (MonoObject *this, MonoObject *key, MonoString *source, gint32 options)
{
	MonoArray *arr;
	gint32 keylen, i;

	MONO_ARCH_SAVE_REGS;
	
	keylen=mono_string_length (source);
	
	arr=mono_array_new (mono_domain_get (), mono_defaults.byte_class,
			    keylen);
	for(i=0; i<keylen; i++) {
		mono_array_set (arr, guint8, i, mono_string_chars (source)[i]);
	}
	
	set_field_by_name (key, "key", arr);
}

int ves_icall_System_Globalization_CompareInfo_internal_index (MonoObject *this, MonoString *source, gint32 sindex, gint32 count, MonoString *value, gint32 options, MonoBoolean first)
{
	MONO_ARCH_SAVE_REGS;
	
	return(string_invariant_indexof (source, sindex, count, value, first));
}

int ves_icall_System_Threading_Thread_current_lcid (void)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Invariant */
	return(0x007F);
}

MonoString *ves_icall_System_String_InternalReplace_Str_Comp (MonoString *this, MonoString *old, MonoString *new, MonoObject *comp)
{
	MONO_ARCH_SAVE_REGS;
	
	/* Do a normal ascii string compare and replace, as we only
	 * know the invariant locale if we dont have ICU
	 */
	return(string_invariant_replace (this, old, new));
}

MonoString *ves_icall_System_String_InternalToLower_Comp (MonoString *this,
							  MonoObject *cult)
{
	MONO_ARCH_SAVE_REGS;
	
	return(string_invariant_tolower (this));
}

MonoString *ves_icall_System_String_InternalToUpper_Comp (MonoString *this,
							  MonoObject *cult)
{
	MONO_ARCH_SAVE_REGS;
	
	return(string_invariant_toupper (this));
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
		// Rotor/ms return the full value just not -1 and 1
		return (gint32) c1 - c2;
	} else {
		/* No options. Kana, symbol and spacing options don't
		 * apply to the invariant culture.
		 */
		if (c1type == G_UNICODE_UPPERCASE_LETTER &&
		    c2type == G_UNICODE_LOWERCASE_LETTER) {
			return(1);
		}
					
		if (c1type == G_UNICODE_LOWERCASE_LETTER &&
		    c2type == G_UNICODE_UPPERCASE_LETTER) {
			return(-1);
		}
		
		result = (gint32) c1 - c2;
	}

	return ((result < 0) ? -1 : (result > 0) ? 1 : 0);
}

static gint32 string_invariant_compare (MonoString *str1, MonoString *str2,
					gint32 options)
{
	/* c translation of C# code from old string.cs.. :) */
	gint32 lenstr1;
	gint32 lenstr2;
	gint32 length;
	gint32 charcmp;
	gunichar2 *ustr1;
	gunichar2 *ustr2;
	gint32 pos;

	lenstr1 = mono_string_length(str1);
	lenstr2 = mono_string_length(str2);

	if(lenstr1 >= lenstr2) {
		length=lenstr1;
	} else {
		length=lenstr2;
	}

	ustr1 = mono_string_chars(str1);
	ustr2 = mono_string_chars(str2);

	pos = 0;

	for (pos = 0; pos != length; pos++) {
		if (pos >= lenstr1 || pos >= lenstr2)
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
	if (pos >= lenstr1) {
		if (pos >= lenstr2) {
			return(0);
		} else {
			return(-1);
		}
	} else if (pos >= lenstr2) {
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
		for (i = 0; i <= srclen - oldstrlen; i++)
			if (0 == memcmp(src + i, oldstr, oldstrlen * sizeof(gunichar2)))
				occurr++;
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

	lencmpstr = mono_string_length(value);
	
	src = mono_string_chars(source);
	cmpstr = mono_string_chars(value);

	if(first) {
		while(count >= lencmpstr) {
			if(memcmp (src+sindex, cmpstr,
				   lencmpstr * sizeof(gunichar2))==0) {
				return(sindex);
			}
			sindex++;
			count--;
		}
	} else {
		while(count >= lencmpstr) {
			if(memcmp (src+(sindex-lencmpstr+1), cmpstr,
				   lencmpstr * sizeof(gunichar2))==0) {
				return(sindex-lencmpstr+1);
			}
			sindex--;
			count--;
		}
	}
	
	return(-1);
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
