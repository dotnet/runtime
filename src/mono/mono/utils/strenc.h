/**
 * \file
 * string encoding conversions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#ifndef _MONO_STRENC_H_
#define _MONO_STRENC_H_ 1

#include <glib.h>
#include <mono/utils/mono-publib.h>

MONO_API gunichar2 *mono_unicode_from_external (const gchar *in, gsize *bytes);
MONO_API gchar *mono_utf8_from_external (const gchar *in);
MONO_API gchar *mono_unicode_to_external (const gunichar2 *uni);
MONO_API gboolean mono_utf8_validate_and_len (const gchar *source, glong* oLength, const gchar** oEnd);
MONO_API gboolean mono_utf8_validate_and_len_with_bounds (const gchar *source, glong max_bytes, glong* oLength, const gchar** oEnd);

#endif /* _MONO_STRENC_H_ */
