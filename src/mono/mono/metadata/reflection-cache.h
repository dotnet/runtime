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
#include <mono/metadata/metadata-update.h>

/*
 * We need to return always the same object for MethodInfo, FieldInfo etc..
 * but we need to consider the reflected type.
 * type uses a different hash, since it uses custom hash/equal functions.
 */

typedef struct {
	gpointer item;
	MonoClass *refclass;
	uint32_t generation; /* 0 is normal; hot reload may change it */
} ReflectedEntry;

enum {
	MONO_REFL_CACHE_DEFAULT = 0,
	MONO_REFL_CACHE_NO_HOT_RELOAD_INVALIDATE = 1,
};

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
cache_object (MonoMemoryManager *mem_manager, int flags, MonoClass *klass, gpointer item, MonoObject* o)
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
		if (G_UNLIKELY(mono_metadata_has_updates()) && ((flags & MONO_REFL_CACHE_NO_HOT_RELOAD_INVALIDATE) == 0))
			e->generation = mono_metadata_update_get_thread_generation();
		else
			e->generation = 0;
		mono_conc_g_hash_table_insert (mem_manager->refobject_hash, e, o);
		obj = o;
	}
	mono_mem_manager_unlock (mem_manager);
	return obj;
}

static inline MonoObjectHandle
cache_object_handle (MonoMemoryManager *mem_manager, int flags, MonoClass *klass, gpointer item, MonoObjectHandle o)
{
	MonoObjectHandle obj;
	ReflectedEntry pe;
	pe.item = item;
	pe.refclass = klass;

	mono_mem_manager_init_reflection_hashes (mem_manager);

	mono_mem_manager_lock (mem_manager);
	if (!mem_manager->collectible) {
		obj = MONO_HANDLE_NEW (MonoObject, (MonoObject *)mono_conc_g_hash_table_lookup (mem_manager->refobject_hash, &pe));
		if (MONO_HANDLE_IS_NULL (obj)) {
			ReflectedEntry *e = alloc_reflected_entry (mem_manager);
			e->item = item;
			e->refclass = klass;
			if (G_UNLIKELY(mono_metadata_has_updates()) && ((flags & MONO_REFL_CACHE_NO_HOT_RELOAD_INVALIDATE) == 0))
				e->generation = mono_metadata_update_get_thread_generation();
			else
				e->generation = 0;
			mono_conc_g_hash_table_insert (mem_manager->refobject_hash, e, MONO_HANDLE_RAW (o));
			MONO_HANDLE_ASSIGN (obj, o);
		}
	} else {
		obj = MONO_HANDLE_NEW (MonoObject, (MonoObject *)mono_weak_hash_table_lookup (mem_manager->weak_refobject_hash, &pe));
		if (MONO_HANDLE_IS_NULL (obj)) {
			ReflectedEntry *e = alloc_reflected_entry (mem_manager);
			e->item = item;
			e->refclass = klass;
			if (G_UNLIKELY(mono_metadata_has_updates()) && ((flags & MONO_REFL_CACHE_NO_HOT_RELOAD_INVALIDATE) == 0))
				e->generation = mono_metadata_update_get_thread_generation();
			else
				e->generation = 0;
			mono_weak_hash_table_insert (mem_manager->weak_refobject_hash, e, MONO_HANDLE_RAW (o));
			MONO_HANDLE_ASSIGN (obj, o);
		}
	}
	mono_mem_manager_unlock (mem_manager);
	return obj;
}

#define CACHE_OBJECT(t,mem_manager,flags,p,o,k) ((t) (cache_object ((mem_manager), (flags), (k), (p), (o))))
#define CACHE_OBJECT_HANDLE(t,mem_manager,flags,p,o,k) (MONO_HANDLE_CAST (t, cache_object_handle ((mem_manager), (flags), (k), (p), (o))))

static inline MonoObjectHandle
check_object_handle (MonoMemoryManager *mem_manager, int flags, MonoClass *klass, gpointer item)
{
	MonoObjectHandle obj_handle;
	gpointer orig_e, orig_value;
	ReflectedEntry e;
	e.item = item;
	e.refclass = klass;

	// FIXME: May need a memory manager for item+klass ?

	mono_mem_manager_init_reflection_hashes (mem_manager);

	mono_mem_manager_lock (mem_manager);
	if (!mem_manager->collectible) {
		MonoConcGHashTable *hash = mem_manager->refobject_hash;
		obj_handle = MONO_HANDLE_NEW (MonoObject, (MonoObject *)mono_conc_g_hash_table_lookup (hash, &e));
	} else {
		MonoWeakHashTable *hash = mem_manager->weak_refobject_hash;
		obj_handle = MONO_HANDLE_NEW (MonoObject, (MonoObject *)mono_weak_hash_table_lookup (hash, &e));
	}

	if (!mem_manager->collectible) {
		MonoConcGHashTable *hash = mem_manager->refobject_hash;
		if (mono_conc_g_hash_table_lookup_extended (hash, &e, &orig_e, &orig_value))
			if (mono_metadata_has_updates() && ((flags & MONO_REFL_CACHE_NO_HOT_RELOAD_INVALIDATE) == 0) && ((ReflectedEntry *)orig_e)->generation < mono_metadata_update_get_thread_generation()) {
				mono_conc_g_hash_table_remove (hash, &e);
				free_reflected_entry ((ReflectedEntry *)orig_e);
				obj_handle = MONO_HANDLE_NEW (MonoObject, NULL);
			} else {
				obj_handle = MONO_HANDLE_NEW (MonoObject, (MonoObject *)orig_value);
			}
		else {
			obj_handle = MONO_HANDLE_NEW (MonoObject, NULL);
		}
	} else {
		MonoWeakHashTable *hash = mem_manager->weak_refobject_hash;
		obj_handle = MONO_HANDLE_NEW (MonoObject, (MonoObject *)mono_weak_hash_table_lookup (hash, &e));
	}
	mono_mem_manager_unlock (mem_manager);

	return obj_handle;
}

typedef MonoObjectHandle (*ReflectionCacheConstructFunc_handle) (MonoClass*, gpointer, gpointer, MonoError *);

static inline MonoObjectHandle
check_or_construct_handle (MonoMemoryManager *mem_manager, int flags, MonoClass *klass, gpointer item, gpointer user_data, MonoError *error, ReflectionCacheConstructFunc_handle construct)
{
	error_init (error);
	MonoObjectHandle obj = check_object_handle (mem_manager, flags, klass, item);
	if (!MONO_HANDLE_IS_NULL (obj))
		return obj;
	MONO_HANDLE_ASSIGN (obj, construct (klass, item, user_data, error));
	return_val_if_nok (error, NULL_HANDLE);
	if (MONO_HANDLE_IS_NULL (obj))
		return obj;
	/* note no caching if there was an error in construction */
	return cache_object_handle (mem_manager, flags, klass, item, obj);
}

#define CHECK_OR_CONSTRUCT_HANDLE(type,mem_manager,flags,item,klass,construct,user_data) \
	(MONO_HANDLE_CAST (type, check_or_construct_handle ( \
		(mem_manager), (flags), (klass), (item), (user_data), error, (ReflectionCacheConstructFunc_handle) (construct))))

#endif /*__MONO_METADATA_REFLECTION_CACHE_H__*/
