/*
 * ghashtable.c: Hashtable implementation
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
#include <stdio.h>
#include <math.h>
#include <glib.h>

typedef struct _Slot Slot;

struct _Slot {
	gpointer key;
	gpointer value;
	Slot    *next;
};

static gpointer KEYMARKER_REMOVED = &KEYMARKER_REMOVED;

struct _GHashTable {
	GHashFunc      hash_func;
	GEqualFunc     key_equal_func;

	Slot **table;
	int   table_size;
	int   in_use;
	int   threshold;
	GDestroyNotify value_destroy_func, key_destroy_func;
};

static int prime_tbl[] = {
	11, 19, 37, 73, 109, 163, 251, 367, 557, 823, 1237,
	1861, 2777, 4177, 6247, 9371, 14057, 21089, 31627,
	47431, 71143, 106721, 160073, 240101, 360163,
	540217, 810343, 1215497, 1823231, 2734867, 4102283,
	6153409, 9230113, 13845163
};

static gboolean
test_prime (int x)
{
	if ((x & 1) != 0) {
		int n;
		for (n = 3; n< (int)sqrt (x); n += 2) {
			if ((x % n) == 0)
				return FALSE;
		}
		return TRUE;
	}
	// There is only one even prime - 2.
	return (x == 2);
}

static int
calc_prime (int x)
{
	int i;
	
	for (i = (x & (~1))-1; i< G_MAXINT32; i += 2) {
		if (test_prime (i))
			return i;
	}
	return x;
}

static int
to_prime (int x)
{
	int i;
	
	for (i = 0; i < G_N_ELEMENTS (prime_tbl); i++) {
		if (x <= prime_tbl [i])
			return prime_tbl [i];
	}
	return calc_prime (x);
}

static void
adjust_threshold (GHashTable *hash)
{
	int size = hash->table_size;

	hash->threshold = (int) hash->table_size * 0.75;
	if (hash->threshold >= hash->table_size)
		hash->threshold = hash->table_size-1;
	if (hash->threshold == 0)
		hash->threshold = 1;
}
	
static void
set_table (GHashTable *hash, Slot **table)
{
	hash->table = table;
	adjust_threshold (hash);
}

GHashTable *
g_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func)
{
	GHashTable *hash;

	g_return_val_if_fail (hash_func != NULL, NULL);
	g_return_val_if_fail (key_equal_func != NULL, NULL);
			  
	hash = g_new0 (GHashTable, 1);

	hash->hash_func = hash_func;
	hash->key_equal_func = key_equal_func;

	hash->table_size = to_prime (1);
	hash->table = g_new0 (Slot *, hash->table_size);
	adjust_threshold (hash);
	
	return hash;
}

GHashTable *
g_hash_table_new_full (GHashFunc hash_func, GEqualFunc key_equal_func,
		       GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func)
{
	GHashTable *hash = g_hash_table_new (hash_func, key_equal_func);
	if (hash == NULL)
		return NULL;
	
	hash->key_destroy_func = key_destroy_func;
	hash->value_destroy_func = value_destroy_func;
	
	return hash;
}

void
rehash (GHashTable *hash)
{
	/* FIXME */
}

void
g_hash_table_insert_replace (GHashTable *hash, gpointer key, gpointer value, gboolean replace)
{
	guint hashcode;
	Slot *s;
	GEqualFunc equal;
	
	g_return_if_fail (hash != NULL);

	equal = hash->key_equal_func;
	if (hash->in_use >= hash->threshold)
		rehash (hash);

	hashcode = ((*hash->hash_func) (key)) % hash->table_size;
	for (s = hash->table [hashcode]; s != NULL; s = s->next){
		if ((*equal) (s->key, key)){
			if (replace){
				if (hash->key_destroy_func != NULL)
					(*hash->key_destroy_func)(s->key);
				s->key = key;
			}
			if (hash->value_destroy_func != NULL)
				(*hash->value_destroy_func) (s->value);
			s->value = value;
			return;
		}
	}
	s = g_new (Slot, 1);
	s->key = key;
	s->value = value;
	s->next = hash->table [hashcode];
	hash->table [hashcode] = s;
	hash->in_use++;
}

guint
g_hash_table_size (GHashTable *hash)
{
	g_return_if_fail (hash != NULL);
	
	return hash->in_use;
}

gpointer
g_hash_table_lookup (GHashTable *hash, gconstpointer key)
{
	gpointer orig_key, value;
	
	if (g_hash_table_lookup_extended (hash, key, &orig_key, &value))
		return value;
	else
		return NULL;
}

gboolean
g_hash_table_lookup_extended (GHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value)
{
	GEqualFunc equal;
	Slot *s;
	guint hashcode;
	
	g_return_if_fail (hash != NULL);
	equal = hash->key_equal_func;

	hashcode = ((*hash->hash_func) (key)) % hash->table_size;	
	for (s = hash->table [hashcode]; s != NULL; s = s->next){
		if ((*equal)(s->key, key)){
			*orig_key = s->key;
			*value = s->value;
			return TRUE;
		}
	}
	return FALSE;
}

void
g_hash_table_foreach (GHashTable *hash, GHFunc func, gpointer user_data)
{
	int i;
	
	g_return_if_fail (hash != NULL);
	g_return_if_fail (func != NULL);

	for (i = 0; i < hash->table_size; i++){
		Slot *s;

		for (s = hash->table [i]; s != NULL; s = s->next)
			(*func)(s->key, s->value, user_data);
	}
}

gpointer
g_hash_table_find (GHashTable *hash, GHRFunc predicate, gpointer user_data)
{
	int i;
	
	g_return_if_fail (hash != NULL);
	g_return_if_fail (predicate != NULL);

	for (i = 0; i < hash->table_size; i++){
		Slot *s;

		for (s = hash->table [i]; s != NULL; s = s->next)
			if ((*predicate)(s->key, s->value, user_data))
				return;
	}
}

gboolean
g_hash_table_remove (GHashTable *hash, gconstpointer key)
{
	GEqualFunc equal;
	Slot *s, **last;
	guint hashcode;
	
	g_return_val_if_fail (hash != NULL, FALSE);
	equal = hash->key_equal_func;

	hashcode = ((*hash->hash_func)(key)) % hash->table_size;
	last = &hash->table [hashcode];
	for (s = *last; s != NULL; s = s->next){
		if ((*equal)(s->key, key)){
			if (hash->key_destroy_func != NULL)
				(*hash->key_destroy_func)(s->key);
			if (hash->value_destroy_func != NULL)
				(*hash->value_destroy_func)(s->value);
			*last = s->next;
			g_free (s);
			hash->in_use--;
			return TRUE;
		}
		last = &s;
	}
	return FALSE;
}

guint
g_hash_table_foreach_remove (GHashTable *hash, GHRFunc func, gpointer user_data)
{
	int i;
	int count = 0;
	
	g_return_val_if_fail (hash != NULL, 0);
	g_return_val_if_fail (func != NULL, 0);

	for (i = 0; i < hash->table_size; i++){
		Slot *s, **last;

		last = &hash->table [i];
		for (s = hash->table [i]; s != NULL; s = s->next){
			if ((*func)(s->key, s->value, user_data)){
				if (hash->key_destroy_func != NULL)
					(*hash->key_destroy_func)(s->key);
				if (hash->value_destroy_func != NULL)
					(*hash->value_destroy_func)(s->value);
				*last = s->next;
				g_free (s);
				hash->in_use--;
				count++;
			}
		}
	}
	if (count > 0)
		rehash (hash);
}

void
g_hash_table_destroy (GHashTable *hash)
{
	int i;
	
	g_return_if_fail (hash != NULL);

	for (i = 0; i < hash->table_size; i++){
		Slot *s, *next;

		for (s = hash->table [i]; s != NULL; s = next){
			next = s->next;
			
			if (hash->key_destroy_func != NULL)
				(*hash->key_destroy_func)(s->key);
			if (hash->value_destroy_func != NULL)
				(*hash->value_destroy_func)(s->value);
			g_free (s);
		}
	}
	g_free (hash->table);
	
	g_free (hash);
}

gboolean
g_direct_equal (gconstpointer v1, gconstpointer v2)
{
	return v1 == v2;
}

guint
g_direct_hash (gconstpointer v1)
{
	return GPOINTER_TO_UINT (v1);
}

gboolean
g_int_equal (gconstpointer v1, gconstpointer v2)
{
	return GPOINTER_TO_INT (v1) == GPOINTER_TO_INT (v2);
}

guint
g_int_hash (gconstpointer v1)
{
	return GPOINTER_TO_UINT(v1);
}

gboolean
g_str_equal (gconstpointer v1, gconstpointer v2)
{
	return strcmp (v1, v2) == 0;
}

guint
g_str_hash (gconstpointer v1)
{
	guint hash = 0;
	char *p = (char *) v1;

	while (*p++)
		hash = (hash << 5) - (hash + *p);

	return hash;
}
