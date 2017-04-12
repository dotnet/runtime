/**
 * \file
 * A mostly concurrent hashtable
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2014 Xamarin
 */

#include "mono-conc-hashtable.h"
#include <mono/utils/hazard-pointer.h>

/* Configuration knobs. */

#define INITIAL_SIZE 32
#define LOAD_FACTOR 0.75f
#define TOMBSTONE ((gpointer)(ssize_t)-1)

typedef struct {
	gpointer key;
	gpointer value;
} key_value_pair;

typedef struct {
	int table_size;
	key_value_pair *kvs;
} conc_table;

struct _MonoConcurrentHashTable {
	volatile conc_table *table; /* goes to HP0 */
	GHashFunc hash_func;
	GEqualFunc equal_func;
	int element_count;
	int overflow_count;
	GDestroyNotify key_destroy_func;
	GDestroyNotify value_destroy_func;
};

static conc_table*
conc_table_new (int size)
{
	conc_table *res = g_new (conc_table, 1);
	res->table_size = size;
	res->kvs = g_new0 (key_value_pair, size);
	return res;
}

static void
conc_table_free (gpointer ptr)
{
	conc_table *table = (conc_table *)ptr;
	g_free (table->kvs);
	g_free (table);
}

static void
conc_table_lf_free (conc_table *table)
{
	mono_thread_hazardous_try_free (table, conc_table_free);
}


/*
A common problem with power of two hashtables is that it leads of bad clustering when dealing
with aligned numbers.

The solution here is to mix the bits from two primes plus the hash itself, it produces a better spread
than just the numbers.
*/

static MONO_ALWAYS_INLINE int
mix_hash (int hash)
{
	return ((hash * 215497) >> 16) ^ (hash * 1823231 + hash);
}

static MONO_ALWAYS_INLINE void
insert_one_local (conc_table *table, GHashFunc hash_func, gpointer key, gpointer value)
{
	key_value_pair *kvs = table->kvs;
	int table_mask = table->table_size - 1;
	int hash = mix_hash (hash_func (key));
	int i = hash & table_mask;

	while (table->kvs [i].key)
		i = (i + 1) & table_mask;

	kvs [i].key = key;
	kvs [i].value = value;
}

/* LOCKING: Must be called holding hash_table->mutex */
static void
expand_table (MonoConcurrentHashTable *hash_table)
{
	conc_table *old_table = (conc_table*)hash_table->table;
	conc_table *new_table = conc_table_new (old_table->table_size * 2);
	key_value_pair *kvs = old_table->kvs;
	int i;

	for (i = 0; i < old_table->table_size; ++i) {
		if (kvs [i].key && kvs [i].key != TOMBSTONE)
			insert_one_local (new_table, hash_table->hash_func, kvs [i].key, kvs [i].value);
	}
	mono_memory_barrier ();
	hash_table->table = new_table;
	hash_table->overflow_count = (int)(new_table->table_size * LOAD_FACTOR);
	conc_table_lf_free (old_table);
}


MonoConcurrentHashTable*
mono_conc_hashtable_new (GHashFunc hash_func, GEqualFunc key_equal_func)
{
	MonoConcurrentHashTable *res = g_new0 (MonoConcurrentHashTable, 1);
	res->hash_func = hash_func ? hash_func : g_direct_hash;
	res->equal_func = key_equal_func;
	// res->equal_func = g_direct_equal;
	res->table = conc_table_new (INITIAL_SIZE);
	res->element_count = 0;
	res->overflow_count = (int)(INITIAL_SIZE * LOAD_FACTOR);
	return res;
}

MonoConcurrentHashTable*
mono_conc_hashtable_new_full (GHashFunc hash_func, GEqualFunc key_equal_func, GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func)
{
	MonoConcurrentHashTable *res = mono_conc_hashtable_new (hash_func, key_equal_func);
	res->key_destroy_func = key_destroy_func;
	res->value_destroy_func = value_destroy_func;
	return res;
}


void
mono_conc_hashtable_destroy (MonoConcurrentHashTable *hash_table)
{
	if (hash_table->key_destroy_func || hash_table->value_destroy_func) {
		int i;
		conc_table *table = (conc_table*)hash_table->table;
		key_value_pair *kvs = table->kvs;

		for (i = 0; i < table->table_size; ++i) {
			if (kvs [i].key && kvs [i].key != TOMBSTONE) {
				if (hash_table->key_destroy_func)
					(hash_table->key_destroy_func) (kvs [i].key);
				if (hash_table->value_destroy_func)
					(hash_table->value_destroy_func) (kvs [i].value);
			}
		}
	}
	conc_table_free ((gpointer)hash_table->table);
	g_free (hash_table);
}

gpointer
mono_conc_hashtable_lookup (MonoConcurrentHashTable *hash_table, gpointer key)
{
	MonoThreadHazardPointers* hp;
	conc_table *table;
	int hash, i, table_mask;
	key_value_pair *kvs;
	hash = mix_hash (hash_table->hash_func (key));
	hp = mono_hazard_pointer_get ();

retry:
	table = (conc_table *)mono_get_hazardous_pointer ((gpointer volatile*)&hash_table->table, hp, 0);
	table_mask = table->table_size - 1;
	kvs = table->kvs;
	i = hash & table_mask;

	if (G_LIKELY (!hash_table->equal_func)) {
		while (kvs [i].key) {
			if (key == kvs [i].key) {
				gpointer value;
				/* The read of keys must happen before the read of values */
				mono_memory_barrier ();
				value = kvs [i].value;
				/* FIXME check for NULL if we add suppport for removal */
				mono_hazard_pointer_clear (hp, 0);
				return value;
			}
			i = (i + 1) & table_mask;
		}
	} else {
		GEqualFunc equal = hash_table->equal_func;

		while (kvs [i].key) {
			if (kvs [i].key != TOMBSTONE && equal (key, kvs [i].key)) {
				gpointer value;
				/* The read of keys must happen before the read of values */
				mono_memory_barrier ();
				value = kvs [i].value;

				/* We just read a value been deleted, try again. */
				if (G_UNLIKELY (!value))
					goto retry;

				mono_hazard_pointer_clear (hp, 0);
				return value;
			}
			i = (i + 1) & table_mask;
		}
	}

	/* The table might have expanded and the value is now on the newer table */
	mono_memory_barrier ();
	if (hash_table->table != table)
		goto retry;

	mono_hazard_pointer_clear (hp, 0);
	return NULL;
}

/**
 * mono_conc_hashtable_remove:
 * Remove a value from the hashtable. Requires external locking
 * \returns the old value if \p key is already present or NULL
 */
gpointer
mono_conc_hashtable_remove (MonoConcurrentHashTable *hash_table, gpointer key)
{
	conc_table *table;
	key_value_pair *kvs;
	int hash, i, table_mask;

	g_assert (key != NULL && key != TOMBSTONE);

	hash = mix_hash (hash_table->hash_func (key));

	table = (conc_table*)hash_table->table;
	kvs = table->kvs;
	table_mask = table->table_size - 1;
	i = hash & table_mask;

	if (!hash_table->equal_func) {
		for (;;) {
			if (!kvs [i].key) {
				return NULL; /*key not found*/
			}

			if (key == kvs [i].key) {
				gpointer value = kvs [i].value;
				kvs [i].value = NULL;
				mono_memory_barrier ();
				kvs [i].key = TOMBSTONE;
				--hash_table->element_count;

				if (hash_table->key_destroy_func != NULL)
					(*hash_table->key_destroy_func) (key);
				if (hash_table->value_destroy_func != NULL)
					(*hash_table->value_destroy_func) (value);

				return value;
			}
			i = (i + 1) & table_mask;
		}
	} else {
		GEqualFunc equal = hash_table->equal_func;
		for (;;) {
			if (!kvs [i].key) {
				return NULL; /*key not found*/
			}

			if (kvs [i].key != TOMBSTONE && equal (key, kvs [i].key)) {
				gpointer old_key = kvs [i].key;
				gpointer value = kvs [i].value;
				kvs [i].value = NULL;
				mono_memory_barrier ();
				kvs [i].key = TOMBSTONE;

				if (hash_table->key_destroy_func != NULL)
					(*hash_table->key_destroy_func) (old_key);
				if (hash_table->value_destroy_func != NULL)
					(*hash_table->value_destroy_func) (value);
				return value;
			}

			i = (i + 1) & table_mask;
		}
	}
}
/**
 * mono_conc_hashtable_insert:
 * Insert a value into the hashtable. Requires external locking.
 * \returns the old value if \p key is already present or NULL
 */
gpointer
mono_conc_hashtable_insert (MonoConcurrentHashTable *hash_table, gpointer key, gpointer value)
{
	conc_table *table;
	key_value_pair *kvs;
	int hash, i, table_mask;

	g_assert (key != NULL && key != TOMBSTONE);
	g_assert (value != NULL);

	hash = mix_hash (hash_table->hash_func (key));

	if (hash_table->element_count >= hash_table->overflow_count)
		expand_table (hash_table);

	table = (conc_table*)hash_table->table;
	kvs = table->kvs;
	table_mask = table->table_size - 1;
	i = hash & table_mask;

	if (!hash_table->equal_func) {
		for (;;) {
			if (!kvs [i].key || kvs [i].key == TOMBSTONE) {
				kvs [i].value = value;
				/* The write to values must happen after the write to keys */
				mono_memory_barrier ();
				kvs [i].key = key;
				++hash_table->element_count;
				return NULL;
			}
			if (key == kvs [i].key) {
				gpointer value = kvs [i].value;
				return value;
			}
			i = (i + 1) & table_mask;
		}
	} else {
		GEqualFunc equal = hash_table->equal_func;
		for (;;) {
			if (!kvs [i].key || kvs [i].key == TOMBSTONE) {
				kvs [i].value = value;
				/* The write to values must happen after the write to keys */
				mono_memory_barrier ();
				kvs [i].key = key;
				++hash_table->element_count;
				return NULL;
			}
			if (equal (key, kvs [i].key)) {
				gpointer value = kvs [i].value;
				return value;
			}
			i = (i + 1) & table_mask;
		}
	}
}

/**
 * mono_conc_hashtable_foreach:
 * Calls \p func for each value in the hashtable. Requires external locking.
 */
void
mono_conc_hashtable_foreach (MonoConcurrentHashTable *hash_table, GHFunc func, gpointer userdata)
{
	int i;
	conc_table *table = (conc_table*)hash_table->table;
	key_value_pair *kvs = table->kvs;

	for (i = 0; i < table->table_size; ++i) {
		if (kvs [i].key && kvs [i].key != TOMBSTONE) {
			func (kvs [i].key, kvs [i].value, userdata);
		}
	}
}

/**
 * mono_conc_hashtable_foreach_steal:
 *
 * Calls @func for each entry in the hashtable, if @func returns true, remove from the hashtable. Requires external locking.
 * Same semantics as g_hash_table_foreach_steal.
 */
void
mono_conc_hashtable_foreach_steal (MonoConcurrentHashTable *hash_table, GHRFunc func, gpointer userdata)
{
	int i;
	conc_table *table = (conc_table*)hash_table->table;
	key_value_pair *kvs = table->kvs;

	for (i = 0; i < table->table_size; ++i) {
		if (kvs [i].key && kvs [i].key != TOMBSTONE) {
			if (func (kvs [i].key, kvs [i].value, userdata)) {
				kvs [i].value = NULL;
				mono_memory_barrier ();
				kvs [i].key = TOMBSTONE;
				--hash_table->element_count;
			}
		}
	}
}
