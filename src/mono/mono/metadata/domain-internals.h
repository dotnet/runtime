/**
 * \file
 * Appdomain-related internal data structures and functions.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_DOMAIN_INTERNALS_H__
#define __MONO_METADATA_DOMAIN_INTERNALS_H__

#include <mono/utils/mono-forward-internal.h>
#include <mono/metadata/object-forward.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/lock-tracer.h>
#include <mono/utils/mono-codeman.h>
#include <mono/metadata/mono-hash.h>
#include <mono/metadata/mono-conc-hash.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-internal-hash.h>
#include <mono/metadata/loader-internals.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/handle-decl.h>
#include <mono/mini/mono-private-unstable.h>

G_BEGIN_DECLS

/*
 * If this is set, the memory belonging to appdomains is not freed when a domain is
 * unloaded, and assemblies loaded by the appdomain are not unloaded either. This
 * allows us to use typed gc in non-default appdomains too, leading to increased
 * performance.
 */ 
extern gboolean mono_dont_free_domains;

struct _MonoAppContext {
	MonoObject obj;
	gint32 domain_id;
	gint32 context_id;
};

typedef struct _MonoThunkFreeList {
	guint32 size;
	int length;		/* only valid for the wait list */
	struct _MonoThunkFreeList *next;
} MonoThunkFreeList;

typedef struct _MonoJitCodeHash MonoJitCodeHash;

struct _MonoDomain {
	/*
	 * keep all the managed objects close to each other for the precise GC
	 * For the Boehm GC we additionally keep close also other GC-tracked pointers.
	 */
#define MONO_DOMAIN_FIRST_OBJECT domain
	MonoAppDomain      *domain;
	MonoException      *out_of_memory_ex;
	MonoException      *null_reference_ex;
	MonoException      *stack_overflow_ex;
	/* typeof (void) */
	MonoObject         *typeof_void;
	/* Ephemeron Tombstone*/
	MonoObject         *ephemeron_tombstone;
	/* new MonoType [0] */
	MonoArray          *empty_types;
	MonoString         *empty_string;
#define MONO_DOMAIN_LAST_OBJECT empty_string
	/* Needed by Thread:GetDomainID() */
	gint32             domain_id;
	/*
	 * For framework Mono, this is every assembly loaded in this
	 * domain. For netcore, this is every assembly loaded in every ALC in
	 * this domain.  In netcore, the thread that adds an assembly to its
	 * MonoAssemblyLoadContext:loaded_assemblies should also add it to this
	 * list.
	 */
	GSList             *domain_assemblies;
	char               *friendly_name;

	/* Used when accessing 'domain_assemblies' */
	MonoCoopMutex  assemblies_lock;
};

typedef struct  {
	guint16 major, minor, build, revision;
} AssemblyVersionSet;

/* MonoRuntimeInfo: Contains information about versions supported by this runtime */
typedef struct  {
	char runtime_version [12];
	char framework_version [4];
	AssemblyVersionSet version_sets [5];
} MonoRuntimeInfo;

static inline void
mono_domain_assemblies_lock (MonoDomain *domain)
{
	mono_locks_coop_acquire (&domain->assemblies_lock, DomainAssembliesLock);
}

static inline void
mono_domain_assemblies_unlock (MonoDomain *domain)
{
	mono_locks_coop_release (&domain->assemblies_lock, DomainAssembliesLock);
}

typedef MonoDomain* (*MonoLoadFunc) (const char *filename, const char *runtime_version);

void
mono_install_runtime_load  (MonoLoadFunc func);

MonoDomain*
mono_runtime_load (const char *filename, const char *runtime_version);

void
mono_runtime_quit_internal (void);

void
mono_close_exe_image (void);

void
mono_domain_unset (void);

void
mono_domain_set_internal_with_options (MonoDomain *domain, gboolean migrate_exception);

void
mono_jit_code_hash_init (MonoInternalHashTable *jit_code_hash);

MonoAssembly *
mono_assembly_load_corlib (MonoImageOpenStatus *status);

const MonoRuntimeInfo*
mono_get_runtime_info (void);

void
mono_runtime_set_no_exec (gboolean val);

gboolean
mono_runtime_get_no_exec (void);

gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname);

MonoAssembly *
mono_domain_assembly_open_internal (MonoDomain *domain, MonoAssemblyLoadContext *alc, const char *name);

MonoImage *mono_assembly_open_from_bundle (MonoAssemblyLoadContext *alc,
					   const char *filename,
					   MonoImageOpenStatus *status,
					   const char *culture);

MonoAssembly *
mono_try_assembly_resolve (MonoAssemblyLoadContext *alc, const char *fname, MonoAssembly *requesting, MonoError *error);

MonoAssembly *
mono_domain_assembly_postload_search (MonoAssemblyLoadContext *alc, MonoAssembly *requesting, MonoAssemblyName *aname, gboolean postload, gpointer user_data, MonoError *error);

MonoJitInfo* mono_jit_info_table_find_internal (MonoDomain *domain, gpointer addr, gboolean try_aot, gboolean allow_trampolines);

typedef void (*MonoJitInfoFunc) (MonoJitInfo *ji, gpointer user_data);

void
mono_jit_info_table_foreach_internal (MonoDomain *domain, MonoJitInfoFunc func, gpointer user_data);

void mono_enable_debug_domain_unload (gboolean enable);

void
mono_runtime_init_checked (MonoDomain *domain, MonoThreadStartCB start_cb, MonoThreadAttachCB attach_cb, MonoError *error);

gboolean
mono_assembly_has_reference_assembly_attribute (MonoAssembly *assembly, MonoError *error);

GPtrArray*
mono_domain_get_assemblies (MonoDomain *domain);

void
mono_runtime_register_appctx_properties (int nprops, const char **keys,  const char **values);

void
mono_runtime_register_runtimeconfig_json_properties (MonovmRuntimeConfigArguments *arg, MonovmRuntimeConfigArgumentsCleanup cleanup_fn, void *user_data);

void
mono_runtime_install_appctx_properties (void);

gboolean 
mono_domain_set_fast (MonoDomain *domain, gboolean force);

static inline MonoMemoryManager *
mono_domain_memory_manager (MonoDomain *domain)
{
	return (MonoMemoryManager *)mono_alc_get_default ()->memory_manager;
}

static inline MonoMemoryManager*
mono_mem_manager_get_ambient (void)
{
	// FIXME: All callers should get a MemoryManager from their callers or context
	return mono_domain_memory_manager (mono_get_root_domain ());
}

G_END_DECLS

#endif /* __MONO_METADATA_DOMAIN_INTERNALS_H__ */
