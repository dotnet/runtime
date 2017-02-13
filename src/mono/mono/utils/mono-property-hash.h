/**
 * \file
 * Hash table for (object, property) pairs
 *
 * Author:
 *	Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2008 Novell, Inc
 */

/*
 * This is similar to the GLib hash table, but stores (object, property) pairs. It can
 * be used to store rarely used fields of runtime structures, decreasing memory usage.
 * The memory required to store one property is the size of one hash node, about 3
 * pointers.
 */

#ifndef _MONO_PROPERTY_HASH_H_
#define _MONO_PROPERTY_HASH_H_

#include <glib.h>
#include <mono/utils/mono-publib.h>

G_BEGIN_DECLS

typedef struct _MonoPropertyHash MonoPropertyHash;

MONO_API MonoPropertyHash* mono_property_hash_new (void);

MONO_API void mono_property_hash_destroy (MonoPropertyHash *hash);

MONO_API void mono_property_hash_insert (MonoPropertyHash *hash, gpointer object, guint32 property,
								gpointer value);

/* Remove all properties of OBJECT */
MONO_API void mono_property_hash_remove_object (MonoPropertyHash *hash, gpointer object);

MONO_API gpointer mono_property_hash_lookup (MonoPropertyHash *hash, gpointer object, guint32 property);

G_END_DECLS

#endif
