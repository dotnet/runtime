/**
 * \file
 * Hash table for (object, property) pairs
 *
 * Author:
 *	Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2008 Novell, Inc
 */

#include <config.h>
#include "mono-property-hash.h"

struct _MonoPropertyHash {
	/* We use one hash table per property */
	GHashTable *hashes;
};

MonoPropertyHash*
mono_property_hash_new (void)
{
	MonoPropertyHash *hash = g_new0 (MonoPropertyHash, 1);

	hash->hashes = g_hash_table_new (NULL, NULL);

	return hash;
}

static void
free_hash (gpointer key, gpointer value, gpointer user_data)
{
	GHashTable *hash = (GHashTable*)value;

	g_hash_table_destroy (hash);
}

void
mono_property_hash_destroy (MonoPropertyHash *hash)
{
	g_hash_table_foreach (hash->hashes, free_hash, NULL);
	g_hash_table_destroy (hash->hashes);

	g_free (hash);
}

void
mono_property_hash_insert (MonoPropertyHash *hash, gpointer object, guint32 property,
						   gpointer value)
{
	GHashTable *prop_hash;

	prop_hash = (GHashTable *) g_hash_table_lookup (hash->hashes, GUINT_TO_POINTER (property));
	if (!prop_hash) {
		// FIXME: Maybe use aligned_hash
		prop_hash = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (hash->hashes, GUINT_TO_POINTER (property), prop_hash);
	}

	g_hash_table_insert (prop_hash, object, value);
}

static void
remove_object (gpointer key, gpointer value, gpointer user_data)
{
	GHashTable *prop_hash = (GHashTable*)value;

	g_hash_table_remove (prop_hash, user_data);
}

void
mono_property_hash_remove_object (MonoPropertyHash *hash, gpointer object)
{
	g_hash_table_foreach (hash->hashes, remove_object, object);
}

gpointer
mono_property_hash_lookup (MonoPropertyHash *hash, gpointer object, guint32 property)
{
	GHashTable *prop_hash;

	prop_hash = (GHashTable *) g_hash_table_lookup (hash->hashes, GUINT_TO_POINTER (property));
	if (!prop_hash)
		return NULL;
	return g_hash_table_lookup (prop_hash, object);
}

