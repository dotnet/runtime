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
#include "config.h"
#include <stdlib.h>
#include <glib.h>

GByteArray *
g_byte_array_new (void)
{
	return (GByteArray *) g_array_new (FALSE, TRUE, 1);
}

guint8*
g_byte_array_free (GByteArray *array,
	      gboolean free_segment)
{
	return (guint8*) g_array_free ((GArray *)array, free_segment);
}

GByteArray *
g_byte_array_append (GByteArray *array,
		     const guint8 *data,
		     guint len)
{
	return (GByteArray *)g_array_append_vals ((GArray *)array, data, len);
}

void
g_byte_array_set_size (GByteArray *array, gint length)
{
	g_array_set_size ((GArray *)array, length);
}

