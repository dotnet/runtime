/*
 * strenc.h: string encoding conversions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#ifndef _MONO_STRENC_H_
#define _MONO_STRENC_H_ 1

#include <glib.h>

extern gunichar2 *mono_unicode_from_external (const gchar *in, gsize *bytes);
extern gchar *mono_utf8_from_external (const gchar *in);
extern gchar *mono_unicode_to_external (const gunichar2 *uni);

#endif /* _MONO_STRENC_H_ */
