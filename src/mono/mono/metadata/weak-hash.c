/* Based on mono-hash.c. */

/*
 * This is similar to MonoGHashTable, but it doesn't keep the keys/values alive.
 * Instead, keys/values are stored in object[] arrays kept alive by a object[2]
 * which is referenced by a weak ref.
 */

#include <config.h>
#include <stdio.h>
#include <math.h>
#include <glib.h>

#include "weak-hash.h"
#include "metadata/gc-internals.h"

#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/unlocked.h>

struct _MonoWeakHashTable {
	GHashFunc      hash_func;
	GEqualFunc     key_equal_func;

	/* Only set if the keys/values are not GC tracked */
	MonoObject **keys;
	MonoObject **values;
	int   table_size;
	int   in_use;
	MonoGHashGCType gc_type;
	// Weak handle to a object[2]
	MonoGCHandle key_value_handle;
};

#define HASH_TABLE_MAX_LOAD_FACTOR 0.7f
/* We didn't really do compaction before, keep it lenient for now */
#define HASH_TABLE_MIN_LOAD_FACTOR 0.05f
/* We triple the table size at rehash time, similar with previous implementation */
#define HASH_TABLE_RESIZE_RATIO 3

static MonoArray*
get_keys (MonoWeakHashTable *hash)
{
	// FIXME: Do it in the caller
	MonoArray *holder = (MonoArray*)mono_gchandle_get_target_internal (hash->key_value_handle);
	/* This is expected to be alive */
	g_assert (holder);
	return mono_array_get_fast (holder, MonoArray*, 0);
}

static MonoArray*
get_values (MonoWeakHashTable *hash)
{
	MonoArray *holder = (MonoArray*)mono_gchandle_get_target_internal (hash->key_value_handle);
	/* This is expected to be alive */
	g_assert (holder);
	return mono_array_get_fast (holder, MonoArray*, 1);
}

static void
key_store (MonoWeakHashTable *hash, int slot, MonoObject* key)
{
	if (hash->gc_type & MONO_HASH_KEY_GC) {
		MonoArray *keys = get_keys (hash);
		mono_array_setref_fast (keys, slot, key);
	} else {
		hash->keys [slot] = key;
	}
}

static void
value_store (MonoWeakHashTable *hash, int slot, MonoObject* value)
{
	if (hash->gc_type & MONO_HASH_VALUE_GC) {
		MonoArray *values = get_values (hash);
		mono_array_setref_fast (values, slot, value);
	} else {
		hash->values [slot] = value;
	}
}

/* Returns position of key or of an empty slot for it */
static int
mono_weak_hash_table_find_slot (MonoWeakHashTable *hash, const MonoObject *key)
{
	guint start = ((*hash->hash_func) (key)) % hash->table_size;
	guint i = start;

	if (hash->gc_type & MONO_HASH_KEY_GC) {
		MonoArray *keys = get_keys (hash);

		if (hash->key_equal_func) {
			GEqualFunc equal = hash->key_equal_func;

			while (TRUE) {
				MonoObject *key2 = mono_array_get_fast (keys, MonoObject*, i);
				if (!(key2 && !(*equal) (key2, key)))
					break;
				i++;
				if (i == hash->table_size)
					i = 0;
			}
		} else {
			while (TRUE) {
				MonoObject *key2 = mono_array_get_fast (keys, MonoObject*, i);
				if (!(key2 && key2 != key))
					break;
				i++;
				if (i == hash->table_size)
					i = 0;
			}
		}
	} else {
		if (hash->key_equal_func) {
			GEqualFunc equal = hash->key_equal_func;

			while (hash->keys [i] && !(*equal) (hash->keys [i], key)) {
				i++;
				if (i == hash->table_size)
					i = 0;
			}
		} else {
			while (hash->keys [i] && hash->keys [i] != key) {
				i++;
				if (i == hash->table_size)
					i = 0;
			}
		}
	}

	return i;
}

MonoWeakHashTable *
mono_weak_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type, MonoGCHandle key_value_handle)
{
	MONO_REQ_GC_UNSAFE_MODE;
	MonoWeakHashTable *hash;
	ERROR_DECL (error);
	MonoArray *holder;

	if (!hash_func)
		hash_func = g_direct_hash;

	hash = g_new0 (MonoWeakHashTable, 1);

	hash->hash_func = hash_func;
	hash->key_equal_func = key_equal_func;
	hash->table_size = g_spaced_primes_closest (1);
	hash->gc_type = type;
	hash->key_value_handle = key_value_handle;

	g_assert (type <= MONO_HASH_KEY_VALUE_GC);

	holder = (MonoArray*)mono_gchandle_get_target_internal (key_value_handle);
	g_assert (holder);

	if (hash->gc_type & MONO_HASH_KEY_GC) {
		MonoArray *keys = mono_array_new_checked (mono_get_object_class (), hash->table_size, error);
		mono_error_assert_ok (error);
		mono_array_setref_fast (holder, 0, keys);
	} else {
		hash->keys = g_new0 (MonoObject*, hash->table_size);
	}
	if (hash->gc_type & MONO_HASH_VALUE_GC) {
		MonoArray *values = mono_array_new_checked (mono_get_object_class (), hash->table_size, error);
		mono_error_assert_ok (error);
		mono_array_setref_fast (holder, 1, values);
	} else {
		hash->values = g_new0 (MonoObject*, hash->table_size);
	}

	return hash;
}

typedef struct {
	MonoWeakHashTable *hash;
	int new_size;
	MonoObject **keys;
	MonoObject **values;
	MonoArray *keys_arr;
	MonoArray *values_arr;
} RehashData;

static void*
do_rehash (void *_data)
{
	RehashData *data = (RehashData *)_data;
	MonoWeakHashTable *hash = data->hash;
	int current_size, i;
	MonoObject **old_keys = NULL;
	MonoArray *old_values_arr = NULL;

	MonoArray *holder = (MonoArray*)mono_gchandle_get_target_internal (hash->key_value_handle);
	g_assert (holder);

	current_size = hash->table_size;
	hash->table_size = data->new_size;

	if (hash->gc_type & MONO_HASH_VALUE_GC) {
		old_keys = hash->keys;
		hash->keys = data->keys;

		old_values_arr = get_values (hash);
		mono_array_setref_fast (holder, 1, data->values_arr);

		for (i = 0; i < current_size; i++) {
			if (old_keys [i]) {
				int slot = mono_weak_hash_table_find_slot (hash, old_keys [i]);
				key_store (hash, slot, old_keys [i]);
				value_store (hash, slot, mono_array_get_fast (old_values_arr, MonoObject*, i));
			}
		}
	} else {
		// FIXME:
		g_assert_not_reached ();
	}
	return NULL;
}

static void
rehash (MonoWeakHashTable *hash)
{
	MONO_REQ_GC_UNSAFE_MODE; //we must run in unsafe mode to make rehash safe

	RehashData data;
	void *old_keys = hash->keys;
	void *old_values = hash->values;
	ERROR_DECL (error);

	memset (&data, 0, sizeof (RehashData));

	data.hash = hash;
	/*
	 * Rehash to a size that can fit the current elements. Rehash relative to in_use
	 * to allow also for compaction.
	 */
	data.new_size = g_spaced_primes_closest (GFLOAT_TO_UINT (hash->in_use / HASH_TABLE_MAX_LOAD_FACTOR * HASH_TABLE_RESIZE_RATIO));

	MonoArray *holder = (MonoArray*)mono_gchandle_get_target_internal (hash->key_value_handle);
	g_assert (holder);

	if (hash->gc_type & MONO_HASH_KEY_GC) {
		MonoArray *keys = mono_array_new_checked (mono_get_object_class (), data.new_size, error);
		mono_error_assert_ok (error);
		data.keys_arr = keys;
	} else {
		data.keys = g_new0 (MonoObject*, data.new_size);
	}
	if (hash->gc_type & MONO_HASH_VALUE_GC) {
		MonoArray *values = mono_array_new_checked (mono_get_object_class (), data.new_size, error);
		mono_error_assert_ok (error);
		data.values_arr = values;
	} else {
		data.values = g_new0 (MonoObject*, data.new_size);
	}

	if (!mono_threads_are_safepoints_enabled ()) {
		// FIXME: Does this work ?
		g_assert_not_reached ();
		mono_gc_invoke_with_gc_lock (do_rehash, &data);
	} else {
		/* We cannot be preempted */
		do_rehash (&data);
	}

	if (!(hash->gc_type & MONO_HASH_KEY_GC))
		g_free (old_keys);
	if (!(hash->gc_type & MONO_HASH_VALUE_GC))
		g_free (old_values);
}

/**
 * mono_weak_hash_table_size:
 */
guint
mono_weak_hash_table_size (MonoWeakHashTable *hash)
{
	g_assert (hash);

	return hash->in_use;
}

/**
 * mono_weak_hash_table_lookup:
 */
gpointer
mono_weak_hash_table_lookup (MonoWeakHashTable *hash, gconstpointer key)
{
	g_assert (hash);

	int slot = mono_weak_hash_table_find_slot (hash, (MonoObject*)key);

	// FIXME:
	g_assert (hash->gc_type == MONO_HASH_VALUE_GC);

	MonoArray* values = get_values (hash);

	if (hash->keys [slot])
		return mono_array_get_fast (values, MonoObject*, slot);
	else
		return NULL;
}

/**
 * mono_weak_hash_table_destroy:
 */
void
mono_weak_hash_table_destroy (MonoWeakHashTable *hash)
{
	g_assert (hash);

	if (!(hash->gc_type & MONO_HASH_KEY_GC))
		g_free (hash->keys);
	if (!(hash->gc_type & MONO_HASH_VALUE_GC))
		g_free (hash->values);
	g_free (hash);
}

static void
mono_weak_hash_table_insert_replace (MonoWeakHashTable *hash, gpointer key, gpointer value, gboolean replace)
{
	MONO_REQ_GC_UNSAFE_MODE;
	int slot;

	g_assert (hash);

	if (hash->in_use > (hash->table_size * HASH_TABLE_MAX_LOAD_FACTOR))
		rehash (hash);

	slot = mono_weak_hash_table_find_slot (hash, (MonoObject*)key);

	// FIXME:
	g_assert (hash->gc_type == MONO_HASH_VALUE_GC);

	if (hash->keys [slot]) {
		if (replace) {
			key_store (hash, slot, (MonoObject*)key);
		}
		value_store (hash, slot, (MonoObject*)value);
	} else {
		key_store (hash, slot, (MonoObject*)key);
		value_store (hash, slot, (MonoObject*)value);
		hash->in_use++;
	}
}

void
mono_weak_hash_table_insert (MonoWeakHashTable *h, gpointer k, gpointer v)
{
	MONO_REQ_GC_UNSAFE_MODE;
	mono_weak_hash_table_insert_replace (h, k, v, FALSE);
}
