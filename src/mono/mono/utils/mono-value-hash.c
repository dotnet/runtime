/**
 * \file
 * A hash table which only stores values in the hash nodes.
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2006,2008 Novell, Inc.
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
#include <stdio.h>
#include <math.h>
#include <glib.h>

#include "mono-value-hash.h"

#ifndef G_MAXINT32
#define G_MAXINT32 2147483647
#endif

/*
 * This code is based on eglib/ghashtable.c with work done by Hans Petter Jansson
 * (hpj@novell.com) to make it use internal probing instead of chaining.
 */

#define HASH_TABLE_MIN_SHIFT 3  /* 1 << 3 == 8 buckets */

typedef struct _Slot Slot;

#define GET_VALUE(slot) ((gpointer)((((gsize)((slot)->value)) >> 2) << 2))

#define SET_VALUE(slot,value) ((slot)->value = (value))

#define IS_EMPTY(slot) ((gsize)((slot)->value) == 0)
#define IS_TOMBSTONE(slot) ((gsize)((slot)->value) & 1)

#define MAKE_TOMBSTONE(slot) do { (slot)->value = (gpointer)((gsize)((slot)->value) | 1); } while (1)

#define HASH(table, key) ((table)->hash_func ((key)))

struct _Slot {
	/* A NULL value means the slot is empty */
	/* The tombstone status is stored in the lowest order bit of the value. */
	gpointer value;
};

static gpointer KEYMARKER_REMOVED = &KEYMARKER_REMOVED;

struct _MonoValueHashTable {
	GHashFunc      hash_func;
	GEqualFunc     key_equal_func;
	MonoValueHashKeyExtractFunc key_extract_func;

	Slot *table;
	int   table_size;
	int   table_mask;
	int   in_use;
	int   n_occupied;
	GDestroyNotify value_destroy_func, key_destroy_func;
};

static void
mono_value_hash_table_set_shift (MonoValueHashTable *hash_table, gint shift)
{
	gint i;
	guint mask = 0;

	hash_table->table_size = 1 << shift;

	for (i = 0; i < shift; i++) {
		mask <<= 1;
		mask |= 1;
	}

	hash_table->table_mask = mask;
}

static gint
mono_value_hash_table_find_closest_shift (gint n)
{
	gint i;

	for (i = 0; n; i++)
		n >>= 1;

	return i;
}

static void
mono_value_hash_table_set_shift_from_size (MonoValueHashTable *hash_table, gint size)
{
	gint shift;

	shift = mono_value_hash_table_find_closest_shift (size);
	shift = MAX (shift, HASH_TABLE_MIN_SHIFT);

	mono_value_hash_table_set_shift (hash_table, shift);
}

MonoValueHashTable *
mono_value_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func, MonoValueHashKeyExtractFunc key_extract)
{
	MonoValueHashTable *hash;

	if (hash_func == NULL)
		hash_func = g_direct_hash;
	if (key_equal_func == NULL)
		key_equal_func = g_direct_equal;
	hash = g_new0 (MonoValueHashTable, 1);

	hash->hash_func = hash_func;
	hash->key_equal_func = key_equal_func;
	hash->key_extract_func = key_extract;

	mono_value_hash_table_set_shift (hash, HASH_TABLE_MIN_SHIFT);
	hash->table = g_new0 (Slot, hash->table_size);
	
	return hash;
}

#if 0

MonoValueHashTable *
mono_value_hash_table_new_full (GHashFunc hash_func, GEqualFunc key_equal_func,
								GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func)
{
	MonoValueHashTable *hash = mono_value_hash_table_new (hash_func, key_equal_func);
	if (hash == NULL)
		return NULL;
	
	hash->key_destroy_func = key_destroy_func;
	hash->value_destroy_func = value_destroy_func;
	
	return hash;
}

#endif

static void
do_rehash (MonoValueHashTable *hash)
{
	int i;
	int old_size;
	Slot *old_table;

	old_size = hash->table_size;
	old_table = hash->table;

	mono_value_hash_table_set_shift_from_size (hash, hash->in_use * 2);

	/* printf ("New size: %d\n", hash->table_size); */
	hash->table = g_new0 (Slot, hash->table_size);
	
	for (i = 0; i < old_size; i++){
		Slot *s = &old_table [i];
		Slot *new_s;
		guint hash_val;
		guint step = 0;
		gpointer s_value, s_key;

		if (IS_EMPTY (s) || IS_TOMBSTONE (s))
			continue;

		s_value = GET_VALUE (s);
		s_key = hash->key_extract_func (s_value);
		hash_val = HASH (hash, s_key) & hash->table_mask;
		new_s = &hash->table [hash_val];

		while (!IS_EMPTY (new_s)) {
			step++;
			hash_val += step;
			hash_val &= hash->table_mask;
			new_s = &hash->table [hash_val];
		}

		*new_s = *s;
	}
	g_free (old_table);
	hash->n_occupied = hash->in_use;
}

static void
rehash (MonoValueHashTable *hash)
{
	int n_occupied = hash->n_occupied;
	int table_size = hash->table_size;

	if ((table_size > hash->in_use * 4 && table_size > 1 << HASH_TABLE_MIN_SHIFT) ||
	    (table_size <= n_occupied + (n_occupied / 16)))
		do_rehash (hash);
}

static void
mono_value_hash_table_insert_replace (MonoValueHashTable *hash, gpointer key, gpointer value, gboolean replace)
{
	guint hashcode;
	Slot *s;
	guint s_index;
	GEqualFunc equal;
	guint first_tombstone = 0;
	gboolean have_tombstone = FALSE;
	guint step = 0;

	g_assert (value);
	g_assert (hash->key_extract_func (value) == key);
	
	g_return_if_fail (hash != NULL);

	hashcode = HASH (hash, key);

	s_index = hashcode & hash->table_mask;
	s = &hash->table [s_index];

	equal = hash->key_equal_func;

	while (!IS_EMPTY (s)) {
		gpointer s_value = GET_VALUE (s);
		gpointer s_key = hash->key_extract_func (s_value);
		guint s_key_hash = HASH (hash, s_key);
		if (s_key_hash == hashcode && (*equal) (s_key, key)) {
			if (replace){
				if (hash->key_destroy_func != NULL)
					(*hash->key_destroy_func)(s_key);
			}
			if (hash->value_destroy_func != NULL)
				(*hash->value_destroy_func) (GET_VALUE (s));
			SET_VALUE (s, value);
			return;
		} else if (IS_TOMBSTONE (s) && !have_tombstone) {
			first_tombstone = s_index;
			have_tombstone = TRUE;
		}

		step++;
		s_index += step;
		s_index &= hash->table_mask;
		s = &hash->table [s_index];
	}

	if (have_tombstone) {
		s = &hash->table [first_tombstone];
	} else {
		hash->n_occupied++;
	}

	SET_VALUE (s, value);
	hash->in_use++;

	rehash (hash);
}

void
mono_value_hash_table_insert (MonoValueHashTable *hash, gpointer key, gpointer value)
{
	mono_value_hash_table_insert_replace (hash, key, value, TRUE);
}

static Slot *
lookup_internal (MonoValueHashTable *hash, gconstpointer key)
{
	GEqualFunc equal;
	Slot *s;
	guint hashcode;
	guint s_index;
	guint step = 0;
	
	hashcode = HASH (hash, key);

	s_index = hashcode & hash->table_mask;
	s = &hash->table [s_index];

	equal = hash->key_equal_func;

	while (!IS_EMPTY (s)) {
		gpointer s_value = GET_VALUE (s);
		gpointer s_key = hash->key_extract_func (s_value);
		guint s_key_hash = HASH (hash, s_key);
		if (s_key_hash == hashcode && (*equal) (hash->key_extract_func (s_value), key))
			return s;

		step++;
		s_index += step;
		s_index &= hash->table_mask;
		s = &hash->table [s_index];
	}

	return NULL;
}

gpointer
mono_value_hash_table_lookup (MonoValueHashTable *hash, gconstpointer key)
{
	Slot *slot = lookup_internal (hash, key);

	if (slot)
		return GET_VALUE (slot);
	else
		return NULL;
}

void
mono_value_hash_table_destroy (MonoValueHashTable *hash)
{
	int i;
	
	g_return_if_fail (hash != NULL);

	for (i = 0; i < hash->table_size; i++){
		Slot *s = &hash->table [i];

		if (!IS_EMPTY (s) && !IS_TOMBSTONE (s)) {
			if (hash->key_destroy_func != NULL)
				(*hash->key_destroy_func)(hash->key_extract_func (GET_VALUE (s)));
			if (hash->value_destroy_func != NULL)
				(*hash->value_destroy_func)(GET_VALUE (s));
		}
	}
	g_free (hash->table);
	
	g_free (hash);
}
