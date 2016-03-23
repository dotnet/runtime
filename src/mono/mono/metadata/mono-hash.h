/*
 * mono-hash.c: GC-aware hashtable, based on Eglib's Hashtable
 *
 * Authors:
 *   Paolo Molaro (lupus@xamarin.com)
 *
 * Copyright 2013 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_G_HASH_H__
#define __MONO_G_HASH_H__

#include <mono/metadata/mono-gc.h>

MONO_BEGIN_DECLS
/* do not change the values of this enum */
typedef enum {
	MONO_HASH_KEY_GC = 1,
	MONO_HASH_VALUE_GC = 2,
	MONO_HASH_KEY_VALUE_GC = MONO_HASH_KEY_GC | MONO_HASH_VALUE_GC,
} MonoGHashGCType;

typedef struct _MonoGHashTable MonoGHashTable;

MONO_API MonoGHashTable *mono_g_hash_table_new_type (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type, MonoGCRootSource source, const char *msg);
MONO_API guint    mono_g_hash_table_size            (MonoGHashTable *hash);
MONO_API gpointer mono_g_hash_table_lookup          (MonoGHashTable *hash, gconstpointer key);
MONO_API gboolean mono_g_hash_table_lookup_extended (MonoGHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value);
MONO_API void     mono_g_hash_table_foreach         (MonoGHashTable *hash, GHFunc func, gpointer user_data);
MONO_API gpointer mono_g_hash_table_find            (MonoGHashTable *hash, GHRFunc predicate, gpointer user_data);
MONO_API gboolean mono_g_hash_table_remove          (MonoGHashTable *hash, gconstpointer key);
MONO_API guint    mono_g_hash_table_foreach_remove  (MonoGHashTable *hash, GHRFunc func, gpointer user_data);
MONO_API void     mono_g_hash_table_destroy         (MonoGHashTable *hash);
MONO_API void     mono_g_hash_table_insert          (MonoGHashTable *h, gpointer k, gpointer v);
MONO_API void     mono_g_hash_table_replace         (MonoGHashTable *h, gpointer k, gpointer v);
MONO_API void     mono_g_hash_table_print_stats     (MonoGHashTable *table);

MONO_END_DECLS
#endif /* __MONO_G_HASH_H__ */
