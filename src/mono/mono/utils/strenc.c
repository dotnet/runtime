/**
 * \file
 * string encoding conversions
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
#include "strenc-internals.h"
#include <mono/utils/mono-error.h>
#include "mono-error-internals.h"

static const char trailingBytesForUTF8[256] = {
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
	1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
	2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2, 3,3,3,3,3,3,3,3,4,4,4,4,5,5,0,0
};

/**
 * mono_unicode_from_external:
 * \param in pointers to the buffer.
 * \param bytes number of bytes in the string.
 * Tries to turn a NULL-terminated string into UTF-16.
 *
 * First, see if it's valid UTF-8, in which case just turn it directly
 * into UTF-16. If the conversion doesn't succeed, return NULL.
 *
 * Callers must free the returned string if not NULL. \p bytes holds the number
 * of bytes in the returned string, not including the terminator.
 */
gunichar2 *mono_unicode_from_external (const gchar *in, gsize *bytes)
{
	if(in==NULL) {
		return(NULL);
	}

	if(g_utf8_validate (in, -1, NULL)) {
		glong items_written;
		gunichar2 *unires=g_utf8_to_utf16 (in, -1, NULL, &items_written, NULL);
		items_written *= 2;
		*bytes = items_written;
		return(unires);
	}

	return(NULL);
}

/**
 * mono_utf8_from_external:
 * \param in pointer to the string buffer.
 * Tries to turn a NULL-terminated string into UTF8.
 *
 * First, see if it's valid UTF-8, in which case there's nothing more
 * to be done. If the conversion doesn't succeed, return NULL.
 *
 * Callers must free the returned string if not NULL.
 *
 * This function is identical to \c mono_unicode_from_external, apart
 * from returning UTF-8 not UTF-16; it's handy in a few places to work
 * in UTF-8.
 */
gchar *mono_utf8_from_external (const gchar *in)
{
	if(in==NULL) {
		return(NULL);
	}

	if(g_utf8_validate (in, -1, NULL)) {
		return(g_strdup (in));
	}

	return(NULL);
}

/**
 * mono_unicode_to_external:
 * \param uni a UTF-16 string to convert to an external representation.
 * Turns NULL-terminated UTF-16 into UTF-8. If the conversion doesn't
 * work, then NULL is returned.
 * Callers must free the returned string.
 */
gchar *mono_unicode_to_external (const gunichar2 *uni)
{
	return mono_unicode_to_external_checked (uni, NULL);
}

gchar *mono_unicode_to_external_checked (const gunichar2 *uni, MonoError *err)
{
	gchar *utf8;
	GError *gerr = NULL;

	utf8=g_utf16_to_utf8 (uni, -1, NULL, NULL, &gerr);
	if (utf8 == NULL) {
		mono_error_set_argument (err, "uni", gerr->message);
		g_error_free (gerr);
		return NULL;
	}

	return(utf8);
}

/**
 * mono_utf8_validate_and_len
 * \param source Pointer to putative UTF-8 encoded string.
 * Checks \p source for being valid UTF-8. \p utf is assumed to be
 * null-terminated.
 * \returns TRUE if \p source is valid.
 * \p oEnd will equal the null terminator at the end of the string if valid.
 * if not valid, it will equal the first charater of the invalid sequence.
 * \p oLength will equal the length to \p oEnd
 **/
gboolean
mono_utf8_validate_and_len (const gchar *source, glong* oLength, const gchar** oEnd)
{
	gboolean retVal = TRUE;
	gboolean lastRet = TRUE;
	guchar* ptr = (guchar*) source;
	guchar* srcPtr;
	guint length;
	guchar a;
	*oLength = 0;
	while (*ptr != 0) {
		length = trailingBytesForUTF8 [*ptr] + 1;
		srcPtr = (guchar*) ptr + length;
		switch (length) {
		default: retVal = FALSE;
		/* Everything else falls through when "TRUE"... */
		case 4: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;
				if ((a == (guchar) 0xBF || a == (guchar) 0xBE) && *(srcPtr-1) == (guchar) 0xBF) {
				if (*(srcPtr-2) == (guchar) 0x8F || *(srcPtr-2) == (guchar) 0x9F ||
					*(srcPtr-2) == (guchar) 0xAF || *(srcPtr-2) == (guchar) 0xBF)
					retVal = FALSE;
				}
		case 3: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;
		case 2: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;

		switch (*ptr) {
		/* no fall-through in this inner switch */
		case 0xE0: if (a < (guchar) 0xA0) retVal = FALSE; break;
		case 0xED: if (a > (guchar) 0x9F) retVal = FALSE; break;
		case 0xEF: {
			if (a == (guchar)0xB7 && (*(srcPtr+1) > (guchar) 0x8F && *(srcPtr+1) < 0xB0)) retVal = FALSE;
			else if (a == (guchar)0xBF && (*(srcPtr+1) == (guchar) 0xBE || *(srcPtr+1) == 0xBF)) retVal = FALSE;
			break;
		}
		case 0xF0: if (a < (guchar) 0x90) retVal = FALSE; break;
		case 0xF4: if (a > (guchar) 0x8F) retVal = FALSE; break;
		default:   if (a < (guchar) 0x80) retVal = FALSE;
		}

		case 1: if (*ptr >= (guchar ) 0x80 && *ptr < (guchar) 0xC2) retVal = FALSE;
		}
		if (*ptr > (guchar) 0xF4)
			retVal = FALSE;
		//If the string is invalid, set the end to the invalid byte.
		if (!retVal && lastRet) {
			if (oEnd != NULL)
				*oEnd = (gchar*) ptr;
			lastRet = FALSE;
		}
		ptr += length;
		(*oLength)++;
	}
	if (retVal && oEnd != NULL)
		*oEnd = (gchar*) ptr;
	return retVal;
}


/**
 * mono_utf8_validate_and_len_with_bounds
 * \param source: Pointer to putative UTF-8 encoded string.
 * \param max_bytes: Max number of bytes that can be decoded.
 *
 * Checks \p source for being valid UTF-8. \p utf is assumed to be
 * null-terminated.
 *
 * This function returns FALSE if it needs to decode characters beyond \p max_bytes.
 *
 * \returns TRUE if \p source is valid.
 * \p oEnd will equal the null terminator at the end of the string if valid.
 * if not valid, it will equal the first charater of the invalid sequence.
 * \p oLength will equal the length to \p oEnd
 **/
gboolean
mono_utf8_validate_and_len_with_bounds (const gchar *source, glong max_bytes, glong* oLength, const gchar** oEnd)
{
	gboolean retVal = TRUE;
	gboolean lastRet = TRUE;
	guchar* ptr = (guchar*) source;
	guchar *end = ptr + max_bytes;
	guchar* srcPtr;
	guint length;
	guchar a;
	*oLength = 0;

	if (max_bytes < 1) {
		if (oEnd)
			*oEnd = (gchar*) ptr;
		return FALSE;
	}

	while (*ptr != 0) {
		length = trailingBytesForUTF8 [*ptr] + 1;
		srcPtr = (guchar*) ptr + length;

		/* since *ptr is not zero we must ensure that we can decode the current char + the byte after
		   srcPtr points to the first byte after the current char.*/
		if (srcPtr >= end) {
			retVal = FALSE;
			break;
		}
		switch (length) {
		default: retVal = FALSE;
		/* Everything else falls through when "TRUE"... */
		case 4: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;
				if ((a == (guchar) 0xBF || a == (guchar) 0xBE) && *(srcPtr-1) == (guchar) 0xBF) {
				if (*(srcPtr-2) == (guchar) 0x8F || *(srcPtr-2) == (guchar) 0x9F ||
					*(srcPtr-2) == (guchar) 0xAF || *(srcPtr-2) == (guchar) 0xBF)
					retVal = FALSE;
				}
		case 3: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;
		case 2: if ((a = (*--srcPtr)) < (guchar) 0x80 || a > (guchar) 0xBF) retVal = FALSE;

		switch (*ptr) {
		/* no fall-through in this inner switch */
		case 0xE0: if (a < (guchar) 0xA0) retVal = FALSE; break;
		case 0xED: if (a > (guchar) 0x9F) retVal = FALSE; break;
		case 0xEF: {
			if (a == (guchar)0xB7 && (*(srcPtr+1) > (guchar) 0x8F && *(srcPtr+1) < 0xB0)) retVal = FALSE;
			else if (a == (guchar)0xBF && (*(srcPtr+1) == (guchar) 0xBE || *(srcPtr+1) == 0xBF)) retVal = FALSE;
			break;
		}
		case 0xF0: if (a < (guchar) 0x90) retVal = FALSE; break;
		case 0xF4: if (a > (guchar) 0x8F) retVal = FALSE; break;
		default:   if (a < (guchar) 0x80) retVal = FALSE;
		}

		case 1: if (*ptr >= (guchar ) 0x80 && *ptr < (guchar) 0xC2) retVal = FALSE;
		}
		if (*ptr > (guchar) 0xF4)
			retVal = FALSE;
		//If the string is invalid, set the end to the invalid byte.
		if (!retVal && lastRet) {
			if (oEnd != NULL)
				*oEnd = (gchar*) ptr;
			lastRet = FALSE;
		}
		ptr += length;
		(*oLength)++;
	}
	if (retVal && oEnd != NULL)
		*oEnd = (gchar*) ptr;
	return retVal;
}

