/*
 * EGLib Unit Group/Test Runners
 *
 * Author:
 *   Aaron Bockover (abockover@novell.com)
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
 
#ifndef _TEST_H
#define _TEST_H

#include <config.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>

#ifdef _MSC_VER
/* disable the following warnings 
 * C4100: The formal parameter is not referenced in the body of the function. The unreferenced parameter is ignored. 
 * C4127: conditional expression is constant (test macros produce a lot of these)
*/
#pragma warning(disable:4100 4127)
#endif

typedef gchar * RESULT;

typedef struct _Test Test;
typedef struct _Group Group;

typedef gchar * (* RunTestHandler)(void);
typedef Test * (* LoadGroupHandler)(void);

struct _Test {
	const gchar *name;
	RunTestHandler handler;
};

struct _Group {
	const gchar *name;
	LoadGroupHandler handler;
};

gboolean run_group(const Group *group, gint iterations, gboolean quiet,
	gboolean time, const char *tests);
#undef FAILED
RESULT FAILED(const gchar *format, ...);
gdouble get_timestamp (void);
gchar ** eg_strsplit (const gchar *string, const gchar *delimiter, gint max_tokens);
void eg_strfreev (gchar **str_array);

#define OK NULL

#define DEFINE_TEST_GROUP_INIT(name, table) \
	const Test * (name) (void); \
	const Test * (name) (void) { return table; }

#define DEFINE_TEST_GROUP_INIT_H(name) \
	Test * (name) (void);

#endif /* _TEST_H */
