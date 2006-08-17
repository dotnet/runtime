/*
 * String functions
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
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
#define _GNU_SOURCE
#include <stdio.h>
#include <glib.h>

GString *
g_string_new (const gchar *init)
{
	GString *ret = g_new (GString, 1);
	int len, alloc;

	len = strlen (init);
	if (len < 15)
		alloc = 16;
	else
		alloc = len+1;
	ret->str = g_malloc (alloc);
	ret->len = len;
	ret->allocated_len = alloc;
	strcpy (ret->str, init);

	return ret;
}

GString *
g_string_new_len (const gchar *init, gsize len)
{
	GString *ret = g_new (GString, 1);

	ret->str = g_malloc (len+1);
	ret->allocated_len = len + 1;
	ret->len = len;

	memcpy (ret->str, init, len);
	ret->str [len] = 0;
	
	return ret;
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
	char *data;
	g_return_val_if_fail (string != NULL, NULL);

	data = string->str;
	if (free_segment)
		g_free (data);
	g_free (string);

	if (free_segment)
		return NULL;
	else
		return data;
	
}

GString *
g_string_append (GString *string, const gchar *val)
{
	int len, size;
	char *new;
	
	g_return_val_if_fail (string != NULL, NULL);
	g_return_val_if_fail (val != NULL, string);
	
	len = strlen (val);
	if ((string->len + len) < string->allocated_len){
		strcat (string->str, val);
		string->len += len;
		return string;
	}
	size = (len + string->len + 16) * 2;
	new = g_malloc (size);
	memcpy (new, string->str, string->len);
	memcpy (new + string->len, val, len);
	g_free (string->str);
	string->str = new;
	string->allocated_len = size;
	string->len += len;
	new [string->len] = 0;
	
	return string;
}

GString *
g_string_append_c (GString *string, gchar c)
{
	gsize size;
	char *new;
	
	g_return_val_if_fail (string != NULL, NULL);

	if (string->len + 1 < string->allocated_len){
		string->str [string->len] = c;
		string->str [string->len+1] = 0;
		string->len++;
		return string;
	}
	size = (string->allocated_len + 16) * 2;
	new = g_malloc (size);
	memcpy (new, string->str, string->len);
	new [string->len] = c;
	new [string->len+1] = 0;
	
	g_free (string->str);
	string->allocated_len = size;
	string->len++;
	string->str = new;

	return string;
}

GString *
g_string_append_len (GString *string, const gchar *val, gsize len)
{
	int size;
	char *new;
	
	g_return_val_if_fail (string != NULL, NULL);
	g_return_val_if_fail (val != NULL, string);
	
	if ((string->len + len) < string->allocated_len){
		memcpy (string->str+string->len, val, len);
		string->len += len;
		return string;
	}
	size = (len + string->len + 16) * 2;
	new = g_malloc (size);
	memcpy (new, string->str, string->len);
	memcpy (new + string->len, val, len);
	g_free (string->str);
	string->str = new;
	string->allocated_len = size;
	string->len += len;
	new [string->len] = 0;
	
	return string;
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

	free (ret);
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
