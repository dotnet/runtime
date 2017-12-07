/**
 * \file
 * GC-aware concurrent hashtable, based on utils/mono-conc-hashtable
 */

#ifndef __MONO_CONC_G_HASH_H__
#define __MONO_CONC_G_HASH_H__

#include <mono/metadata/mono-hash.h>


typedef struct _MonoConcGHashTable MonoConcGHashTable;

MonoConcGHashTable * mono_conc_g_hash_table_new_type (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type, MonoGCRootSource source, void *key, const char *msg);
gpointer mono_conc_g_hash_table_lookup (MonoConcGHashTable *hash, gconstpointer key);
gboolean mono_conc_g_hash_table_lookup_extended (MonoConcGHashTable *hash, gconstpointer key, gpointer *orig_key, gpointer *value);
void mono_conc_g_hash_table_foreach (MonoConcGHashTable *hash, GHFunc func, gpointer user_data);
void mono_conc_g_hash_table_destroy (MonoConcGHashTable *hash);
gpointer mono_conc_g_hash_table_insert (MonoConcGHashTable *h, gpointer k, gpointer v);
gpointer mono_conc_g_hash_table_remove (MonoConcGHashTable *hash, gconstpointer key);

#endif /* __MONO_CONC_G_HASH_H__ */
