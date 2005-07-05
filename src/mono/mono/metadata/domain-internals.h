/*
 * Appdomain-related internal data structures and functions.
 */
#ifndef __MONO_METADATA_DOMAIN_INTERNALS_H__
#define __MONO_METADATA_DOMAIN_INTERNALS_H__

#include <mono/metadata/appdomain.h>
#include <mono/utils/mono-codeman.h>
#include <mono/utils/mono-hash.h>
#include <mono/io-layer/io-layer.h>

extern CRITICAL_SECTION mono_delegate_section;

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
	MonoBoolean path_changed;
	int loader_optimization;
	MonoBoolean disallow_binding_redirects;
	MonoBoolean disallow_code_downloads;
} MonoAppDomainSetup;

typedef GArray MonoJitInfoTable;

typedef struct {
	guint32  flags;
	gint32   exvar_offset;
	gpointer try_start;
	gpointer try_end;
	gpointer handler_start;
	union {
		MonoClass *catch_class;
		gpointer filter;
	} data;
} MonoJitExceptionInfo;

struct _MonoJitInfo {
	MonoMethod *method;
	gpointer    code_start;
	guint32     used_regs;
	int         code_size;
	guint32     num_clauses:24;
	/* Whenever the code is domain neutral or 'shared' */
	gboolean    domain_neutral:1;
	gboolean    cas_inited:1;
	gboolean    cas_class_assert:1;
	gboolean    cas_class_deny:1;
	gboolean    cas_class_permitonly:1;
	gboolean    cas_method_assert:1;
	gboolean    cas_method_deny:1;
	gboolean    cas_method_permitonly:1;
	MonoJitExceptionInfo clauses [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	MonoJitInfo *ji;
	MonoCodeManager *code_mp;
} MonoJitDynamicMethodInfo;

struct _MonoAppContext {
	MonoObject obj;
	gint32 domain_id;
	gint32 context_id;
	gpointer *static_data;
};

typedef enum {
	MONO_APPDOMAIN_CREATED,
	MONO_APPDOMAIN_UNLOADING,
	MONO_APPDOMAIN_UNLOADED
} MonoAppDomainState;

struct _MonoDomain {
	MonoAppDomain      *domain;
	CRITICAL_SECTION    lock;
	MonoMemPool        *mp;
	MonoCodeManager    *code_mp;
	MonoGHashTable     *env;
	GSList             *domain_assemblies;
	MonoAssembly       *entry_assembly;
	MonoAppDomainSetup *setup;
	char               *friendly_name;
	guint32            state;
	MonoGHashTable     *ldstr_table;
	GHashTable         *class_vtable_hash;
	/* maps MonoString -> MonoRemoteClass */
	MonoGHashTable     *proxy_vtable_hash;
	MonoGHashTable     *static_data_hash;
	GHashTable         *jit_code_hash;
	/* maps MonoMethod -> MonoJitDynamicMethodInfo */
	GHashTable         *dynamic_code_hash;
	/* maps delegate trampoline addr -> delegate object */
	MonoGHashTable     *delegate_hash_table;
	MonoJitInfoTable   *jit_info_table;
	/* hashtables for Reflection handles */
	MonoGHashTable     *type_hash;
	MonoGHashTable     *refobject_hash;
	/* Needed by Thread:GetDomainID() */
	gint32             domain_id;
	/* Used when loading assemblies */
	gchar **search_path;
	/* Used by remoting proxies */
	MonoMethod         *create_proxy_for_type_method;
	MonoMethod         *private_invoke_method;
	MonoAppContext     *default_context;
	MonoException      *out_of_memory_ex;
	MonoException      *null_reference_ex;
	MonoException      *stack_overflow_ex;
	/* Used to store offsets of thread and context static fields */
	GHashTable         *special_static_fields;
	GHashTable         *jump_target_hash;
	GHashTable         *class_init_trampoline_hash;
	GHashTable         *jump_trampoline_hash;
	GHashTable         *jit_trampoline_hash;
	/* 
	 * This must be a GHashTable, since these objects can't be finalized
	 * if the hashtable contains a GC visible reference to them.
	 */
	GHashTable         *finalizable_objects_hash;
	/* Used when accessing 'domain_assemblies' */
	CRITICAL_SECTION    assemblies_lock;
};

typedef struct  {
	guint16 major, minor, build, revision;
} AssemblyVersionSet;

/* MonoRuntimeInfo: Contains information about versions supported by this runtime */
typedef struct  {
	const char runtime_version [12];
	const char framework_version [4];
	const AssemblyVersionSet version_sets [2];
} MonoRuntimeInfo;

#define mono_domain_lock(domain)   EnterCriticalSection(&(domain)->lock)
#define mono_domain_unlock(domain) LeaveCriticalSection(&(domain)->lock)
#define mono_domain_assemblies_lock(domain)   EnterCriticalSection(&(domain)->assemblies_lock)
#define mono_domain_assemblies_unlock(domain) LeaveCriticalSection(&(domain)->assemblies_lock)

void
mono_jit_info_table_add    (MonoDomain *domain, MonoJitInfo *ji);

void
mono_jit_info_table_remove (MonoDomain *domain, MonoJitInfo *ji);

void
mono_jit_info_add_aot_module (MonoImage *image, gpointer start, gpointer end);

/* 
 * Installs a new function which is used to return a MonoJitInfo for a method inside
 * an AOT module.
 */
typedef MonoJitInfo *(*MonoJitInfoFindInAot)         (MonoDomain *domain, MonoImage *image, gpointer addr);
void          mono_install_jit_info_find_in_aot (MonoJitInfoFindInAot func);

MonoAppDomain *
ves_icall_System_AppDomain_getCurDomain            (void);

MonoAppDomain *
ves_icall_System_AppDomain_getRootDomain           (void);

MonoAppDomain *
ves_icall_System_AppDomain_createDomain            (MonoString         *friendly_name,
						    MonoAppDomainSetup *setup);

MonoObject *
ves_icall_System_AppDomain_GetData                 (MonoAppDomain *ad, 
						    MonoString    *name);

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssemblyRaw         (MonoAppDomain *ad,
    						    MonoArray *raw_assembly, 
						    MonoArray *raw_symbol_store,
						    MonoObject *evidence,
						    MonoBoolean refonly);

void
ves_icall_System_AppDomain_SetData                 (MonoAppDomain *ad, 
						    MonoString    *name, 
						    MonoObject    *data);

MonoAppDomainSetup *
ves_icall_System_AppDomain_getSetup                (MonoAppDomain *ad);

MonoString *
ves_icall_System_AppDomain_getFriendlyName         (MonoAppDomain *ad);

MonoArray *
ves_icall_System_AppDomain_GetAssemblies           (MonoAppDomain *ad,
						    MonoBoolean refonly);

MonoReflectionAssembly *
ves_icall_System_Reflection_Assembly_LoadFrom      (MonoString *fname,
						    MonoBoolean refonly);

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly            (MonoAppDomain *ad, 
						    MonoString *assRef,
						    MonoObject    *evidence,
						    MonoBoolean refonly);

gboolean
ves_icall_System_AppDomain_InternalIsFinalizingForUnload (gint32 domain_id);

void
ves_icall_System_AppDomain_InternalUnload          (gint32 domain_id);

gint32
ves_icall_System_AppDomain_ExecuteAssembly         (MonoAppDomain *ad, 
						    MonoString    *file, 
						    MonoObject    *evidence,
						    MonoArray     *args);

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomain	   (MonoAppDomain *ad);

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomainByID   (gint32 domainid);

void
ves_icall_System_AppDomain_InternalPushDomainRef (MonoAppDomain *ad);

void
ves_icall_System_AppDomain_InternalPushDomainRefByID (gint32 domain_id);

void
ves_icall_System_AppDomain_InternalPopDomainRef (void);

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetContext      (void);

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetDefaultContext      (void);

MonoAppContext * 
ves_icall_System_AppDomain_InternalSetContext	   (MonoAppContext *mc);

gint32 
ves_icall_System_AppDomain_GetIDFromDomain (MonoAppDomain * ad);

MonoString *
ves_icall_System_AppDomain_InternalGetProcessGuid (MonoString* newguid);

MonoAssembly *
mono_assembly_load_corlib (const MonoRuntimeInfo *runtime, MonoImageOpenStatus *status);

const MonoRuntimeInfo*
mono_get_runtime_info (void);

gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname);

void
mono_assembly_name_free (MonoAssemblyName *aname);

#endif /* __MONO_METADATA_DOMAIN_INTERNALS_H__ */
