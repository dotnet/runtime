/**
 * \file
 * Conc GC aware Hashtable implementation
 *
 * Author:
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 */
#include <config.h>
#include <stdio.h>
#include <math.h>
#include <glib.h>
#include "mono-conc-hash.h"
#include "metadata/gc-internals.h"
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads-coop.h>

#define INITIAL_SIZE 32
#define LOAD_FACTOR 0.75f
#define PTR_TOMBSTONE ((gpointer)(ssize_t)-1)
/* Expand ration must be a power of two */
#define EXPAND_RATIO 2

typedef struct {
	int table_size;
	MonoGHashGCType gc_type;
	void **keys;
	void **values;
} conc_table;

struct _MonoConcGHashTable {
	volatile conc_table *table; /* goes to HP0 */
	GHashFunc hash_func;
	GEqualFunc equal_func;
	int element_count; //KVP + tombstones
	int tombstone_count; //just tombstones
	int overflow_count;
	GDestroyNotify key_destroy_func;
	GDestroyNotify value_destroy_func;
	MonoGHashGCType gc_type;
	MonoGCRootSource source;
	void *key;
	const char *msg;
};


static conc_table*
conc_table_new (MonoConcGHashTable *hash, int size)
{
	conc_table *table = g_new0 (conc_table, 1);

	table->keys = g_new0 (void*, size);
	table->values = g_new0 (void*, size);
	table->table_size = size;
	table->gc_type = hash->gc_type;

	if (hash->gc_type & MONO_HASH_KEY_GC)
		mono_gc_register_root_wbarrier ((char*)table->keys, sizeof (MonoObject*) * size, mono_gc_make_vector_descr (), hash->source, hash->key, hash->msg);
	if (hash->gc_type & MONO_HASH_VALUE_GC)
		mono_gc_register_root_wbarrier ((char*)table->values, sizeof (MonoObject*) * size, mono_gc_make_vector_descr (), hash->source, hash->key, hash->msg);

	return table;
}

static void
conc_table_free (gpointer ptr)
{
	MONO_REQ_GC_UNSAFE_MODE;

	conc_table *table = (conc_table *)ptr;
	if (table->gc_type & MONO_HASH_KEY_GC)
		mono_gc_deregister_root ((char*)table->keys);
	if (table->gc_type & MONO_HASH_VALUE_GC)
		mono_gc_deregister_root ((char*)table->values);

	g_free (table->keys);
	g_free (table->values);
	g_free (table);
}

static void
conc_table_lf_free (conc_table *table)
{
	mono_thread_hazardous_try_free (table, conc_table_free);
}


static gboolean
key_is_tombstone (MonoConcGHashTable *hash, gpointer ptr)
{
	if (hash->gc_type & MONO_HASH_KEY_GC)
		return ptr == mono_domain_get()->ephemeron_tombstone;
	return ptr == PTR_TOMBSTONE;
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


static void
set_key (conc_table *table, int slot, gpointer key)
{
	gpointer *key_addr = &table->keys [slot];
	if (table->gc_type & MONO_HASH_KEY_GC)
		mono_gc_wbarrier_generic_store_internal (key_addr, (MonoObject*)key);
	else
		*key_addr = key;
}

static void
set_key_to_tombstone (conc_table *table, int slot)
{
	gpointer *key_addr = &table->keys [slot];
	if (table->gc_type & MONO_HASH_KEY_GC)
		mono_gc_wbarrier_generic_store_internal (key_addr, mono_domain_get()->ephemeron_tombstone);
	else
		*key_addr = PTR_TOMBSTONE;
}

static void
set_value (conc_table *table, int slot, gpointer value)
{
	gpointer *value_addr = &table->values [slot];
	if (table->gc_type & MONO_HASH_VALUE_GC)
		mono_gc_wbarrier_generic_store_internal (value_addr, (MonoObject*)value);
	else
		*value_addr = value;
}

static MONO_ALWAYS_INLINE void
insert_one_local (conc_table *table, GHashFunc hash_func, gpointer key, gpointer value)
{
	int table_mask = table->table_size - 1;
	int hash = mix_hash (hash_func (key));
	int i = hash & table_mask;

	while (table->keys [i])
		i = (i + 1) & table_mask;

	set_key (table, i, key);
	set_value (table, i, value);
}

static void
rehash_table (MonoConcGHashTable *hash_table, int multiplier)
{
	conc_table *old_table = (conc_table*)hash_table->table;
	conc_table *new_table = conc_table_new (hash_table, old_table->table_size * multiplier);
	int i;

	for (i = 0; i < old_table->table_size; ++i) {
		if (old_table->keys [i] && !key_is_tombstone (hash_table, old_table->keys [i]))
			insert_one_local (new_table, hash_table->hash_func, old_table->keys [i], old_table->values [i]);
	}

	mono_memory_barrier ();
	hash_table->table = new_table;
	hash_table->overflow_count = (int)(new_table->table_size * LOAD_FACTOR);
	hash_table->element_count -= hash_table->tombstone_count;
	hash_table->tombstone_count = 0;
	conc_table_lf_free (old_table);
}


static void
check_table_size (MonoConcGHashTable *hash_table)
{
	if (hash_table->element_count >= hash_table->overflow_count) {
		//if we have more tombstones than KVP we rehash to the same size
		if (hash_table->tombstone_count > hash_table->element_count / 2)
			rehash_table (hash_table, 1);
		else
			rehash_table (hash_table, EXPAND_RATIO);
	}
}

MonoConcGHashTable *
mono_conc_g_hash_table_new_type (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type, MonoGCRootSource source, void *key, const char *msg)
{
	MonoConcGHashTable *hash;

	if (!hash_func)
		hash_func = g_direct_hash;

	hash = g_new0 (MonoConcGHashTable, 1);
	hash->hash_func = hash_func;
	hash->equal_func = key_equal_func;

	hash->element_count = 0;
	hash->overflow_count = (int)(INITIAL_SIZE * LOAD_FACTOR);
	hash->gc_type = type;
	hash->source = source;
	hash->key = key;
	hash->msg = msg;

	hash->table = conc_table_new (hash, INITIAL_SIZE);

	if (type > MONO_HASH_KEY_VALUE_GC)
		g_error ("wrong type for gc hashtable");

	return hash;
}

gpointer
mono_conc_g_hash_table_lookup (MonoConcGHashTable *hash, gconstpointer key)
{
	gpointer orig_key, value;

	if (mono_conc_g_hash_table_lookup_extended (hash, key, &orig_key, &value))
		return value;
	else
		return NULL;
}

gboolean
mono_conc_g_hash_table_lookup_extended (MonoConcGHashTable *hash_table, gconstpointer key, gpointer *orig_key_ptr, gpointer *value_ptr)
{
	MonoThreadHazardPointers* hp;
	conc_table *table;
	int hash, i, table_mask;
	hash = mix_hash (hash_table->hash_func (key));
	hp = mono_hazard_pointer_get ();

retry:
	table = (conc_table *)mono_get_hazardous_pointer ((gpointer volatile*)&hash_table->table, hp, 0);
	table_mask = table->table_size - 1;
	i = hash & table_mask;

	if (G_LIKELY (!hash_table->equal_func)) {
		while (table->keys [i]) {
			gpointer orig_key = table->keys [i];
			if (key == orig_key) {
				gpointer value;
				/* The read of keys must happen before the read of values */
				mono_memory_barrier ();
				value = table->values [i];

				/* We just read a value been deleted, try again. */
				if (G_UNLIKELY (!value))
					goto retry;

				mono_hazard_pointer_clear (hp, 0);

				*orig_key_ptr = orig_key;
				*value_ptr = value;
				return TRUE;
			}
			i = (i + 1) & table_mask;
		}
	} else {
		GEqualFunc equal = hash_table->equal_func;

		while (table->keys [i]) {
			gpointer orig_key = table->keys [i];
			if (!key_is_tombstone (hash_table, orig_key) && equal (key, orig_key)) {
				gpointer value;
				/* The read of keys must happen before the read of values */
				mono_memory_barrier ();
				value = table->values [i];

				/* We just read a value been deleted, try again. */
				if (G_UNLIKELY (!value))
					goto retry;

				mono_hazard_pointer_clear (hp, 0);
				*orig_key_ptr = orig_key;
				*value_ptr = value;
				return TRUE;

			}
			i = (i + 1) & table_mask;
		}
	}

	/* The table might have expanded and the value is now on the newer table */
	mono_memory_barrier ();
	if (hash_table->table != table)
		goto retry;

	mono_hazard_pointer_clear (hp, 0);

	*orig_key_ptr = NULL;
	*value_ptr = NULL;
	return FALSE;
}

void
mono_conc_g_hash_table_foreach (MonoConcGHashTable *hash_table, GHFunc func, gpointer user_data)
{
	int i;
	conc_table *table = (conc_table*)hash_table->table;

	for (i = 0; i < table->table_size; ++i) {
		if (table->keys [i] && !key_is_tombstone (hash_table, table->keys [i])) {
			func (table->keys [i], table->values [i], user_data);
		}
	}
}

void
mono_conc_g_hash_table_destroy (MonoConcGHashTable *hash_table)
{
	if (hash_table->key_destroy_func || hash_table->value_destroy_func) {
		int i;
		conc_table *table = (conc_table*)hash_table->table;

		for (i = 0; i < table->table_size; ++i) {
			if (table->keys [i] && !key_is_tombstone (hash_table, table->keys [i])) {
				if (hash_table->key_destroy_func)
					(hash_table->key_destroy_func) (table->keys [i]);
				if (hash_table->value_destroy_func)
					(hash_table->value_destroy_func) (table->values [i]);
			}
		}
	}
	conc_table_free ((gpointer)hash_table->table);
	g_free (hash_table);
}

/* Return NULL on success or the old value in failure */
gpointer
mono_conc_g_hash_table_insert (MonoConcGHashTable *hash_table, gpointer key, gpointer value)
{
	conc_table *table;
	int hash, i, table_mask;

	g_assert (key != NULL);
	g_assert (value != NULL);

	hash = mix_hash (hash_table->hash_func (key));

	check_table_size (hash_table);

	table = (conc_table*)hash_table->table;
	table_mask = table->table_size - 1;
	i = hash & table_mask;

	if (!hash_table->equal_func) {
		for (;;) {
			gpointer cur_key = table->keys [i];
			gboolean is_tombstone = FALSE;
			if (!cur_key || (is_tombstone = key_is_tombstone (hash_table, cur_key))) {
				set_value (table, i, value);

				/* The write to values must happen after the write to keys */
				mono_memory_barrier ();
				set_key (table, i, key);
				if (is_tombstone)
					--hash_table->tombstone_count;
				else
					++hash_table->element_count;

				return NULL;
			}
			if (key == cur_key) {
				return table->values [i];
			}
			i = (i + 1) & table_mask;
		}
	} else {
		GEqualFunc equal = hash_table->equal_func;
		for (;;) {
			gpointer cur_key = table->keys [i];
			gboolean is_tombstone = FALSE;
			if (!cur_key || (is_tombstone = key_is_tombstone (hash_table, cur_key))) {
				set_value (table, i, value);
				/* The write to values must happen after the write to keys */
				mono_memory_barrier ();
				set_key (table, i, key);
				if (is_tombstone)
					--hash_table->tombstone_count;
				else
					++hash_table->element_count;

				return NULL;
			}
			if (equal (key, cur_key)) {
				return table->values [i];
			}
			i = (i + 1) & table_mask;
		}
	}
}

gpointer
mono_conc_g_hash_table_remove (MonoConcGHashTable *hash_table, gconstpointer key)
{
	conc_table *table;
	int hash, i, table_mask;

	g_assert (key != NULL);

	hash = mix_hash (hash_table->hash_func (key));

	table = (conc_table*)hash_table->table;
	table_mask = table->table_size - 1;
	i = hash & table_mask;

	if (!hash_table->equal_func) {
		for (;;) {
			gpointer cur_key = table->keys [i];
			if (!cur_key) {
				return NULL; /*key not found*/
			}

			if (key == cur_key) {
				gpointer value = table->values [i];
				table->values [i] = NULL;
				mono_memory_barrier ();
				set_key_to_tombstone (table, i);
				++hash_table->tombstone_count;

				if (hash_table->key_destroy_func != NULL)
					(*hash_table->key_destroy_func) (cur_key);
				if (hash_table->value_destroy_func != NULL)
					(*hash_table->value_destroy_func) (value);

				check_table_size (hash_table);
				return value;
			}
			i = (i + 1) & table_mask;
		}
	} else {
		GEqualFunc equal = hash_table->equal_func;
		for (;;) {
			gpointer cur_key = table->keys [i];
			if (!cur_key) {
				return NULL; /*key not found*/
			}

			if (!key_is_tombstone (hash_table, cur_key) && equal (key, cur_key)) {
				gpointer value = table->values [i];
				table->values [i] = NULL;
				mono_memory_barrier ();
				set_key_to_tombstone (table, i);

				if (hash_table->key_destroy_func != NULL)
					(*hash_table->key_destroy_func) (cur_key);
				if (hash_table->value_destroy_func != NULL)
					(*hash_table->value_destroy_func) (value);

				check_table_size (hash_table);
				return value;
			}

			i = (i + 1) & table_mask;
		}
	}
}
