/*
 * unicode.h:  unicode conversion
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_UNICODE_H_
#define _WAPI_UNICODE_H_

/* This is an internal, private header file */

#include <glib.h>

extern gchar *_wapi_unicode_to_utf8 (const gunichar2 *uni);

#endif /* _WAPI_UNICODE_H_ */
