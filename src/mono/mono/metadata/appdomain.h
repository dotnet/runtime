/*
 * appdomain.h: AppDomain functions
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_APPDOMAIN_H_
#define _MONO_METADATA_APPDOMAIN_H_

#include <glib.h>

#include <mono/metadata/object.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/mempool.h>
#include <mono/utils/mono-hash.h>
#include <mono/io-layer/io-layer.h>

typedef void (*MonoThreadStartCB) (gpointer stack_start);

/* This is a copy of System.AppDomainSetup */
typedef struct {
	MonoObject object;
	MonoString *application_base;
	MonoString *application_name;
	MonoString *cache_path;
	MonoString *configuration_file;
	MonoString *dynamic_base;
	MonoString *license_file;
	MonoString *private_bin_path;
	MonoString *private_bin_path_probe;
	MonoString *shadow_copy_directories;
	MonoString *shadow_copy_files;
	MonoBoolean publisher_policy;
} MonoAppDomainSetup;

typedef GArray MonoJitInfoTable;

typedef struct {
	guint32  flags;
	gpointer try_start;
	gpointer try_end;
	gpointer handler_start;
	union {
		guint32 token;
		gpointer filter;
	} data;
} MonoJitExceptionInfo;

typedef struct {
	MonoMethod *method;
	gpointer    code_start;
	int         code_size;
	guint32     used_regs;
	unsigned    num_clauses;
	signed      exvar_offset;
	MonoJitExceptionInfo *clauses;
} MonoJitInfo;

typedef struct _MonoAppDomain MonoAppDomain;

struct _MonoDomain {
	MonoAppDomain      *domain;
	CRITICAL_SECTION    lock;
	MonoMemPool        *mp;
	MonoMemPool        *code_mp;
	MonoGHashTable     *env;
	GHashTable         *assemblies;
	MonoAssembly       *entry_assembly;
	MonoAppDomainSetup *setup;
	char               *friendly_name;
	MonoGHashTable     *ldstr_table;
	MonoGHashTable     *class_vtable_hash;
	MonoGHashTable     *proxy_vtable_hash;
	MonoGHashTable     *static_data_hash;
	GHashTable         *jit_code_hash;
	MonoJitInfoTable   *jit_info_table;
	/* hashtables for Reflection handles */
	MonoGHashTable     *type_hash;
	MonoGHashTable     *refobject_hash;
	/* Needed by Thread:GetDomainID() */
	gint32             domain_id;
};

/* This is a copy of System.AppDomain */
struct _MonoAppDomain {
	MonoObject  object;
	MonoDomain *data;
};

extern MonoDomain *mono_root_domain;

extern HANDLE mono_delegate_semaphore;
extern CRITICAL_SECTION mono_delegate_section;

#define mono_domain_lock(domain)   EnterCriticalSection(&(domain)->lock)
#define mono_domain_unlock(domain) LeaveCriticalSection(&(domain)->lock)

MonoDomain*
mono_init                  (const char *filename);

void
mono_runtime_init          (MonoDomain *domain, MonoThreadStartCB start_cb);

void
mono_runtime_cleanup       (MonoDomain *domain);

MonoDomain *
mono_domain_create         (void);

inline MonoDomain *
mono_domain_get            (void);

inline void
mono_domain_set            (MonoDomain *domain);

MonoAssembly *
mono_domain_assembly_open  (MonoDomain *domain, const char *name);

void
mono_domain_finalize       (MonoDomain *domain);

void
mono_domain_unload         (MonoDomain *domain, gboolean force);

void
mono_jit_info_table_add    (MonoDomain *domain, MonoJitInfo *ji);

MonoJitInfo *
mono_jit_info_table_find   (MonoDomain *domain, char *addr);

void
ves_icall_System_AppDomainSetup_InitAppDomainSetup (MonoAppDomainSetup *setup);

MonoAppDomain *
ves_icall_System_AppDomain_getCurDomain            (void);

MonoAppDomain *
ves_icall_System_AppDomain_createDomain            (MonoString         *friendly_name,
						    MonoAppDomainSetup *setup);

MonoObject *
ves_icall_System_AppDomain_GetData                 (MonoAppDomain *ad, 
						    MonoString    *name);

void
ves_icall_System_AppDomain_SetData                 (MonoAppDomain *ad, 
						    MonoString    *name, 
						    MonoObject    *data);

MonoAppDomainSetup *
ves_icall_System_AppDomain_getSetup                (MonoAppDomain *ad);

MonoString *
ves_icall_System_AppDomain_getFriendlyName         (MonoAppDomain *ad);

MonoArray *
ves_icall_System_AppDomain_GetAssemblies           (MonoAppDomain *ad);

MonoReflectionAssembly *
ves_icall_System_Reflection_Assembly_LoadFrom      (MonoString *fname);

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly            (MonoAppDomain *ad, 
						    MonoReflectionAssemblyName *assRef,
						    MonoObject    *evidence);

void
ves_icall_System_AppDomain_Unload                  (MonoAppDomain *ad);

gint32
ves_icall_System_AppDomain_ExecuteAssembly         (MonoAppDomain *ad, 
						    MonoString    *file, 
						    MonoObject    *evidence,
						    MonoArray     *args);

#endif /* _MONO_METADATA_APPDOMAIN_H_ */
