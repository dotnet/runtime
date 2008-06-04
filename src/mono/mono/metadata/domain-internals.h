/*
 * Appdomain-related internal data structures and functions.
 */
#ifndef __MONO_METADATA_DOMAIN_INTERNALS_H__
#define __MONO_METADATA_DOMAIN_INTERNALS_H__

#include <mono/metadata/appdomain.h>
#include <mono/utils/mono-codeman.h>
#include <mono/utils/mono-hash.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-internal-hash.h>
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
	MonoObject *activation_arguments; /* it is System.Object in 1.x, ActivationArguments in 2.0 */
	MonoObject *domain_initializer;
	MonoArray *domain_initializer_args;
	MonoObject *application_trust; /* it is System.Object in 1.x, ApplicationTrust in 2.0 */
	MonoBoolean disallow_appbase_probe;
	MonoArray *configuration_bytes;
} MonoAppDomainSetup;

typedef struct _MonoJitInfoTable MonoJitInfoTable;
typedef struct _MonoJitInfoTableChunk MonoJitInfoTableChunk;

#define MONO_JIT_INFO_TABLE_CHUNK_SIZE		64

struct _MonoJitInfoTableChunk
{
	int		       refcount;
	volatile int           num_elements;
	volatile gint8        *last_code_end;
	MonoJitInfo * volatile data [MONO_JIT_INFO_TABLE_CHUNK_SIZE];
};

struct _MonoJitInfoTable
{
	MonoDomain	       *domain;
	int			num_chunks;
	MonoJitInfoTableChunk  *chunks [MONO_ZERO_LEN_ARRAY];
};

typedef GArray MonoAotModuleInfoTable;

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

/*
 * Will contain information on the generic type arguments in the
 * future.  For now, all arguments are always reference types.
 */
typedef struct {
	int dummy;
} MonoGenericSharingContext;

typedef struct
{
	MonoGenericSharingContext *generic_sharing_context;
	gint32 this_offset;
	guint8 this_reg;
	gboolean this_in_reg:1;
} MonoGenericJitInfo;

struct _MonoJitInfo {
	/* NOTE: These first two elements (method and
	   next_jit_code_hash) must be in the same order and at the
	   same offset as in RuntimeMethod, because of the jit_code_hash
	   internal hash table in MonoDomain. */
	MonoMethod *method;
	struct _MonoJitInfo *next_jit_code_hash;
	gpointer    code_start;
	guint32     used_regs;
	int         code_size;
	guint32     num_clauses:16;
	/* Whenever the code is domain neutral or 'shared' */
	gboolean    domain_neutral:1;
	gboolean    cas_inited:1;
	gboolean    cas_class_assert:1;
	gboolean    cas_class_deny:1;
	gboolean    cas_class_permitonly:1;
	gboolean    cas_method_assert:1;
	gboolean    cas_method_deny:1;
	gboolean    cas_method_permitonly:1;
	gboolean    has_generic_jit_info:1;
	MonoJitExceptionInfo clauses [MONO_ZERO_LEN_ARRAY];
	/* There is an optional MonoGenericJitInfo after the clauses */
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

typedef struct _MonoJitCodeHash MonoJitCodeHash;

struct _MonoDomain {
	CRITICAL_SECTION    lock;
	MonoMemPool        *mp;
	MonoCodeManager    *code_mp;
	/*
	 * keep all the managed objects close to each other for the precise GC
	 * For the Boehm GC we additionally keep close also other GC-tracked pointers.
	 */
#define MONO_DOMAIN_FIRST_OBJECT setup
	MonoAppDomainSetup *setup;
	MonoAppDomain      *domain;
	MonoAppContext     *default_context;
	MonoException      *out_of_memory_ex;
	MonoException      *null_reference_ex;
	MonoException      *stack_overflow_ex;
#define MONO_DOMAIN_FIRST_GC_TRACKED env
	MonoGHashTable     *env;
	MonoGHashTable     *ldstr_table;
	/* hashtables for Reflection handles */
	MonoGHashTable     *type_hash;
	MonoGHashTable     *refobject_hash;
	/* a GC-tracked array to keep references to the static fields of types */
	gpointer           *static_data_array;
	/* maps class -> type initialization exception object */
	MonoGHashTable    *type_init_exception_hash;
	/* maps delegate trampoline addr -> delegate object */
	MonoGHashTable     *delegate_hash_table;
#define MONO_DOMAIN_LAST_GC_TRACKED delegate_hash_table
	guint32            state;
	/* Needed by Thread:GetDomainID() */
	gint32             domain_id;
	gint32             shadow_serial;
	unsigned char      inet_family_hint; // used in socket-io.c as a cache
	GSList             *domain_assemblies;
	MonoAssembly       *entry_assembly;
	char               *friendly_name;
	GHashTable         *class_vtable_hash;
	/* maps remote class key -> MonoRemoteClass */
	GHashTable         *proxy_vtable_hash;
	MonoInternalHashTable jit_code_hash;
	/* maps MonoMethod -> MonoJitDynamicMethodInfo */
	GHashTable         *dynamic_code_hash;
	int		    num_jit_info_tables;
	MonoJitInfoTable * 
	  volatile          jit_info_table;
	GSList		   *jit_info_free_queue;
	/* Used when loading assemblies */
	gchar **search_path;
	gchar *private_bin_path;
	
	/* Used by remoting proxies */
	MonoMethod         *create_proxy_for_type_method;
	MonoMethod         *private_invoke_method;
	/* Used to store offsets of thread and context static fields */
	GHashTable         *special_static_fields;
	GHashTable         *jump_target_hash;
	GHashTable         *class_init_trampoline_hash;
	GHashTable         *jump_trampoline_hash;
	GHashTable         *jit_trampoline_hash;
	GHashTable         *delegate_trampoline_hash;
	/* 
	 * This must be a GHashTable, since these objects can't be finalized
	 * if the hashtable contains a GC visible reference to them.
	 */
	GHashTable         *finalizable_objects_hash;
	/* Used when accessing 'domain_assemblies' */
	CRITICAL_SECTION    assemblies_lock;

	GHashTable	   *shared_generics_hash;
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

typedef MonoDomain* (*MonoLoadFunc) (const char *filename, const char *runtime_version);

void
mono_install_runtime_load  (MonoLoadFunc func) MONO_INTERNAL;

MonoDomain*
mono_runtime_load (const char *filename, const char *runtime_version) MONO_INTERNAL;

void 
mono_init_com_types (void) MONO_INTERNAL;

void 
mono_cleanup (void) MONO_INTERNAL;

void
mono_close_exe_image (void) MONO_INTERNAL;

void
mono_jit_info_table_add    (MonoDomain *domain, MonoJitInfo *ji) MONO_INTERNAL;

void
mono_jit_info_table_remove (MonoDomain *domain, MonoJitInfo *ji) MONO_INTERNAL;

void
mono_jit_info_add_aot_module (MonoImage *image, gpointer start, gpointer end) MONO_INTERNAL;

MonoGenericJitInfo*
mono_jit_info_get_generic_jit_info (MonoJitInfo *ji) MONO_INTERNAL;

MonoGenericSharingContext*
mono_jit_info_get_generic_sharing_context (MonoJitInfo *ji) MONO_INTERNAL;

void
mono_jit_info_set_generic_sharing_context (MonoJitInfo *ji, MonoGenericSharingContext *gsctx) MONO_INTERNAL;

MonoJitInfo*
mono_domain_lookup_shared_generic (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;

void
mono_domain_register_shared_generic (MonoDomain *domain, MonoMethod *method, MonoJitInfo *jit_info) MONO_INTERNAL;

char *
mono_make_shadow_copy (const char *filename);

/* 
 * Installs a new function which is used to return a MonoJitInfo for a method inside
 * an AOT module.
 */
typedef MonoJitInfo *(*MonoJitInfoFindInAot)         (MonoDomain *domain, MonoImage *image, gpointer addr);
void          mono_install_jit_info_find_in_aot (MonoJitInfoFindInAot func) MONO_INTERNAL;

void
mono_jit_code_hash_init (MonoInternalHashTable *jit_code_hash) MONO_INTERNAL;

MonoAppDomain *
ves_icall_System_AppDomain_getCurDomain            (void) MONO_INTERNAL;

MonoAppDomain *
ves_icall_System_AppDomain_getRootDomain           (void) MONO_INTERNAL;

MonoAppDomain *
ves_icall_System_AppDomain_createDomain            (MonoString         *friendly_name,
						    MonoAppDomainSetup *setup) MONO_INTERNAL;

MonoObject *
ves_icall_System_AppDomain_GetData                 (MonoAppDomain *ad, 
						    MonoString    *name) MONO_INTERNAL;

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssemblyRaw         (MonoAppDomain *ad,
    						    MonoArray *raw_assembly, 
						    MonoArray *raw_symbol_store,
						    MonoObject *evidence,
						    MonoBoolean refonly) MONO_INTERNAL;

void
ves_icall_System_AppDomain_SetData                 (MonoAppDomain *ad, 
						    MonoString    *name, 
						    MonoObject    *data) MONO_INTERNAL;

MonoAppDomainSetup *
ves_icall_System_AppDomain_getSetup                (MonoAppDomain *ad) MONO_INTERNAL;

MonoString *
ves_icall_System_AppDomain_getFriendlyName         (MonoAppDomain *ad) MONO_INTERNAL;

MonoArray *
ves_icall_System_AppDomain_GetAssemblies           (MonoAppDomain *ad,
						    MonoBoolean refonly) MONO_INTERNAL;

MonoReflectionAssembly *
ves_icall_System_Reflection_Assembly_LoadFrom      (MonoString *fname,
						    MonoBoolean refonly) MONO_INTERNAL;

MonoReflectionAssembly *
ves_icall_System_AppDomain_LoadAssembly            (MonoAppDomain *ad, 
						    MonoString *assRef,
						    MonoObject    *evidence,
						    MonoBoolean refonly) MONO_INTERNAL;

gboolean
ves_icall_System_AppDomain_InternalIsFinalizingForUnload (gint32 domain_id) MONO_INTERNAL;

void
ves_icall_System_AppDomain_InternalUnload          (gint32 domain_id) MONO_INTERNAL;

gint32
ves_icall_System_AppDomain_ExecuteAssembly         (MonoAppDomain *ad, 
													MonoReflectionAssembly *refass,
													MonoArray     *args) MONO_INTERNAL;

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomain	   (MonoAppDomain *ad) MONO_INTERNAL;

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomainByID   (gint32 domainid) MONO_INTERNAL;

void
ves_icall_System_AppDomain_InternalPushDomainRef (MonoAppDomain *ad) MONO_INTERNAL;

void
ves_icall_System_AppDomain_InternalPushDomainRefByID (gint32 domain_id) MONO_INTERNAL;

void
ves_icall_System_AppDomain_InternalPopDomainRef (void) MONO_INTERNAL;

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetContext      (void) MONO_INTERNAL;

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetDefaultContext      (void) MONO_INTERNAL;

MonoAppContext * 
ves_icall_System_AppDomain_InternalSetContext	   (MonoAppContext *mc) MONO_INTERNAL;

gint32 
ves_icall_System_AppDomain_GetIDFromDomain (MonoAppDomain * ad) MONO_INTERNAL;

MonoString *
ves_icall_System_AppDomain_InternalGetProcessGuid (MonoString* newguid) MONO_INTERNAL;

MonoAssembly *
mono_assembly_load_corlib (const MonoRuntimeInfo *runtime, MonoImageOpenStatus *status) MONO_INTERNAL;

const MonoRuntimeInfo*
mono_get_runtime_info (void) MONO_INTERNAL;

gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname) MONO_INTERNAL;

void
mono_assembly_name_free (MonoAssemblyName *aname) MONO_INTERNAL;

MonoImage *mono_assembly_open_from_bundle (const char *filename,
					   MonoImageOpenStatus *status,
					   gboolean refonly) MONO_INTERNAL;

void
mono_domain_add_class_static_data (MonoDomain *domain, MonoClass *klass, gpointer data, guint32 *bitmap);

MonoReflectionAssembly *
mono_try_assembly_resolve (MonoDomain *domain, MonoString *fname, gboolean refonly) MONO_INTERNAL;

MonoAssembly* mono_assembly_load_full_nosearch (MonoAssemblyName *aname, 
						const char       *basedir, 
						MonoImageOpenStatus *status,
						gboolean refonly) MONO_INTERNAL;

#endif /* __MONO_METADATA_DOMAIN_INTERNALS_H__ */
