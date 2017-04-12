/**
 * \file
 * A mostly concurrent hashtable
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2014 Xamarin
 */

#ifndef __MONO_CONCURRENT_HASHTABLE_H__
#define __MONO_CONCURRENT_HASHTABLE_H__

#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-os-mutex.h>
#include <glib.h>

typedef struct _MonoConcurrentHashTable MonoConcurrentHashTable;

MONO_API MonoConcurrentHashTable* mono_conc_hashtable_new (GHashFunc hash_func, GEqualFunc key_equal_func);
MONO_API MonoConcurrentHashTable* mono_conc_hashtable_new_full (GHashFunc hash_func, GEqualFunc key_equal_func, GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func);
MONO_API void mono_conc_hashtable_destroy (MonoConcurrentHashTable *hash_table);
MONO_API gpointer mono_conc_hashtable_lookup (MonoConcurrentHashTable *hash_table, gpointer key);
MONO_API gpointer mono_conc_hashtable_insert (MonoConcurrentHashTable *hash_table, gpointer key, gpointer value);
MONO_API gpointer mono_conc_hashtable_remove (MonoConcurrentHashTable *hash_table, gpointer key);
MONO_API void mono_conc_hashtable_foreach (MonoConcurrentHashTable *hashtable, GHFunc func, gpointer userdata);
MONO_API void mono_conc_hashtable_foreach_steal (MonoConcurrentHashTable *hashtable, GHRFunc func, gpointer userdata);

#endif
