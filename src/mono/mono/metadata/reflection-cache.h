/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_REFLECTION_CACHE_H__
#define __MONO_METADATA_REFLECTION_CACHE_H__

#include <glib.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/mono-hash.h>
#include <mono/metadata/mempool.h>
#include <mono/utils/mono-error-internals.h>

/*
 * We need to return always the same object for MethodInfo, FieldInfo etc..
 * but we need to consider the reflected type.
 * type uses a different hash, since it uses custom hash/equal functions.
 */

typedef struct {
	gpointer item;
	MonoClass *refclass;
} ReflectedEntry;

gboolean
reflected_equal (gconstpointer a, gconstpointer b);

guint
reflected_hash (gconstpointer a);

static inline ReflectedEntry*
alloc_reflected_entry (MonoDomain *domain)
{
	if (!mono_gc_is_moving ())
		return g_new0 (ReflectedEntry, 1);
	else
		return (ReflectedEntry *)mono_mempool_alloc (domain->mp, sizeof (ReflectedEntry));
}

static void
free_reflected_entry (ReflectedEntry *entry)
{
	if (!mono_gc_is_moving ())
		g_free (entry);
}

static inline MonoObject*
cache_object (MonoDomain *domain, MonoClass *klass, gpointer item, MonoObject* o)
{
	MonoObject *obj;
	ReflectedEntry pe;
	pe.item = item;
	pe.refclass = klass;

	mono_domain_lock (domain);
	if (!domain->refobject_hash)
		domain->refobject_hash = mono_conc_g_hash_table_new_type (reflected_hash, reflected_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Object Table");

	obj = (MonoObject*) mono_conc_g_hash_table_lookup (domain->refobject_hash, &pe);
	if (obj == NULL) {
		ReflectedEntry *e = alloc_reflected_entry (domain);
		e->item = item;
		e->refclass = klass;
		mono_conc_g_hash_table_insert (domain->refobject_hash, e, o);
		obj = o;
	}
	mono_domain_unlock (domain);
	return obj;
}


static inline MonoObjectHandle
cache_object_handle (MonoDomain *domain, MonoClass *klass, gpointer item, MonoObjectHandle o)
{
	ReflectedEntry pe;
	pe.item = item;
	pe.refclass = klass;

	mono_domain_lock (domain);
	if (!domain->refobject_hash)
		domain->refobject_hash = mono_conc_g_hash_table_new_type (reflected_hash, reflected_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, domain, "Domain Reflection Object Table");

	MonoObjectHandle obj = MONO_HANDLE_NEW (MonoObject, mono_conc_g_hash_table_lookup (domain->refobject_hash, &pe));
	if (MONO_HANDLE_IS_NULL (obj)) {
		ReflectedEntry *e = alloc_reflected_entry (domain);
		e->item = item;
		e->refclass = klass;
		mono_conc_g_hash_table_insert (domain->refobject_hash, e, MONO_HANDLE_RAW (o));
		MONO_HANDLE_ASSIGN (obj, o);
	}
	mono_domain_unlock (domain);
	return obj;
}

#define CACHE_OBJECT(t,p,o,k) ((t) (cache_object (domain, (k), (p), (o))))
#define CACHE_OBJECT_HANDLE(t,p,o,k) ((t) (cache_object_handle (domain, (k), (p), (o))))

static inline MonoObjectHandle
check_object_handle (MonoDomain* domain, MonoClass *klass, gpointer item)
{
	ReflectedEntry e;
	e.item = item;
	e.refclass = klass;
	MonoConcGHashTable *hash = domain->refobject_hash;
	if (!hash)
		return MONO_HANDLE_NEW (MonoObject, NULL);

	MonoObjectHandle obj = MONO_HANDLE_NEW (MonoObject, mono_conc_g_hash_table_lookup (hash, &e));
	return obj;
}


typedef MonoObjectHandle (*ReflectionCacheConstructFunc_handle) (MonoDomain*, MonoClass*, gpointer, gpointer, MonoError *);

static inline MonoObjectHandle
check_or_construct_handle (MonoDomain *domain, MonoClass *klass, gpointer item, gpointer user_data, MonoError *error, ReflectionCacheConstructFunc_handle construct)
{
	error_init (error);
	MonoObjectHandle obj = check_object_handle (domain, klass, item);
	if (!MONO_HANDLE_IS_NULL (obj))
		return obj;
	MONO_HANDLE_ASSIGN (obj, construct (domain, klass, item, user_data, error));
	return_val_if_nok (error, NULL);
	if (MONO_HANDLE_IS_NULL (obj))
		return obj;
	/* note no caching if there was an error in construction */
	return cache_object_handle (domain, klass, item, obj);
}


#define CHECK_OR_CONSTRUCT_HANDLE(t,p,k,construct,ud) ((t) check_or_construct_handle (domain, (k), (p), (ud), error, (ReflectionCacheConstructFunc_handle) (construct)))


#endif /*__MONO_METADATA_REFLECTION_CACHE_H__*/
