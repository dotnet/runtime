#ifndef __MONO_SGENHASHTABLE_H__
#define __MONO_SGENHASHTABLE_H__

#include "config.h"

#ifdef HAVE_SGEN_GC

#include <glib.h>

/* hash tables */

typedef struct _SgenHashTableEntry SgenHashTableEntry;
struct _SgenHashTableEntry {
	SgenHashTableEntry *next;
	gpointer key;
	char data [MONO_ZERO_LEN_ARRAY]; /* data is pointer-aligned */
};

typedef struct {
	int table_mem_type;
	int entry_mem_type;
	size_t data_size;
	GHashFunc hash_func;
	GEqualFunc equal_func;
	SgenHashTableEntry **table;
	guint size;
	guint num_entries;
} SgenHashTable;

#define SGEN_HASH_TABLE_INIT(table_type,entry_type,data_size,hash_func,equal_func)	{ (table_type), (entry_type), (data_size), (hash_func), (equal_func), NULL, 0, 0 }
#define SGEN_HASH_TABLE_ENTRY_SIZE(data_size)			((data_size) + sizeof (SgenHashTableEntry*) + sizeof (gpointer))

gpointer sgen_hash_table_lookup (SgenHashTable *table, gpointer key) MONO_INTERNAL;
gboolean sgen_hash_table_replace (SgenHashTable *table, gpointer key, gpointer new_value, gpointer old_value) MONO_INTERNAL;
gboolean sgen_hash_table_set_value (SgenHashTable *table, gpointer key, gpointer new_value, gpointer old_value) MONO_INTERNAL;
gboolean sgen_hash_table_set_key (SgenHashTable *hash_table, gpointer old_key, gpointer new_key) MONO_INTERNAL;
gboolean sgen_hash_table_remove (SgenHashTable *table, gpointer key, gpointer data_return) MONO_INTERNAL;

void sgen_hash_table_clean (SgenHashTable *table) MONO_INTERNAL;

#define sgen_hash_table_num_entries(h)	((h)->num_entries)

#define SGEN_HASH_TABLE_FOREACH(h,k,v) do {				\
		SgenHashTable *__hash_table = (h);			\
		SgenHashTableEntry **__table = __hash_table->table;	\
		guint __i;						\
		for (__i = 0; __i < (h)->size; ++__i) {			\
			SgenHashTableEntry **__iter, **__next;			\
			for (__iter = &__table [__i]; *__iter; __iter = __next) {	\
				SgenHashTableEntry *__entry = *__iter;	\
				__next = &__entry->next;	\
				(k) = __entry->key;			\
				(v) = (gpointer)__entry->data;

/* The loop must be continue'd after using this! */
#define SGEN_HASH_TABLE_FOREACH_REMOVE(free)	do {			\
		*__iter = *__next;	\
		__next = __iter;	\
		--__hash_table->num_entries;				\
		if ((free))						\
			sgen_free_internal (__entry, __hash_table->entry_mem_type); \
	} while (0)

#define SGEN_HASH_TABLE_FOREACH_SET_KEY(k)	((__entry)->key = (k))

#define SGEN_HASH_TABLE_FOREACH_END					\
			}						\
		}							\
	} while (0)

#endif

#endif
