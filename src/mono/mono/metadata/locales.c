/*
 * locales.c: Culture-sensitive handling
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/locales.h>

#define DEBUG

static void set_field_by_name (MonoObject *obj, const guchar *fieldname,
			       gpointer value)
{
	MonoClassField *field;

	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	mono_field_set_value (obj, field, value);
}

static gpointer get_field_by_name (MonoObject *obj, const guchar *fieldname)
{
	MonoClassField *field;
	gpointer ret;
	
	field=mono_class_get_field_from_name (mono_object_class (obj),
					      fieldname);
	mono_field_get_value (obj, field, &ret);
	return(ret);
}

#ifdef HAVE_ICU

#include <unicode/utypes.h>
#include <unicode/ustring.h>
#include <unicode/ures.h>
#include <unicode/ucnv.h>
#include <unicode/ucol.h>

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

static UChar *monostring_to_UChars (const MonoString *str, UConverter *conv)
{
	UErrorCode ec;
	UChar *dest;
	int32_t ret, dest_strlen;
	
	/* Add 1 for the trailing NULL */
	dest_strlen=mono_string_length (str)+1;
	dest=(UChar *)g_malloc0 (sizeof(UChar)*dest_strlen);
	
	ec=U_ZERO_ERROR;
	/* mono_string_length()*2 because its counting bytes not chars */
	ret=ucnv_toUChars (conv, dest, dest_strlen,
			   (const char *)mono_string_chars (str),
			   mono_string_length (str)*2, &ec);
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
	conv=ucnv_open ("UTF-16LE", &ec);
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
	conv=ucnv_open ("UTF-16LE", &ec);
	if(U_FAILURE (ec)) {
		g_free (icu_locale);
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		return;
	}
	
	/* Fill in the static fields */

	/* TODO: Calendar, CurrentCulture,
	 * CurrentUICulture, InstalledUICulture, NumberFormat,
	 * OptionalCalendars, Parent, TextInfo
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

	set_field_by_name (this, "iso3lang",mono_string_new_wrapper (uloc_getISO3Language (icu_locale)));

	ret=uloc_getLanguage (icu_locale, str, str_len, &ec);
	if(U_SUCCESS (ec) && ret<str_len) {
		set_field_by_name (this, "iso2lang",
				   mono_string_new_wrapper (str));
	}

	set_field_by_name (this, "datetime_format",
			   create_DateTimeFormat (icu_locale));
	
	g_free (str);
	g_free (ustr);
	g_free (icu_locale);
	ucnv_close (conv);
}

void ves_icall_System_Globalization_CultureInfo_construct_compareinfo (MonoObject *comp, MonoString *locale)
{
	UCollator *coll;
	UErrorCode ec;
	char *icu_locale;
	
	MONO_ARCH_SAVE_REGS;
	
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
		 */
		ucol_setAttribute (coll, UCOL_STRENGTH, UCOL_TERTIARY, &ec);
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
		/* TODO */
	}
}

gint32 ves_icall_System_Globalization_CompareInfo_internal_compare (MonoObject *this, MonoString *str1, MonoString *str2, gint32 options)
{
	UConverter *conv;
	UCollator *coll;
	UChar *ustr1, *ustr2;
	UCollationResult result;
	UErrorCode ec;
	
	MONO_ARCH_SAVE_REGS;
	
	coll=get_field_by_name (this, "ICU_collator");
	if(coll==NULL) {
		return(0);
	}
	
	ec=U_ZERO_ERROR;
	conv=ucnv_open ("UTF-16LE", &ec);
	if(U_FAILURE (ec)) {
		return(0);
	}
	
	ustr1=monostring_to_UChars (str1, conv);
	ustr2=monostring_to_UChars (str2, conv);
	
	ucnv_close (conv);
	
	set_collator_options (coll, options);
			
	result=ucol_strcoll (coll, ustr1, -1, ustr2, -1);
	
	g_free (ustr1);
	g_free (ustr2);
	
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
	conv=ucnv_open ("UTF-16LE", &ec);
	if(U_FAILURE (ec)) {
		mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
		return;
	}
	ustr=monostring_to_UChars (source, conv);
	ucnv_close (conv);
	
	set_collator_options (coll, options);

	keylen=ucol_getSortKey (coll, ustr, -1, NULL, 0);
	keybuf=g_malloc (sizeof(char)* keylen);
	ucol_getSortKey (coll, ustr, -1, keybuf, keylen);
	
	arr=mono_array_new (mono_domain_get (), mono_defaults.byte_class,
			    keylen);
	for(i=0; i<keylen; i++) {
		mono_array_set (arr, guint8, i, keybuf[i]);
	}
	
	set_field_by_name (key, "key", arr);

	g_free (ustr);
	g_free (keybuf);
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
	/* TODO */
	mono_raise_exception((MonoException *)mono_exception_from_name(mono_defaults.corlib, "System", "SystemException"));
}

void ves_icall_System_Globalization_CompareInfo_free_internal_collator (MonoObject *this)
{
	/* Nothing to do here */
}

void ves_icall_System_Globalization_CompareInfo_assign_sortkey (MonoObject *this, MonoObject *key, MonoString *source, gint32 options)
{
	MonoArray *arr;
	int32_t keylen, i;

	MONO_ARCH_SAVE_REGS;
	
	keylen=mono_string_length (source);
	
	arr=mono_array_new (mono_domain_get (), mono_defaults.byte_class,
			    keylen);
	for(i=0; i<keylen; i++) {
		mono_array_set (arr, guint8, i, mono_string_chars (source)[i]);
	}
	
	set_field_by_name (key, "key", arr);
}

#endif /* HAVE_ICU */
