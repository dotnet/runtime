/**
 * \file
 */

#ifndef __MONO_G_HASH_INTERNALS_H__
#define __MONO_G_HASH_INTERNALS_H__

#include "mono/metadata/mono-hash.h"
#include "mono/metadata/mono-gc.h"
#include "mono/utils/mono-compiler.h"

MONO_COMPONENT_API MonoGHashTable *
mono_g_hash_table_new_type_internal (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type, MonoGCRootSource source, void *key, const char *msg);

MONO_COMPONENT_API void 
mono_g_hash_table_insert_internal (MonoGHashTable *h, gpointer k, gpointer v);

#endif /* __MONO_G_HASH_INTERNALS_H__ */
