/*
 * Appdomain-related internal data structures and functions.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 */
#ifndef __MONO_METADATA_DOMAIN_INTERNALS_H__
#define __MONO_METADATA_DOMAIN_INTERNALS_H__

#include <mono/metadata/appdomain.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/lock-tracer.h>
#include <mono/utils/mono-codeman.h>
#include <mono/metadata/mono-hash.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-internal-hash.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/mempool-internals.h>

extern CRITICAL_SECTION mono_delegate_section;
extern CRITICAL_SECTION mono_strtod_mutex;

/*
 * If this is set, the memory belonging to appdomains is not freed when a domain is
 * unloaded, and assemblies loaded by the appdomain are not unloaded either. This
 * allows us to use typed gc in non-default appdomains too, leading to increased
 * performance.
 */ 
extern gboolean mono_dont_free_domains;

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
	MonoObject *application_trust; /* it is System.Object in 1.x, ApplicationTrust in 2.0 */
	MonoArray *domain_initializer_args;
	MonoBoolean disallow_appbase_probe;
	MonoArray *configuration_bytes;
	MonoArray *serialized_non_primitives;
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

#define MONO_SIZEOF_JIT_INFO_TABLE (sizeof (struct _MonoJitInfoTable) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

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
		gpointer handler_end;
	} data;
} MonoJitExceptionInfo;

/*
 * Will contain information on the generic type arguments in the
 * future.  For now, all arguments are always reference types.
 */
typedef struct {
	int dummy;
} MonoGenericSharingContext;

/* Simplified DWARF location list entry */
typedef struct {
	/* Whenever the value is in a register */
	gboolean is_reg;
	/*
	 * If is_reg is TRUE, the register which contains the value. Otherwise
	 * the base register.
	 */
	int reg;
	/*
	 * If is_reg is FALSE, the offset of the stack location relative to 'reg'.
	 * Otherwise, 0.
	 */
	int offset;
	/*
	 * Offsets of the PC interval where the value is in this location.
	 */
	int from, to;
} MonoDwarfLocListEntry;

typedef struct
{
	MonoGenericSharingContext *generic_sharing_context;
	int nlocs;
	MonoDwarfLocListEntry *locations;
	gint32 this_offset;
	guint8 this_reg;
	gboolean has_this:1;
	gboolean this_in_reg:1;
} MonoGenericJitInfo;

/*
A try block hole is used to represent a non-contiguous part of
of a segment of native code protected by a given .try block.
Usually, a try block is defined as a contiguous segment of code.
But in some cases it's needed to have some parts of it to not be protected.
For example, given "try {} finally {}", the code in the .try block to call
the finally part looks like:

try {
    ...
	call finally_block
	adjust stack
	jump outside try block
	...
} finally {
	...
}

The instructions between the call and the jump should not be under the try block since they happen
after the finally block executes, which means if an async exceptions happens at that point we would
execute the finally clause twice. So, to avoid this, we introduce a hole in the try block to signal
that those instructions are not protected.
*/
typedef struct
{
	guint32 offset;
	guint16 clause;
	guint16 length;
} MonoTryBlockHoleJitInfo;

typedef struct
{
	guint16 num_holes;
	MonoTryBlockHoleJitInfo holes [MONO_ZERO_LEN_ARRAY];
} MonoTryBlockHoleTableJitInfo;

typedef struct
{
	guint32 stack_size;
} MonoArchEHJitInfo;

struct _MonoJitInfo {
	/* NOTE: These first two elements (method and
	   next_jit_code_hash) must be in the same order and at the
	   same offset as in RuntimeMethod, because of the jit_code_hash
	   internal hash table in MonoDomain. */
	MonoMethod *method;
	struct _MonoJitInfo *next_jit_code_hash;
	gpointer    code_start;
	/* This might contain an id for the unwind info instead of a register mask */
	guint32     used_regs;
	int         code_size;
	guint32     num_clauses:15;
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
	gboolean    has_try_block_holes:1;
	gboolean    has_arch_eh_info:1;
	gboolean    from_aot:1;
	gboolean    from_llvm:1;

	/* FIXME: Embed this after the structure later*/
	gpointer    gc_info; /* Currently only used by SGen */
	
	MonoJitExceptionInfo clauses [MONO_ZERO_LEN_ARRAY];
	/* There is an optional MonoGenericJitInfo after the clauses */
	/* There is an optional MonoTryBlockHoleTableJitInfo after MonoGenericJitInfo clauses*/
	/* There is an optional MonoArchEHJitInfo after MonoTryBlockHoleTableJitInfo */
};

#define MONO_SIZEOF_JIT_INFO (offsetof (struct _MonoJitInfo, clauses))

struct _MonoAppContext {
	MonoObject obj;
	gint32 domain_id;
	gint32 context_id;
	gpointer *static_data;
};

/*
 * We have two unloading states because the domain
 * must remain fully functional while AppDomain::DomainUnload is
 * processed.
 * After that unloading began and all domain facilities are teared down
 * such as execution of new threadpool jobs.  
 */
typedef enum {
	MONO_APPDOMAIN_CREATED,
	MONO_APPDOMAIN_UNLOADING_START,
	MONO_APPDOMAIN_UNLOADING,
	MONO_APPDOMAIN_UNLOADED
} MonoAppDomainState;

typedef struct _MonoThunkFreeList {
	guint32 size;
	int length;		/* only valid for the wait list */
	struct _MonoThunkFreeList *next;
} MonoThunkFreeList;

typedef struct _MonoJitCodeHash MonoJitCodeHash;

typedef struct _MonoTlsDataRecord MonoTlsDataRecord;
struct _MonoTlsDataRecord {
	MonoTlsDataRecord *next;
	guint32 tls_offset;
	guint32 size;
};

struct _MonoDomain {
	/*
	 * This lock must never be taken before the loader lock,
	 * i.e. if both are taken by the same thread, the loader lock
	 * must taken first.
	 */
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
	/* typeof (void) */
	MonoObject         *typeof_void;
	/* Ephemeron Tombstone*/
	MonoObject         *ephemeron_tombstone;
	/* new MonoType [0] */
	MonoArray          *empty_types;
	/* 
	 * The fields between FIRST_GC_TRACKED and LAST_GC_TRACKED are roots, but
	 * not object references.
	 */
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
	GPtrArray          *class_vtable_array;
	/* maps remote class key -> MonoRemoteClass */
	GHashTable         *proxy_vtable_hash;
	/* Protected by 'jit_code_hash_lock' */
	MonoInternalHashTable jit_code_hash;
	CRITICAL_SECTION    jit_code_hash_lock;
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
	MonoTlsDataRecord  *tlsrec_list;
	/* 
	 * This must be a GHashTable, since these objects can't be finalized
	 * if the hashtable contains a GC visible reference to them.
	 */
	GHashTable         *finalizable_objects_hash;

	/* These two are boehm only */
	/* Maps MonoObjects to a GSList of WeakTrackResurrection GCHandles pointing to them */
	GHashTable         *track_resurrection_objects_hash;
	/* Maps WeakTrackResurrection GCHandles to the MonoObjects they point to */
	GHashTable         *track_resurrection_handles_hash;

	/* Protects the three hashes above */
	CRITICAL_SECTION   finalizable_objects_hash_lock;
	/* Used when accessing 'domain_assemblies' */
	CRITICAL_SECTION    assemblies_lock;

	GHashTable	   *method_rgctx_hash;

	GHashTable	   *generic_virtual_cases;
	MonoThunkFreeList **thunk_free_lists;

	GHashTable     *generic_virtual_thunks;

	/* Information maintained by the JIT engine */
	gpointer runtime_info;

	/*thread pool jobs, used to coordinate shutdown.*/
	volatile int			threadpool_jobs;
	HANDLE				cleanup_semaphore;
	
	/* Contains the compiled runtime invoke wrapper used by finalizers */
	gpointer            finalize_runtime_invoke;

	/* Contains the compiled runtime invoke wrapper used by async resylt creation to capture thread context*/
	gpointer            capture_context_runtime_invoke;

	/* Contains the compiled method used by async resylt creation to capture thread context*/
	gpointer            capture_context_method;

	/* Assembly bindings, the per-domain part */
	GSList *assembly_bindings;
	gboolean assembly_bindings_parsed;

	/* Used by socket-io.c */
	/* These are domain specific, since the assembly can be unloaded */
	MonoImage *socket_assembly;
	MonoClass *sockaddr_class;
	MonoClassField *sockaddr_data_field;

	/* Used by threadpool.c */
	MonoImage *system_image;
	MonoImage *system_net_dll;
	MonoClass *corlib_asyncresult_class;
	MonoClass *socket_class;
	MonoClass *ad_unloaded_ex_class;
	MonoClass *process_class;

	/* Cache function pointers for architectures  */
	/* that require wrappers */
	GHashTable *ftnptrs_hash;

	guint32 execution_context_field_offset;
};

typedef struct  {
	guint16 major, minor, build, revision;
} AssemblyVersionSet;

/* MonoRuntimeInfo: Contains information about versions supported by this runtime */
typedef struct  {
	const char runtime_version [12];
	const char framework_version [4];
	const AssemblyVersionSet version_sets [3];
} MonoRuntimeInfo;

#define mono_domain_lock(domain) mono_locks_acquire(&(domain)->lock, DomainLock)
#define mono_domain_unlock(domain) mono_locks_release(&(domain)->lock, DomainLock)
#define mono_domain_assemblies_lock(domain) mono_locks_acquire(&(domain)->assemblies_lock, DomainAssembliesLock)
#define mono_domain_assemblies_unlock(domain) mono_locks_release(&(domain)->assemblies_lock, DomainAssembliesLock)
#define mono_domain_jit_code_hash_lock(domain) mono_locks_acquire(&(domain)->jit_code_hash_lock, DomainJitCodeHashLock)
#define mono_domain_jit_code_hash_unlock(domain) mono_locks_release(&(domain)->jit_code_hash_lock, DomainJitCodeHashLock)

typedef MonoDomain* (*MonoLoadFunc) (const char *filename, const char *runtime_version);

void
mono_install_runtime_load  (MonoLoadFunc func) MONO_INTERNAL;

MonoDomain*
mono_runtime_load (const char *filename, const char *runtime_version) MONO_INTERNAL;

typedef void (*MonoCreateDomainFunc) (MonoDomain *domain);

void
mono_install_create_domain_hook (MonoCreateDomainFunc func) MONO_INTERNAL;

typedef void (*MonoFreeDomainFunc) (MonoDomain *domain);

void
mono_install_free_domain_hook (MonoFreeDomainFunc func) MONO_INTERNAL;

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

char *
mono_make_shadow_copy (const char *filename) MONO_INTERNAL;

gboolean
mono_is_shadow_copy_enabled (MonoDomain *domain, const gchar *dir_name) MONO_INTERNAL;

gpointer
mono_domain_alloc  (MonoDomain *domain, guint size) MONO_INTERNAL;

gpointer
mono_domain_alloc0 (MonoDomain *domain, guint size) MONO_INTERNAL;

void*
mono_domain_code_reserve (MonoDomain *domain, int size) MONO_LLVM_INTERNAL;

void*
mono_domain_code_reserve_align (MonoDomain *domain, int size, int alignment) MONO_INTERNAL;

void
mono_domain_code_commit (MonoDomain *domain, void *data, int size, int newsize) MONO_INTERNAL;

void *
nacl_domain_get_code_dest (MonoDomain *domain, void *data) MONO_INTERNAL;

void 
nacl_domain_code_validate (MonoDomain *domain, guint8 **buf_base, int buf_size, guint8 **code_end) MONO_INTERNAL;

void
mono_domain_code_foreach (MonoDomain *domain, MonoCodeManagerFunc func, void *user_data) MONO_INTERNAL;

void
mono_domain_unset (void) MONO_INTERNAL;

void
mono_domain_set_internal_with_options (MonoDomain *domain, gboolean migrate_exception) MONO_INTERNAL;

MonoTryBlockHoleTableJitInfo*
mono_jit_info_get_try_block_hole_table_info (MonoJitInfo *ji) MONO_INTERNAL;

MonoArchEHJitInfo*
mono_jit_info_get_arch_eh_info (MonoJitInfo *ji) MONO_INTERNAL;

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

void
mono_runtime_set_no_exec (gboolean val) MONO_INTERNAL;

gboolean
mono_runtime_get_no_exec (void) MONO_INTERNAL;

gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname) MONO_INTERNAL;

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

void mono_set_private_bin_path_from_config (MonoDomain *domain) MONO_INTERNAL;

int mono_framework_version (void) MONO_INTERNAL;

void mono_reflection_cleanup_domain (MonoDomain *domain) MONO_INTERNAL;

void mono_assembly_cleanup_domain_bindings (guint32 domain_id) MONO_INTERNAL;;

#endif /* __MONO_METADATA_DOMAIN_INTERNALS_H__ */
