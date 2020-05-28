/**
 * \file
 * A hash table which uses the values themselves as nodes.
 *
 * Author:
 *   Mark Probst (mark.probst@gmail.com)
 *
 * (C) 2007 Novell, Inc.
 *
 */
#ifndef __MONO_UTILS_MONO_INTERNAL_HASH__
#define __MONO_UTILS_MONO_INTERNAL_HASH__

/* A MonoInternalHashTable is a hash table that does not allocate hash
   nodes.  It can be used if the following conditions are fulfilled:

   * The key is contained (directly or indirectly) in the value.

   * Each value is in at most one internal hash table at the same
     time.

   The value data structure must then be extended to contain a
   pointer, used by the internal hash table to chain values in the
   same bucket.

   Apart from the hash function, two other functions must be provided,
   namely for extracting the key out of a value, and for getting the
   next value pointer.  The latter must actually return a pointer to
   the next value pointer, because the internal hash table must be
   able to modify it.

   See the class_cache internal hash table in MonoImage for an
   example.
*/

typedef struct _MonoInternalHashTable MonoInternalHashTable;

typedef gpointer (*MonoInternalHashKeyExtractFunc) (gpointer value);
typedef gpointer* (*MonoInternalHashNextValueFunc) (gpointer value);
typedef void (*MonoInternalHashApplyFunc) (gpointer value);

struct _MonoInternalHashTable
{
	GHashFunc hash_func;
	MonoInternalHashKeyExtractFunc key_extract;
	MonoInternalHashNextValueFunc next_value;
	gint size;
	gint num_entries;
	gpointer *table;
};

void
mono_internal_hash_table_init (MonoInternalHashTable *table,
			       GHashFunc hash_func,
			       MonoInternalHashKeyExtractFunc key_extract,
			       MonoInternalHashNextValueFunc next_value);

void
mono_internal_hash_table_destroy (MonoInternalHashTable *table);

gpointer
mono_internal_hash_table_lookup (MonoInternalHashTable *table, gpointer key);

/* mono_internal_hash_table_insert requires that there is no entry for
   key in the hash table.  If you want to change the value for a key
   already in the hash table, remove it first and then insert the new
   one.

   The key pointer is actually only passed here to check a debugging
   assertion and to make the API look more familiar. */
void
mono_internal_hash_table_insert (MonoInternalHashTable *table,
				 gpointer key, gpointer value);

void
mono_internal_hash_table_apply (MonoInternalHashTable *table, MonoInternalHashApplyFunc func);

gboolean
mono_internal_hash_table_remove (MonoInternalHashTable *table, gpointer key);

#endif
