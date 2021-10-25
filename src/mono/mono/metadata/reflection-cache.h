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
mono_reflected_equal (gconstpointer a, gconstpointer b);

guint
mono_reflected_hash (gconstpointer a);

static inline ReflectedEntry*
alloc_reflected_entry (MonoMemoryManager *mem_manager)
{
	if (!mono_gc_is_moving ())
		return g_new0 (ReflectedEntry, 1);
	return (ReflectedEntry *)mono_mem_manager_alloc0 (mem_manager, sizeof (ReflectedEntry));
}

static inline void
free_reflected_entry (ReflectedEntry *entry)
{
	if (!mono_gc_is_moving ())
		g_free (entry);
}

static inline MonoObject*
cache_object (MonoMemoryManager *mem_manager, MonoClass *klass, gpointer item, MonoObject* o)
{
	MonoObject *obj;
	ReflectedEntry pe;
	pe.item = item;
	pe.refclass = klass;

	mono_mem_manager_lock (mem_manager);
	obj = (MonoObject *)mono_conc_g_hash_table_lookup (mem_manager->refobject_hash, &pe);
	if (obj == NULL) {
		ReflectedEntry *e = alloc_reflected_entry (mem_manager);
		e->item = item;
		e->refclass = klass;
		mono_conc_g_hash_table_insert (mem_manager->refobject_hash, e, o);
		obj = o;
	}
	mono_mem_manager_unlock (mem_manager);
	return obj;
}

static inline MonoObjectHandle
cache_object_handle (MonoMemoryManager *mem_manager, MonoClass *klass, gpointer item, MonoObjectHandle o)
{
	ReflectedEntry pe;
	pe.item = item;
	pe.refclass = klass;

	mono_mem_manager_lock (mem_manager);
	MonoObjectHandle obj = MONO_HANDLE_NEW (MonoObject, (MonoObject *)mono_conc_g_hash_table_lookup (mem_manager->refobject_hash, &pe));
	if (MONO_HANDLE_IS_NULL (obj)) {
		ReflectedEntry *e = alloc_reflected_entry (mem_manager);
		e->item = item;
		e->refclass = klass;
		mono_conc_g_hash_table_insert (mem_manager->refobject_hash, e, MONO_HANDLE_RAW (o));
		MONO_HANDLE_ASSIGN (obj, o);
	}
	mono_mem_manager_unlock (mem_manager);
	return obj;
}

#define CACHE_OBJECT(t,mem_manager,p,o,k) ((t) (cache_object ((mem_manager), (k), (p), (o))))
#define CACHE_OBJECT_HANDLE(t,mem_manager,p,o,k) (MONO_HANDLE_CAST (t, cache_object_handle ((mem_manager), (k), (p), (o))))

static inline MonoObjectHandle
check_object_handle (MonoMemoryManager *mem_manager, MonoClass *klass, gpointer item)
{
	MonoObjectHandle obj_handle;
	ReflectedEntry e;
	e.item = item;
	e.refclass = klass;

	// FIXME: May need a memory manager for item+klass ?

	mono_mem_manager_lock (mem_manager);
	MonoConcGHashTable *hash = mem_manager->refobject_hash;
	obj_handle = MONO_HANDLE_NEW (MonoObject, (MonoObject *)mono_conc_g_hash_table_lookup (hash, &e));
	mono_mem_manager_unlock (mem_manager);

	return obj_handle;
}

typedef MonoObjectHandle (*ReflectionCacheConstructFunc_handle) (MonoClass*, gpointer, gpointer, MonoError *);

static inline MonoObjectHandle
check_or_construct_handle (MonoMemoryManager *mem_manager, MonoClass *klass, gpointer item, gpointer user_data, MonoError *error, ReflectionCacheConstructFunc_handle construct)
{
	error_init (error);
	MonoObjectHandle obj = check_object_handle (mem_manager, klass, item);
	if (!MONO_HANDLE_IS_NULL (obj))
		return obj;
	MONO_HANDLE_ASSIGN (obj, construct (klass, item, user_data, error));
	return_val_if_nok (error, NULL_HANDLE);
	if (MONO_HANDLE_IS_NULL (obj))
		return obj;
	/* note no caching if there was an error in construction */
	return cache_object_handle (mem_manager, klass, item, obj);
}

#define CHECK_OR_CONSTRUCT_HANDLE(type,mem_manager, item,klass,construct,user_data) \
	(MONO_HANDLE_CAST (type, check_or_construct_handle ( \
		(mem_manager), (klass), (item), (user_data), error, (ReflectionCacheConstructFunc_handle) (construct))))

#endif /*__MONO_METADATA_REFLECTION_CACHE_H__*/
