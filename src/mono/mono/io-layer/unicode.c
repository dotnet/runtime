/*
 * unicode.c:  unicode conversion
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <errno.h>

#include "mono/io-layer/wapi.h"
#include "unicode.h"

gchar *_wapi_unicode_to_utf8(const gunichar2 *uni)
{
	GError *error = NULL;
	gchar *res;
	
	res = g_utf16_to_utf8 (uni, -1, NULL, NULL, &error);

	g_assert (!error);

	return res;
}
