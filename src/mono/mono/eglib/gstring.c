/*
 * String functions
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
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
#include "config.h"
#include <stdio.h>
#include <glib.h>

#define GROW_IF_NECESSARY(s,l) { \
	if(s->len + l >= s->allocated_len) { \
		s->allocated_len = (s->allocated_len + l + 16) * 2; \
		s->str = g_realloc(s->str, s->allocated_len); \
	} \
}

GString *
g_string_new_len (const gchar *init, gssize len)
{
	GString *ret = g_new (GString, 1);

	if (init == NULL)
		ret->len = 0;
	else
		ret->len = len < 0 ? strlen(init) : len;
	ret->allocated_len = MAX(ret->len + 1, 16);
	ret->str = g_malloc(ret->allocated_len);
	if (init)
		memcpy(ret->str, init, ret->len);
	ret->str[ret->len] = 0;

	return ret;
}

GString *
g_string_new (const gchar *init)
{
	return g_string_new_len(init, -1);
}

GString *
g_string_sized_new (gsize default_size)
{
	GString *ret = g_new (GString, 1);

	ret->str = g_malloc (default_size);
	ret->str [0] = 0;
	ret->len = 0;
	ret->allocated_len = default_size;

	return ret;
}

gchar *
g_string_free (GString *string, gboolean free_segment)
{
	gchar *data;
	
	g_return_val_if_fail (string != NULL, NULL);

	data = string->str;
	g_free(string);
	
	if(!free_segment) {
		return data;
	}

	g_free(data);
	return NULL;
}

GString *
g_string_append_len (GString *string, const gchar *val, gssize len)
{
	g_return_val_if_fail(string != NULL, NULL);
	g_return_val_if_fail(val != NULL, string);

	if(len < 0) {
		len = strlen(val);
	}

	GROW_IF_NECESSARY(string, len);
	memcpy(string->str + string->len, val, len);
	string->len += len;
	string->str[string->len] = 0;

	return string;
}

GString *
g_string_append (GString *string, const gchar *val)
{
	g_return_val_if_fail(string != NULL, NULL);
	g_return_val_if_fail(val != NULL, string);

	return g_string_append_len(string, val, -1);
}

GString *
g_string_append_c (GString *string, gchar c)
{
	g_return_val_if_fail(string != NULL, NULL);

	GROW_IF_NECESSARY(string, 1);
	
	string->str[string->len] = c;
	string->str[string->len + 1] = 0;
	string->len++;

	return string;
}

GString *
g_string_append_unichar (GString *string, gunichar c)
{
	gchar utf8[6];
	gint len;
	
	g_return_val_if_fail (string != NULL, NULL);
	
	if ((len = g_unichar_to_utf8 (c, utf8)) <= 0)
		return string;
	
	return g_string_append_len (string, utf8, len);
}

void
g_string_append_printf (GString *string, const gchar *format, ...)
{
	char *ret;
	va_list args;
	
	g_return_if_fail (string != NULL);
	g_return_if_fail (format != NULL);

	va_start (args, format);
	ret = g_strdup_vprintf (format, args);
	va_end (args);
	g_string_append (string, ret);

	g_free (ret);
}

void
g_string_append_vprintf (GString *string, const gchar *format, va_list args)
{
	char *ret;

	g_return_if_fail (string != NULL);
	g_return_if_fail (format != NULL);

	ret = g_strdup_vprintf (format, args);
	g_string_append (string, ret);
	g_free (ret);
}

void
g_string_printf (GString *string, const gchar *format, ...)
{
	va_list args;
	
	g_return_if_fail (string != NULL);
	g_return_if_fail (format != NULL);

	g_free (string->str);
	
	va_start (args, format);
	string->str = g_strdup_vprintf (format, args);
	va_end (args);

	string->len = strlen (string->str);
	string->allocated_len = string->len+1;
}

GString *
g_string_truncate (GString *string, gsize len)
{
	g_return_val_if_fail (string != NULL, string);

	/* Silent return */
	if (len >= string->len)
		return string;
	
	string->len = len;
	string->str[len] = 0;
	return string;
}

GString *
g_string_set_size (GString *string, gsize len)
{
	g_return_val_if_fail (string != NULL, string);

	GROW_IF_NECESSARY(string, len);
	
	string->len = len;
	string->str[len] = 0;
	return string;
}
