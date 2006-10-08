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
 *
 * NOTES:
 *
 *   In addition to supporting the standard g_hash_table behavior,
 *   this version adds support for providing a malloc/free set of
 *   routines, these by default are malloc and free, but with a GC,
 *   they could be GC_malloc and the free routine could be NULL.  If
 *   the value for free is NULL, we set all the fields that we would
 *   have otherwise released to NULL, to assist the garbage collector.
 *   
 *   This is designed to support the GC-aware hashtable that Mono uses,
 *   and replaces mono-hash.c.   To do this, a new constructor is introduced
 *   that contains the extra parameters.
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
	int   last_rehash;
	GDestroyNotify value_destroy_func, key_destroy_func;
	GMFree  freefn;
	GMAlloc allocfn;
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

#undef g_new0
static void *
g_new0 (size_t t)
{
	return calloc (1, t);
}

GHashTable *
g_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func)
{
	return g_hash_table_new_full_alloc (hash_func, key_equal_func, NULL, NULL, g_new0, free);
}

GHashTable *
g_hash_table_new_alloc (GHashFunc hash_func, GEqualFunc key_equal_func, GMAlloc allocfn, GMFree freefn)
{
	return g_hash_table_new_full_alloc (hash_func, key_equal_func, NULL, NULL, allocfn, freefn);
}


GHashTable *
g_hash_table_new_full (GHashFunc hash_func, GEqualFunc key_equal_func,
		       GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func)
{
	return g_hash_table_new_full_alloc (hash_func, key_equal_func, key_destroy_func, value_destroy_func, g_new0, free);
}

GHashTable *
g_hash_table_new_full_alloc (GHashFunc hash_func, GEqualFunc key_equal_func,
			     GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func,
			     GMAlloc allocfn, GMFree freefn)
{
	GHashTable *hash;

	if (hash_func == NULL)
		hash_func = g_direct_hash;
	if (key_equal_func == NULL)
		key_equal_func = g_direct_equal;

	hash = (*allocfn) (sizeof (GHashTable));

	hash->hash_func = hash_func;
	hash->key_equal_func = key_equal_func;
	hash->key_destroy_func = key_destroy_func;
	hash->value_destroy_func = value_destroy_func;
	hash->table_size = to_prime (1);
	hash->table = (*allocfn) (sizeof (Slot *) * hash->table_size);
	hash->last_rehash = hash->table_size;
	hash->freefn = freefn;
	hash->allocfn = allocfn;
	
	return hash;
}

void
do_rehash (GHashTable *hash)
{
	int current_size, i;
	Slot **table;

	/* printf ("Resizing diff=%d slots=%d\n", hash->in_use - hash->last_rehash, hash->table_size); */
	hash->last_rehash = hash->table_size;
	current_size = hash->table_size;
	hash->table_size = to_prime (hash->in_use);
	/* printf ("New size: %d\n", hash->table_size); */
	table = hash->table;
	hash->table = (*hash->allocfn)(sizeof (Slot *) * hash->table_size);
	
	for (i = 0; i < current_size; i++){
		Slot *s, *next;

		for (s = table [i]; s != NULL; s = next){
			guint hashcode = ((*hash->hash_func) (s->key)) % hash->table_size;
			next = s->next;

			s->next = hash->table [hashcode];
			hash->table [hashcode] = s;
		}
	}
	(*hash->freefn)(table);
}

void
rehash (GHashTable *hash)
{
	int diff = ABS (hash->last_rehash, hash->in_use);

	/* These are the factors to play with to change the rehashing strategy */
	/* I played with them with a large range, and could not really get */
	/* something that was too good, maybe the tests are not that great */
	if (!(diff * 0.75 > hash->table_size * 2))
		return;
	do_rehash (hash);
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
	s = (*hash->allocfn) (sizeof (Slot));
	s->key = key;
	s->value = value;
	s->next = hash->table [hashcode];
	hash->table [hashcode] = s;
	hash->in_use++;
}

guint
g_hash_table_size (GHashTable *hash)
{
	g_return_val_if_fail (hash != NULL, 0);
	
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
	
	g_return_val_if_fail (hash != NULL, FALSE);
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
	
	g_return_val_if_fail (hash != NULL, NULL);
	g_return_val_if_fail (predicate != NULL, NULL);

	for (i = 0; i < hash->table_size; i++){
		Slot *s;

		for (s = hash->table [i]; s != NULL; s = s->next)
			if ((*predicate)(s->key, s->value, user_data))
				return s->value;
	}
	return NULL;
}

gboolean
g_hash_table_remove (GHashTable *hash, gconstpointer key)
{
	GEqualFunc equal;
	Slot *s, *last;
	guint hashcode;
	
	g_return_val_if_fail (hash != NULL, FALSE);
	equal = hash->key_equal_func;

	hashcode = ((*hash->hash_func)(key)) % hash->table_size;
	last = NULL;
	for (s = hash->table [hashcode]; s != NULL; s = s->next){
		if ((*equal)(s->key, key)){
			if (hash->key_destroy_func != NULL)
				(*hash->key_destroy_func)(s->key);
			if (hash->value_destroy_func != NULL)
				(*hash->value_destroy_func)(s->value);
			if (last == NULL)
				hash->table [hashcode] = s->next;
			else
				last->next = s->next;
			if (hash->freefn)
				(*hash->freefn) (s);
			else {
				s->key = NULL;
				s->value = NULL;
			}
			hash->in_use--;
			return TRUE;
		}
		last = s;
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
		Slot *s, *last;

		last = NULL;
		for (s = hash->table [i]; s != NULL; ){
			if ((*func)(s->key, s->value, user_data)){
				Slot *n;

				if (hash->key_destroy_func != NULL)
					(*hash->key_destroy_func)(s->key);
				if (hash->value_destroy_func != NULL)
					(*hash->value_destroy_func)(s->value);
				if (last == NULL){
					hash->table [i] = s->next;
					n = s->next;
				} else  {
					last->next = s->next;
					n = last->next;
				}
				if (hash->freefn)
					(*hash->freefn)(s);
				else {
					s->key = NULL;
					s->value = NULL;
				}
				
				hash->in_use--;
				count++;
				s = n;
			} else {
				last = s;
				s = s->next;
			}
		}
	}
	if (count > 0)
		rehash (hash);
	return count;
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
			if (hash->freefn)
				(*hash->freefn)(s);
			else {
				s->key = NULL;
				s->value = NULL;
			}
		}
	}
	if (hash->freefn){
		(*hash->freefn) (hash->table);
		(*hash->freefn) (hash);
	} else {
		hash->table = NULL;
	}
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
