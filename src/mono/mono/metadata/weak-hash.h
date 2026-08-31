/* Based on mono-hash.h */
#ifndef __MONO_WEAK_HASH_H__
#define __MONO_WEAK_HASH_H__

#include <mono/metadata/mono-gc.h>
#include <mono/metadata/mono-hash.h>

typedef struct _MonoWeakHashTable MonoWeakHashTable;

MonoWeakHashTable *
mono_weak_hash_table_new (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type, MonoGCHandle key_value_handle);
guint    mono_weak_hash_table_size            (MonoWeakHashTable *hash);
gpointer mono_weak_hash_table_lookup          (MonoWeakHashTable *hash, gconstpointer key);
gboolean mono_weak_hash_table_lookup_extended (MonoWeakHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value);
void     mono_weak_hash_table_foreach         (MonoWeakHashTable *hash, GHFunc func, gpointer user_data);
gpointer mono_weak_hash_table_find            (MonoWeakHashTable *hash, GHRFunc predicate, gpointer user_data);
gboolean mono_weak_hash_table_remove          (MonoWeakHashTable *hash, gconstpointer key);
guint    mono_weak_hash_table_foreach_remove  (MonoWeakHashTable *hash, GHRFunc func, gpointer user_data);
void     mono_weak_hash_table_destroy         (MonoWeakHashTable *hash);
void     mono_weak_hash_table_replace         (MonoWeakHashTable *h, gpointer k, gpointer v);
void     mono_weak_hash_table_insert          (MonoWeakHashTable *h, gpointer k, gpointer v);

#endif /* __MONO_WEAK_HASH_H__ */
