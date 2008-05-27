/*
 * mono-value-hash.h: A hash table which only stores values in the hash nodes.
 *
 * Author:
 *   Mark Probst (mark.probst@gmail.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2008 Novell, Inc.
 *
 */
#ifndef __MONO_UTILS_MONO_VALUE_HASH__
#define __MONO_UTILS_MONO_VALUE_HASH__

#include <glib.h>
#include "mono-compiler.h"

G_BEGIN_DECLS

/*
 * This is a hash table with the following features/restrictions:
 * - Keys are not stored in the table, instead a function must be supplied which 
 *   computes them from the value.
 * - Values are assumed to be normal pointers, i.e. their lowest 2-3 bits should be
 *   zero.
 * - NULL values are not allowed.
 * - It uses internal probing instead of chaining.
 * - The above restrictions mean that this hash table will be somewhat slower than
 *   hash tables which store the key (or even the key hash) in the hash nodes. But
 *   it also means that each hash node has a size of one machine word, instead of
 * 4 in GHashTable.
 * - Removal of entries is not supported, as it is not needed by the runtime right 
 *   now.
 */

typedef struct _MonoValueHashTable MonoValueHashTable;

typedef gpointer (*MonoValueHashKeyExtractFunc) (gpointer value);

MonoValueHashTable* mono_value_hash_table_new (GHashFunc hash_func,
											   GEqualFunc key_equal_func,
											   MonoValueHashKeyExtractFunc key_extract) MONO_INTERNAL;

void
mono_value_hash_table_destroy (MonoValueHashTable *table) MONO_INTERNAL;

gpointer
mono_value_hash_table_lookup (MonoValueHashTable *table, gconstpointer key) MONO_INTERNAL;

/* The key pointer is actually only passed here to check a debugging
   assertion and to make the API look more familiar. */
void
mono_value_hash_table_insert (MonoValueHashTable *table,
				 gpointer key, gpointer value) MONO_INTERNAL;

G_END_DECLS

#endif
