/*
 * strenc.c: string encoding conversions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <string.h>

#include "strenc.h"

#undef DEBUG

/* Tries to turn a NULL-terminated string into UTF16.
 *
 * First, see if it's valid UTF8, in which case just turn it directly
 * into UTF16.  Next, run through the colon-separated encodings in
 * MONO_EXTERNAL_ENCODINGS and do an iconv conversion on each,
 * returning the first successful conversion to UTF16.  If no
 * conversion succeeds, return NULL.
 *
 * Callers must free the returned string if not NULL. bytes holds the number
 * of bytes in the returned string, not including the terminator.
 */
gunichar2 *mono_unicode_from_external (const gchar *in, gsize *bytes)
{
	gchar *res=NULL;
	gchar **encodings;
	const gchar *encoding_list;
	int i;
	glong lbytes;
	
	if(in==NULL) {
		return(NULL);
	}
	
	encoding_list=g_getenv ("MONO_EXTERNAL_ENCODINGS");
	if(encoding_list==NULL) {
		encoding_list = "";
	}
	
	encodings=g_strsplit (encoding_list, ":", 0);
	for(i=0;encodings[i]!=NULL; i++) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Trying encoding [%s]",
			   encodings[i]);
#endif
		/* "default_locale" is a special case encoding */
		if(!strcmp (encodings[i], "default_locale")) {
			gchar *utf8=g_locale_to_utf8 (in, -1, NULL, NULL, NULL);
			if(utf8!=NULL) {
				res=(gchar *) g_utf8_to_utf16 (utf8, -1, NULL, &lbytes, NULL);
				*bytes = (gsize) lbytes;
			}
			g_free (utf8);
		} else {
			res=g_convert (in, -1, "UTF16", encodings[i], NULL, bytes, NULL);
		}

		if(res!=NULL) {
			g_strfreev (encodings);
			*bytes *= 2;
			return((gunichar2 *)res);
		}
	}
	
	g_strfreev (encodings);
	
	if(g_utf8_validate (in, -1, NULL)) {
		gunichar2 *unires=g_utf8_to_utf16 (in, -1, NULL, (glong *)bytes, NULL);
		*bytes *= 2;
		return(unires);
	}

	return(NULL);
}

/* Tries to turn a NULL-terminated string into UTF8.
 *
 * First, see if it's valid UTF8, in which case there's nothing more
 * to be done.  Next, run through the colon-separated encodings in
 * MONO_EXTERNAL_ENCODINGS and do an iconv conversion on each,
 * returning the first successful conversion to utf8.  If no
 * conversion succeeds, return NULL.
 *
 * Callers must free the returned string if not NULL.
 *
 * This function is identical to mono_unicode_from_external, apart
 * from returning utf8 not utf16; it's handy in a few places to work
 * in utf8.
 */
gchar *mono_utf8_from_external (const gchar *in)
{
	gchar *res=NULL;
	gchar **encodings;
	const gchar *encoding_list;
	int i;
	
	if(in==NULL) {
		return(NULL);
	}
	
	encoding_list=g_getenv ("MONO_EXTERNAL_ENCODINGS");
	if(encoding_list==NULL) {
		encoding_list = "";
	}
	
	encodings=g_strsplit (encoding_list, ":", 0);
	for(i=0;encodings[i]!=NULL; i++) {
#ifdef DEBUG
		g_message (G_GNUC_PRETTY_FUNCTION ": Trying encoding [%s]",
			   encodings[i]);
#endif
		
		/* "default_locale" is a special case encoding */
		if(!strcmp (encodings[i], "default_locale")) {
			res=g_locale_to_utf8 (in, -1, NULL, NULL, NULL);
			if(res!=NULL && !g_utf8_validate (res, -1, NULL)) {
				g_free (res);
				res=NULL;
			}
		} else {
			res=g_convert (in, -1, "UTF8", encodings[i], NULL,
				       NULL, NULL);
		}

		if(res!=NULL) {
			g_strfreev (encodings);
			return(res);
		}
	}
	
	g_strfreev (encodings);
	
	if(g_utf8_validate (in, -1, NULL)) {
		return(g_strdup (in));
	}

	return(NULL);
}

/* Turns NULL-terminated UTF16 into either UTF8, or the first
 * working item in MONO_EXTERNAL_ENCODINGS if set.  If no conversions
 * work, then UTF8 is returned.
 *
 * Callers must free the returned string.
 */
gchar *mono_unicode_to_external (const gunichar2 *uni)
{
	gchar *utf8;
	const gchar *encoding_list;
	
	/* Turn the unicode into utf8 to start with, because its
	 * easier to work with gchar * than gunichar2 *
	 */
	utf8=g_utf16_to_utf8 (uni, -1, NULL, NULL, NULL);
	g_assert (utf8!=NULL);
	
	encoding_list=g_getenv ("MONO_EXTERNAL_ENCODINGS");
	if(encoding_list==NULL) {
		/* Do UTF8 */
		return(utf8);
	} else {
		gchar *res, **encodings;
		int i;
		
		encodings=g_strsplit (encoding_list, ":", 0);
		for(i=0; encodings[i]!=NULL; i++) {
			if(!strcmp (encodings[i], "default_locale")) {
				res=g_locale_from_utf8 (utf8, -1, NULL, NULL,
							NULL);
			} else {
				res=g_convert (utf8, -1, encodings[i], "UTF8",
					       NULL, NULL, NULL);
			}

			if(res!=NULL) {
				g_free (utf8);
				g_strfreev (encodings);
				
				return(res);
			}
		}
	
		g_strfreev (encodings);
	}
	
	/* Nothing else worked, so just return the utf8 */
	return(utf8);
}

