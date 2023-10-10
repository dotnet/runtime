/*
 * Arrays
 *
 * Author:
 *   Chris Toshok (toshok@novell.com)
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
#include <stdlib.h>
#include <glib.h>

#define INITIAL_CAPACITY 16

#define element_offset(p,i) ((p)->array.data + (i) * (p)->element_size)
#define element_length(p,i) ((i) * (p)->element_size)

typedef struct {
	GArray array;
	gboolean clear_;
	guint element_size;
	gboolean zero_terminated;
	guint capacity;
} GArrayPriv;

static void
ensure_capacity (GArrayPriv *priv, guint capacity)
{
	guint new_capacity;

	if (capacity <= priv->capacity)
		return;

	new_capacity = (capacity + (capacity >> 1) + 63) & ~63;

	priv->array.data = g_realloc (priv->array.data, element_length (priv, new_capacity));

	if (priv->clear_) {
		memset (element_offset (priv, priv->capacity),
			0,
			element_length (priv, new_capacity - priv->capacity));
	}

	priv->capacity = new_capacity;
}

GArray *
g_array_new (gboolean zero_terminated,
	     gboolean clear_,
	     guint element_size)
{
	GArrayPriv *rv = g_new0 (GArrayPriv, 1);
	rv->zero_terminated = zero_terminated;
	rv->clear_ = clear_;
	rv->element_size = element_size;

	ensure_capacity (rv, INITIAL_CAPACITY);

	return (GArray*)rv;
}

GArray *
g_array_sized_new (gboolean zero_terminated,
	     gboolean clear_,
	     guint element_size,
		 guint reserved_size)
{
	GArrayPriv *rv = g_new0 (GArrayPriv, 1);
	rv->zero_terminated = zero_terminated;
	rv->clear_ = clear_;
	rv->element_size = element_size;

	ensure_capacity (rv, reserved_size);

	return (GArray*)rv;
}

gchar*
g_array_free (GArray *array,
	      gboolean free_segment)
{
	gchar* rv = NULL;

	g_return_val_if_fail (array != NULL, NULL);

	if (free_segment)
		g_free (array->data);
	else
		rv = array->data;

	g_free (array);

	return rv;
}

GArray *
g_array_append_vals (GArray *array,
		     gconstpointer data,
		     guint len)
{
	GArrayPriv *priv = (GArrayPriv*)array;

	g_return_val_if_fail (array != NULL, NULL);

	ensure_capacity (priv, priv->array.len + len + (priv->zero_terminated ? 1 : 0));

	memmove (element_offset (priv, priv->array.len),
		 data,
		 element_length (priv, len));

	priv->array.len += len;

	if (priv->zero_terminated) {
		memset (element_offset (priv, priv->array.len),
			0,
			priv->element_size);
	}

	return array;
}

GArray*
g_array_insert_vals (GArray *array,
		     guint index_,
		     gconstpointer data,
		     guint len)
{
	GArrayPriv *priv = (GArrayPriv*)array;
	guint extra = (priv->zero_terminated ? 1 : 0);

	g_return_val_if_fail (array != NULL, NULL);

	ensure_capacity (priv, array->len + len + extra);

	/* first move the existing elements out of the way */
	memmove (element_offset (priv, index_ + len),
		 element_offset (priv, index_),
		 element_length (priv, array->len - index_));

	/* then copy the new elements into the array */
	memmove (element_offset (priv, index_),
		 data,
		 element_length (priv, len));

	array->len += len;

	if (priv->zero_terminated) {
		memset (element_offset (priv, priv->array.len),
			0,
			priv->element_size);
	}

	return array;
}

GArray*
g_array_remove_index (GArray *array,
		      guint index_)
{
	GArrayPriv *priv = (GArrayPriv*)array;

	g_return_val_if_fail (array != NULL, NULL);

	memmove (element_offset (priv, index_),
		 element_offset (priv, index_ + 1),
		 element_length (priv, array->len - index_));

	array->len --;

	if (priv->zero_terminated) {
		memset (element_offset (priv, priv->array.len),
			0,
			priv->element_size);
	}

	return array;
}

GArray*
g_array_remove_index_fast (GArray *array,
		      guint index_)
{
	GArrayPriv *priv = (GArrayPriv*)array;

	g_return_val_if_fail (array != NULL, NULL);

	memmove (element_offset (priv, index_),
		 element_offset (priv, array->len - 1),
		 element_length (priv, 1));

	array->len --;

	if (priv->zero_terminated) {
		memset (element_offset (priv, priv->array.len),
			0,
			priv->element_size);
	}

	return array;
}

void
g_array_set_size (GArray *array, gint length)
{
	GArrayPriv *priv = (GArrayPriv*)array;

	g_return_if_fail (array != NULL);
	g_return_if_fail (length >= 0);

	if (GINT_TO_UINT(length) == priv->capacity)
		return; // nothing to be done

	if (GINT_TO_UINT(length) > priv->capacity) {
		// grow the array
		ensure_capacity (priv, length);
	}

	array->len = length;
}
