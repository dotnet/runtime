/* 
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_REFLECTION_CACHE_H__
#define __MONO_METADATA_REFLECTION_CACHE_H__

#include <glib.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/mono-hash.h>
#include <mono/metadata/mempool.h>

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

#ifdef HAVE_BOEHM_GC
/* ReflectedEntry doesn't need to be GC tracked */
#define ALLOC_REFENTRY g_new0 (ReflectedEntry, 1)
#define FREE_REFENTRY(entry) g_free ((entry))
#define REFENTRY_REQUIRES_CLEANUP
#else
#define ALLOC_REFENTRY (ReflectedEntry *)mono_mempool_alloc (domain->mp, sizeof (ReflectedEntry))
/* FIXME: */
#define FREE_REFENTRY(entry)
#endif


#define CACHE_OBJECT(t,p,o,k)	\
	do {	\
		t _obj;	\
        ReflectedEntry pe; \
        pe.item = (p); \
        pe.refclass = (k); \
        mono_domain_lock (domain); \
		if (!domain->refobject_hash)	\
			domain->refobject_hash = mono_g_hash_table_new_type (reflected_hash, reflected_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, "domain reflection objects table");	\
        _obj = (t)mono_g_hash_table_lookup (domain->refobject_hash, &pe); \
        if (!_obj) { \
		    ReflectedEntry *e = ALLOC_REFENTRY; 	\
		    e->item = (p);	\
		    e->refclass = (k);	\
		    mono_g_hash_table_insert (domain->refobject_hash, e,o);	\
            _obj = o; \
        } \
		mono_domain_unlock (domain);	\
        return _obj; \
	} while (0)

#define CHECK_OBJECT(t,p,k)	\
	do {	\
		t _obj;	\
		ReflectedEntry e; 	\
		e.item = (p);	\
		e.refclass = (k);	\
		mono_domain_lock (domain);	\
		if (!domain->refobject_hash)	\
			domain->refobject_hash = mono_g_hash_table_new_type (reflected_hash, reflected_equal, MONO_HASH_VALUE_GC, MONO_ROOT_SOURCE_DOMAIN, "domain reflection objects table");	\
		if ((_obj = (t)mono_g_hash_table_lookup (domain->refobject_hash, &e))) {	\
			mono_domain_unlock (domain);	\
			return _obj;	\
		}	\
        mono_domain_unlock (domain); \
	} while (0)


#endif /*__MONO_METADATA_REFLECTION_CACHE_H__*/
