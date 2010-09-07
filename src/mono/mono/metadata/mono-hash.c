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
#include <config.h>
#include <stdio.h>
#include <math.h>
#include <glib.h>
#include "mono-hash.h"
#include "metadata/gc-internal.h"

#ifdef HAVE_BOEHM_GC
#define mg_new0(type,n)  ((type *) GC_MALLOC(sizeof(type) * (n)))
#define mg_new(type,n)   ((type *) GC_MALLOC(sizeof(type) * (n)))
#define mg_free(x)       do { } while (0)
#else
#define mg_new0(x,n)     g_new0(x,n)
#define mg_new(type,n)   g_new(type,n)
#define mg_free(x)       g_free(x)
#endif

typedef struct _Slot Slot;

struct _Slot {
	gpointer key;
	gpointer value;
	Slot    *next;
};

static gpointer KEYMARKER_REMOVED = &KEYMARKER_REMOVED;

struct _MonoGHashTable {
	GHashFunc      hash_func;
	GEqualFunc     key_equal_func;

	Slot **table;
	int   table_size;
	int   in_use;
	int   threshold;
	int   last_rehash;
	GDestroyNotify value_destroy_func, key_destroy_func;
	MonoGHashGCType gc_type;
};

static const int prime_tbl[] = {
	11, 19, 37, 73, 109, 163, 251, 367, 557, 823, 1237,
	1861, 2777, 4177, 6247, 9371, 14057, 21089, 31627,
	47431, 71143, 106721, 160073, 240101, 360163,
	540217, 810343, 1215497, 1823231, 2734867, 4102283,
	6153409, 9230113, 13845163
};

#ifdef HAVE_SGEN_GC
static void *table_hash_descr = NULL;

static void mono_g_hash_mark (void *addr, MonoGCMarkFunc mark_func);

static Slot*
new_slot (MonoGHashTable *hash)
{
	if (hash->gc_type == MONO_HASH_CONSERVATIVE_GC)
		return mono_gc_alloc_fixed (sizeof (Slot), NULL);
	else
		return mg_new (Slot, 1);
}

static void
free_slot (MonoGHashTable *hash, Slot *slot)
{
	if (hash->gc_type == MONO_HASH_CONSERVATIVE_GC)
		mono_gc_free_fixed (slot);
	else
		mg_free (slot);
}
#else
#define new_slot(h)	mg_new(Slot,1)
#define free_slot(h,s)	mg_free((s))
#endif

#if UNUSED
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
#endif

MonoGHashTable *
mono_g_hash_table_new_type (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type)
{
	MonoGHashTable *hash = mono_g_hash_table_new (hash_func, key_equal_func);

	hash->gc_type = type;

#ifdef HAVE_SGEN_GC
	if (type < 0 || type > MONO_HASH_KEY_VALUE_GC)
		g_error ("wrong type for gc hashtable");
	/*
	 * We use a user defined marking function to avoid having to register a GC root for
	 * each hash node.
	 */
	if (!table_hash_descr)
		table_hash_descr = mono_gc_make_root_descr_user (mono_g_hash_mark);
	if (type != MONO_HASH_CONSERVATIVE_GC)
		mono_gc_register_root_wbarrier ((char*)hash, sizeof (MonoGHashTable), table_hash_descr);
#endif

	return hash;
}

MonoGHashTable *
mono_g_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func)
{
	MonoGHashTable *hash;

	if (hash_func == NULL)
		hash_func = g_direct_hash;
	if (key_equal_func == NULL)
		key_equal_func = g_direct_equal;
	hash = mg_new0 (MonoGHashTable, 1);

	hash->hash_func = hash_func;
	hash->key_equal_func = key_equal_func;

	hash->table_size = g_spaced_primes_closest (1);
	hash->table = mg_new0 (Slot *, hash->table_size);
	hash->last_rehash = hash->table_size;
	
	return hash;
}

MonoGHashTable *
mono_g_hash_table_new_full (GHashFunc hash_func, GEqualFunc key_equal_func,
			    GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func)
{
	MonoGHashTable *hash = mono_g_hash_table_new (hash_func, key_equal_func);
	if (hash == NULL)
		return NULL;
	
	hash->key_destroy_func = key_destroy_func;
	hash->value_destroy_func = value_destroy_func;
	
	return hash;
}

typedef struct {
	MonoGHashTable *hash;
	int new_size;
	Slot **table;
} RehashData;

static void*
do_rehash (void *_data)
{
	RehashData *data = _data;
	MonoGHashTable *hash = data->hash;
	int current_size, i;
	Slot **table;

	/* printf ("Resizing diff=%d slots=%d\n", hash->in_use - hash->last_rehash, hash->table_size); */
	hash->last_rehash = hash->table_size;
	current_size = hash->table_size;
	hash->table_size = data->new_size;
	/* printf ("New size: %d\n", hash->table_size); */
	table = hash->table;
	hash->table = data->table;

	for (i = 0; i < current_size; i++){
		Slot *s, *next;

		for (s = table [i]; s != NULL; s = next){
			guint hashcode = ((*hash->hash_func) (s->key)) % hash->table_size;
			next = s->next;

			s->next = hash->table [hashcode];
			hash->table [hashcode] = s;
		}
	}
	return table;
}

static void
rehash (MonoGHashTable *hash)
{
	int diff = ABS (hash->last_rehash - hash->in_use);
	RehashData data;
	void *old_table;

	/* These are the factors to play with to change the rehashing strategy */
	/* I played with them with a large range, and could not really get */
	/* something that was too good, maybe the tests are not that great */
	if (!(diff * 0.75 > hash->table_size * 2))
		return;

	data.hash = hash;
	data.new_size = g_spaced_primes_closest (hash->in_use);
	data.table = mg_new0 (Slot *, data.new_size);

	old_table = mono_gc_invoke_with_gc_lock (do_rehash, &data);
	mg_free (old_table);
}

guint
mono_g_hash_table_size (MonoGHashTable *hash)
{
	g_return_val_if_fail (hash != NULL, 0);
	
	return hash->in_use;
}

gpointer
mono_g_hash_table_lookup (MonoGHashTable *hash, gconstpointer key)
{
	gpointer orig_key, value;
	
	if (mono_g_hash_table_lookup_extended (hash, key, &orig_key, &value))
		return value;
	else
		return NULL;
}

gboolean
mono_g_hash_table_lookup_extended (MonoGHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value)
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
mono_g_hash_table_foreach (MonoGHashTable *hash, GHFunc func, gpointer user_data)
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
mono_g_hash_table_find (MonoGHashTable *hash, GHRFunc predicate, gpointer user_data)
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
mono_g_hash_table_remove (MonoGHashTable *hash, gconstpointer key)
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
			free_slot (hash, s);
			hash->in_use--;
			return TRUE;
		}
		last = s;
	}
	return FALSE;
}

guint
mono_g_hash_table_foreach_remove (MonoGHashTable *hash, GHRFunc func, gpointer user_data)
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
				free_slot (hash, s);
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
mono_g_hash_table_destroy (MonoGHashTable *hash)
{
	int i;
	
	g_return_if_fail (hash != NULL);

#ifdef HAVE_SGEN_GC
	mono_gc_deregister_root ((char*)hash);
#endif

	for (i = 0; i < hash->table_size; i++){
		Slot *s, *next;

		for (s = hash->table [i]; s != NULL; s = next){
			next = s->next;
			
			if (hash->key_destroy_func != NULL)
				(*hash->key_destroy_func)(s->key);
			if (hash->value_destroy_func != NULL)
				(*hash->value_destroy_func)(s->value);
			free_slot (hash, s);
		}
	}
	mg_free (hash->table);
	mg_free (hash);
}

static void
mono_g_hash_table_insert_replace (MonoGHashTable *hash, gpointer key, gpointer value, gboolean replace)
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
	s = new_slot (hash);
	s->key = key;
	s->value = value;
	s->next = hash->table [hashcode];
	hash->table [hashcode] = s;
	hash->in_use++;
}

void
mono_g_hash_table_insert (MonoGHashTable *h, gpointer k, gpointer v)
{
	mono_g_hash_table_insert_replace (h, k, v, FALSE);
}

void
mono_g_hash_table_replace(MonoGHashTable *h, gpointer k, gpointer v)
{
	mono_g_hash_table_insert_replace (h, k, v, TRUE);
}

void
mono_g_hash_table_print_stats (MonoGHashTable *table)
{
	int i, chain_size, max_chain_size;
	Slot *node;

	max_chain_size = 0;
	for (i = 0; i < table->table_size; i++) {
		chain_size = 0;
		for (node = table->table [i]; node; node = node->next)
			chain_size ++;
		max_chain_size = MAX(max_chain_size, chain_size);
	}

	printf ("Size: %d Table Size: %d Max Chain Length: %d\n", table->in_use, table->table_size, max_chain_size);
}

#ifdef HAVE_SGEN_GC

/* GC marker function */
static void
mono_g_hash_mark (void *addr, MonoGCMarkFunc mark_func)
{
	MonoGHashTable *table = (MonoGHashTable*)addr;
	Slot *node;
	int i;

	if (table->gc_type == MONO_HASH_KEY_GC) {
		for (i = 0; i < table->table_size; i++) {
			for (node = table->table [i]; node; node = node->next) {
				if (node->key)
					mark_func (&node->key);
			}
		}
	} else if (table->gc_type == MONO_HASH_VALUE_GC) {
		for (i = 0; i < table->table_size; i++) {
			for (node = table->table [i]; node; node = node->next) {
				if (node->value)
					mark_func (&node->value);
			}
		}
	} else if (table->gc_type == MONO_HASH_KEY_VALUE_GC) {
		for (i = 0; i < table->table_size; i++) {
			for (node = table->table [i]; node; node = node->next) {
				if (node->key)
					mark_func (&node->key);
				if (node->value)
					mark_func (&node->value);
			}
		}
	}
}
	
#endif
