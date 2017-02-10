/**
 * \file
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

#ifdef HAVE_SGEN_GC

#include <string.h>

#include <mono/sgen/sgen-gc.h>
#include <mono/sgen/sgen-hash-table.h>

#ifdef HEAVY_STATISTICS
static guint64 stat_lookups;
static guint64 stat_lookup_iterations;
static guint64 stat_lookup_max_iterations;
#endif

static void
rehash (SgenHashTable *hash_table)
{
	SgenHashTableEntry **old_hash = hash_table->table;
	guint old_hash_size = hash_table->size;
	guint i, hash, new_size;
	SgenHashTableEntry **new_hash;
	SgenHashTableEntry *entry, *next;

	if (!old_hash) {
		sgen_register_fixed_internal_mem_type (hash_table->entry_mem_type,
				sizeof (SgenHashTableEntry*) + sizeof (gpointer) + hash_table->data_size);
		new_size = 13;
	} else {
		new_size = g_spaced_primes_closest (hash_table->num_entries);
	}

	new_hash = (SgenHashTableEntry **)sgen_alloc_internal_dynamic (new_size * sizeof (SgenHashTableEntry*), hash_table->table_mem_type, TRUE);
	for (i = 0; i < old_hash_size; ++i) {
		for (entry = old_hash [i]; entry; entry = next) {
			hash = hash_table->hash_func (entry->key) % new_size;
			next = entry->next;
			entry->next = new_hash [hash];
			new_hash [hash] = entry;
		}
	}
	sgen_free_internal_dynamic (old_hash, old_hash_size * sizeof (SgenHashTableEntry*), hash_table->table_mem_type);
	hash_table->table = new_hash;
	hash_table->size = new_size;
}

static void
rehash_if_necessary (SgenHashTable *hash_table)
{
	if (hash_table->num_entries >= hash_table->size * 2)
		rehash (hash_table);

	SGEN_ASSERT (1, hash_table->size, "rehash guarantees size > 0");
}

static SgenHashTableEntry*
lookup (SgenHashTable *hash_table, gpointer key, guint *_hash)
{
	SgenHashTableEntry *entry;
	guint hash;
	GEqualFunc equal = hash_table->equal_func;
#ifdef HEAVY_STATISTICS
	guint64 iterations = 0;
	++stat_lookups;
#endif

	if (!hash_table->size)
		return NULL;

	hash = hash_table->hash_func (key) % hash_table->size;
	if (_hash)
		*_hash = hash;

	for (entry = hash_table->table [hash]; entry; entry = entry->next) {
#ifdef HEAVY_STATISTICS
		++stat_lookup_iterations;
		++iterations;
		if (iterations > stat_lookup_max_iterations)
			stat_lookup_max_iterations = iterations;
#endif
		if ((equal && equal (entry->key, key)) || (!equal && entry->key == key))
			return entry;
	}
	return NULL;
}

gpointer
sgen_hash_table_lookup (SgenHashTable *hash_table, gpointer key)
{
	SgenHashTableEntry *entry = lookup (hash_table, key, NULL);
	if (!entry)
		return NULL;
	return entry->data;
}

gboolean
sgen_hash_table_replace (SgenHashTable *hash_table, gpointer key, gpointer new_value, gpointer old_value)
{
	guint hash;
	SgenHashTableEntry *entry;

	rehash_if_necessary (hash_table);
	entry = lookup (hash_table, key, &hash);

	if (entry) {
		if (old_value)
			memcpy (old_value, entry->data, hash_table->data_size);	
		memcpy (entry->data, new_value, hash_table->data_size);
		return FALSE;
	}

	entry = (SgenHashTableEntry *)sgen_alloc_internal (hash_table->entry_mem_type);
	entry->key = key;
	memcpy (entry->data, new_value, hash_table->data_size);

	entry->next = hash_table->table [hash];
	hash_table->table [hash] = entry;

	hash_table->num_entries++;

	return TRUE;
}

gboolean
sgen_hash_table_set_value (SgenHashTable *hash_table, gpointer key, gpointer new_value, gpointer old_value)
{
	guint hash;
	SgenHashTableEntry *entry;

	entry = lookup (hash_table, key, &hash);

	if (entry) {
		if (old_value)
			memcpy (old_value, entry->data, hash_table->data_size);
		memcpy (entry->data, new_value, hash_table->data_size);
		return TRUE;
	}

	return FALSE;
}

gboolean
sgen_hash_table_set_key (SgenHashTable *hash_table, gpointer old_key, gpointer new_key)
{
	guint hash;
	SgenHashTableEntry *entry;

	entry = lookup (hash_table, old_key, &hash);

	if (entry) {
		entry->key = new_key;
		return TRUE;
	}

	return FALSE;
}

gboolean
sgen_hash_table_remove (SgenHashTable *hash_table, gpointer key, gpointer data_return)
{
	SgenHashTableEntry *entry, *prev;
	guint hash;
	GEqualFunc equal = hash_table->equal_func;

	rehash_if_necessary (hash_table);
	hash = hash_table->hash_func (key) % hash_table->size;

	prev = NULL;
	for (entry = hash_table->table [hash]; entry; entry = entry->next) {
		if ((equal && equal (entry->key, key)) || (!equal && entry->key == key)) {
			if (prev)
				prev->next = entry->next;
			else
				hash_table->table [hash] = entry->next;

			hash_table->num_entries--;

			if (data_return)
				memcpy (data_return, entry->data, hash_table->data_size);

			sgen_free_internal (entry, hash_table->entry_mem_type);

			return TRUE;
		}
		prev = entry;
	}

	return FALSE;
}

void
sgen_hash_table_clean (SgenHashTable *hash_table)
{
	guint i;

	if (!hash_table->size) {
		SGEN_ASSERT (1, !hash_table->table, "clean should reset hash_table->table");
		SGEN_ASSERT (1, !hash_table->num_entries, "clean should reset hash_table->num_entries");
		return;
	}

	for (i = 0; i < hash_table->size; ++i) {
		SgenHashTableEntry *entry = hash_table->table [i];
		while (entry) {
			SgenHashTableEntry *next = entry->next;
			sgen_free_internal (entry, hash_table->entry_mem_type);
			entry = next;
		}
	}

	sgen_free_internal_dynamic (hash_table->table, hash_table->size * sizeof (SgenHashTableEntry*), hash_table->table_mem_type);

	hash_table->table = NULL;
	hash_table->size = 0;
	hash_table->num_entries = 0;
}

void
sgen_init_hash_table (void)
{
#ifdef HEAVY_STATISTICS
	mono_counters_register ("Hash table lookups", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_lookups);
	mono_counters_register ("Hash table lookup iterations", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_lookup_iterations);
	mono_counters_register ("Hash table lookup max iterations", MONO_COUNTER_GC | MONO_COUNTER_ULONG, &stat_lookup_max_iterations);
#endif
}

#endif
