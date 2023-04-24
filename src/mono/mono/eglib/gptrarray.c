/*
 * Pointer Array
 *
 * Author:
 *   Aaron Bockover (abockover@novell.com)
 *   Gonzalo Paniagua Javier (gonzalo@novell.com)
 *   Jeffrey Stedfast (fejj@novell.com)
 *
 * (C) 2006,2011 Novell, Inc.
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

typedef struct _GPtrArrayPriv {
	gpointer *pdata;
	guint len;
	guint size;
} GPtrArrayPriv;

static void
g_ptr_array_grow(GPtrArrayPriv *array, guint length)
{
	g_assert (array);
	guint new_length = array->len + length;

	if(new_length <= array->size) {
		return;
	}

	array->size = 1;

	while(array->size < new_length) {
		array->size <<= 1;
	}

	array->size = MAX(array->size, 16);
	array->pdata = g_realloc(array->pdata, array->size * sizeof(gpointer));
}

GPtrArray *
g_ptr_array_new(void)
{
	return g_ptr_array_sized_new(0);
}

GPtrArray *
g_ptr_array_sized_new(guint reserved_size)
{
	GPtrArrayPriv *array = g_new0(GPtrArrayPriv, 1);

	array->pdata = NULL;
	array->len = 0;
	array->size = 0;

	if(reserved_size > 0) {
		g_ptr_array_grow(array, reserved_size);
	}

	return (GPtrArray *)array;
}

gpointer *
g_ptr_array_free(GPtrArray *array, gboolean free_seg)
{
	gpointer *data = NULL;

	g_assert (array);

	if(free_seg) {
		g_free(array->pdata);
	} else {
		data = array->pdata;
	}

	g_free(array);

	return data;
}

void
g_ptr_array_set_size(GPtrArray *array, gint length)
{
	g_assert (array);

	if((size_t)length > array->len) {
		g_ptr_array_grow((GPtrArrayPriv *)array, length);
		memset(array->pdata + array->len, 0, (length - array->len)
			* sizeof(gpointer));
	}

	array->len = length;
}

void
g_ptr_array_add(GPtrArray *array, gpointer data)
{
	g_assert (array);
	g_ptr_array_grow((GPtrArrayPriv *)array, 1);
	array->pdata[array->len++] = data;
}

gpointer
g_ptr_array_remove_index(GPtrArray *array, guint index)
{
	gpointer removed_node;

	g_assert (array);
	g_return_val_if_fail(index < array->len, NULL);

	removed_node = array->pdata[index];

	if(index != array->len - 1) {
		g_memmove(array->pdata + index, array->pdata + index + 1,
			(array->len - index - 1) * sizeof(gpointer));
	}

	array->len--;
	array->pdata[array->len] = NULL;

	return removed_node;
}

gpointer
g_ptr_array_remove_index_fast(GPtrArray *array, guint index)
{
	gpointer removed_node;

	g_assert (array);
	g_return_val_if_fail(index < array->len, NULL);

	removed_node = array->pdata[index];

	if(index != array->len - 1) {
		g_memmove(array->pdata + index, array->pdata + array->len - 1,
			sizeof(gpointer));
	}

	array->len--;
	array->pdata[array->len] = NULL;

	return removed_node;
}

gboolean
g_ptr_array_remove(GPtrArray *array, gpointer data)
{
	guint i;

	g_assert (array);

	for(i = 0; i < array->len; i++) {
		if(array->pdata[i] == data) {
			g_ptr_array_remove_index(array, i);
			return TRUE;
		}
	}

	return FALSE;
}

gboolean
g_ptr_array_remove_fast(GPtrArray *array, gpointer data)
{
	guint i;

	g_assert (array);

	for(i = 0; i < array->len; i++) {
		if(array->pdata[i] == data) {
			array->len--;
			if (array->len > 0)
				array->pdata [i] = array->pdata [array->len];
			else
				array->pdata [i] = NULL;
			return TRUE;
		}
	}

	return FALSE;
}

void
g_ptr_array_foreach(GPtrArray *array, GFunc func, gpointer user_data)
{
	guint i;

	for(i = 0; i < array->len; i++) {
		func(g_ptr_array_index(array, i), user_data);
	}
}

void
g_ptr_array_sort(GPtrArray *array, GCompareFunc compare)
{
	g_assert (array);
	mono_qsort (array->pdata, array->len, sizeof(gpointer), compare);
}

guint
g_ptr_array_capacity (GPtrArray *array)
{
	return ((GPtrArrayPriv *)array)->size;
}

gboolean
g_ptr_array_find (GPtrArray *array, gconstpointer needle, guint *index)
{
	g_assert (array);
	for (guint i = 0; i < array->len; i++) {
		if (array->pdata [i] == needle) {
			if (index)
				*index = i;
			return TRUE;
		}
	}

	return FALSE;
}
