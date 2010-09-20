/*
 * Arrays
 *
 * Author:
 *   Geoff Norton  (gnorton@novell.com)
 *
 * (C) 2010 Novell, Inc.
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

#include <stdlib.h>
#include <glib.h>

#define INITIAL_CAPACITY 16

static void
ensure_capacity (GByteArray *array,
		 int capacity)
{
	int new_capacity = MAX (array->len, INITIAL_CAPACITY);

	if (capacity < array->len)
		return;

	while (new_capacity < capacity) {
		new_capacity <<= 1;
	}
	capacity = new_capacity;
	array->data = (guint8*) g_realloc (array->data, capacity);

	memset (array->data + array->len, 0, capacity - array->len);
	array->len = capacity;
}

GByteArray *
g_byte_array_new ()
{
	GByteArray *rv = g_new0 (GByteArray, 1);

	ensure_capacity (rv, INITIAL_CAPACITY);

	return rv;
}

guint8*
g_byte_array_free (GByteArray *array,
	      gboolean free_segment)
{
	guint8* rv = NULL;

	g_return_val_if_fail (array != NULL, NULL);

	if (free_segment)
		g_free (array->data);
	else
		rv = array->data;

	g_free (array);

	return rv;
}

GByteArray *
g_array_append (GByteArray *array,
		     guint8 *data,
		     guint len)
{
	g_return_val_if_fail (array != NULL, NULL);

	ensure_capacity (array, array->len + len);
  
	memmove (array->data + array->len, data, len);

	array->len += len;

	return array;
}
