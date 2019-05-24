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
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/handle-decl.h>

/* Mono appdomain support is deeply itegrated in the runtime, as a result, even
 * though .NET Standard does not include System.AppDomain in
 * System.Private.CoreLib, we still depend on having an appdomain class.
 * So we move it to Mono.MonoDomain
 *
 */
#ifndef ENABLE_NETCORE
#define MONO_APPDOMAIN_CLASS_NAME_SPACE "System"
#define MONO_APPDOMAIN_CLASS_NAME "AppDomain"
#define MONO_APPDOMAIN_SETUP_CLASS_NAME_SPACE "System"
#define MONO_APPDOMAIN_SETUP_CLASS_NAME "AppDomainSetup"
#else
#define MONO_APPDOMAIN_CLASS_NAME_SPACE "Mono"
#define MONO_APPDOMAIN_CLASS_NAME "MonoDomain"
#define MONO_APPDOMAIN_SETUP_CLASS_NAME_SPACE "Mono"
#define MONO_APPDOMAIN_SETUP_CLASS_NAME "MonoDomainSetup"
#endif

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
	MonoJitInfo *next_tombstone;
	MonoJitInfo * volatile data [MONO_JIT_INFO_TABLE_CHUNK_SIZE];
};

struct _MonoJitInfoTable
{
	MonoDomain	       *domain;
	int			num_chunks;
	int			num_valid;
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
	/*
	 * For LLVM compiled code, this is the index of the il clause
	 * associated with this handler.
	 */
	int clause_index;
	uint32_t try_offset;
	uint32_t try_len;
	uint32_t handler_offset;
	uint32_t handler_len;
	union {
		MonoClass *catch_class;
		gpointer filter;
		gpointer handler_end;
	} data;
} MonoJitExceptionInfo;

/*
 * Contains information about the type arguments for generic shared methods.
 */
typedef struct {
	gboolean is_gsharedvt;
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
	guint32 epilog_size;
} MonoArchEHJitInfo;

typedef struct {
	/* Relative to code_start */
	int thunks_offset;
	int thunks_size;
} MonoThunkJitInfo;

typedef struct {
	guint8 *unw_info;
	int unw_info_len;
} MonoUnwindJitInfo;

typedef enum {
	JIT_INFO_NONE = 0,
	JIT_INFO_HAS_GENERIC_JIT_INFO = (1 << 0),
	JIT_INFO_HAS_TRY_BLOCK_HOLES = (1 << 1),
	JIT_INFO_HAS_ARCH_EH_INFO = (1 << 2),
	JIT_INFO_HAS_THUNK_INFO = (1 << 3),
	/*
	 * If this is set, the unwind info is stored in the structure, instead of being pointed to by the
	 * 'unwind_info' field.
	 */
	JIT_INFO_HAS_UNWIND_INFO = (1 << 4)
} MonoJitInfoFlags;

G_ENUM_FUNCTIONS (MonoJitInfoFlags)

struct _MonoJitInfo {
	/* NOTE: These first two elements (method and
	   next_jit_code_hash) must be in the same order and at the
	   same offset as in RuntimeMethod, because of the jit_code_hash
	   internal hash table in MonoDomain. */
	union {
		MonoMethod *method;
		MonoImage *image;
		MonoAotModule *aot_info;
		MonoTrampInfo *tramp_info;
	} d;
	union {
		MonoJitInfo *next_jit_code_hash;
		MonoJitInfo *next_tombstone;
	} n;
	gpointer    code_start;
	guint32     unwind_info;
	int         code_size;
	guint32     num_clauses:15;
	/* Whenever the code is domain neutral or 'shared' */
	gboolean    domain_neutral:1;
	gboolean    has_generic_jit_info:1;
	gboolean    has_try_block_holes:1;
	gboolean    has_arch_eh_info:1;
	gboolean    has_thunk_info:1;
	gboolean    has_unwind_info:1;
	gboolean    from_aot:1;
	gboolean    from_llvm:1;
	gboolean    dbg_attrs_inited:1;
	gboolean    dbg_hidden:1;
	/* Whenever this jit info was loaded in async context */
	gboolean    async:1;
	gboolean    dbg_step_through:1;
	gboolean    dbg_non_user_code:1;
	/*
	 * Whenever this jit info refers to a trampoline.
	 * d.tramp_info contains additional data in this case.
	 */
	gboolean    is_trampoline:1;
	/* Whenever this jit info refers to an interpreter method */
	gboolean    is_interp:1;

	/* FIXME: Embed this after the structure later*/
	gpointer    gc_info; /* Currently only used by SGen */

	gpointer    seq_points;
	
	MonoJitExceptionInfo clauses [MONO_ZERO_LEN_ARRAY];
	/* There is an optional MonoGenericJitInfo after the clauses */
	/* There is an optional MonoTryBlockHoleTableJitInfo after MonoGenericJitInfo clauses*/
	/* There is an optional MonoArchEHJitInfo after MonoTryBlockHoleTableJitInfo */
	/* There is an optional MonoThunkJitInfo after MonoArchEHJitInfo */
};

#define MONO_SIZEOF_JIT_INFO (offsetof (struct _MonoJitInfo, clauses))

typedef struct {
	gpointer *static_data; /* Used to free the static data without going through the MonoAppContext object itself. */
	uint32_t gc_handle;
} ContextStaticData;

struct _MonoAppContext {
	MonoObject obj;
	gint32 domain_id;
	gint32 context_id;
	gpointer *static_data;
	ContextStaticData *data;
};

/* Lock-free allocator */
typedef struct {
	guint8 *mem;
	gpointer prev;
	int size, pos;
} LockFreeMempoolChunk;

typedef struct {
	LockFreeMempoolChunk *current, *chunks;
} LockFreeMempool;

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

struct _MonoDomain {
	/*
	 * This lock must never be taken before the loader lock,
	 * i.e. if both are taken by the same thread, the loader lock
	 * must taken first.
	 */
	MonoCoopMutex    lock;
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
	MonoString         *empty_string;
	/* 
	 * The fields between FIRST_GC_TRACKED and LAST_GC_TRACKED are roots, but
	 * not object references.
	 */
#define MONO_DOMAIN_FIRST_GC_TRACKED env
	MonoGHashTable     *env;
	MonoGHashTable     *ldstr_table;
	/* hashtables for Reflection handles */
	MonoGHashTable     *type_hash;
	MonoConcGHashTable     *refobject_hash;
	/* maps class -> type initialization exception object */
	MonoGHashTable    *type_init_exception_hash;
	/* maps delegate trampoline addr -> delegate object */
	MonoGHashTable     *delegate_hash_table;
#define MONO_DOMAIN_LAST_GC_TRACKED delegate_hash_table
	guint32            state;
	/* Needed by Thread:GetDomainID() */
	gint32             domain_id;
	gint32             shadow_serial;
	GSList             *domain_assemblies;
	MonoAssembly       *entry_assembly;
	char               *friendly_name;
	GPtrArray          *class_vtable_array;
	/* maps remote class key -> MonoRemoteClass */
	GHashTable         *proxy_vtable_hash;
	/* Protected by 'jit_code_hash_lock' */
	MonoInternalHashTable jit_code_hash;
	mono_mutex_t    jit_code_hash_lock;
	int		    num_jit_info_table_duplicates;
	MonoJitInfoTable * 
	  volatile          jit_info_table;
	/*
	 * Contains information about AOT loaded code.
	 * Only used in the root domain.
	 */
	MonoJitInfoTable *
	  volatile          aot_modules;
	GSList		   *jit_info_free_queue;
	/* Used when loading assemblies */
	gchar **search_path;
	gchar *private_bin_path;
	LockFreeMempool *lock_free_mp;
	
	/* Used by remoting proxies */
	MonoMethod         *create_proxy_for_type_method;
	MonoMethod         *private_invoke_method;
	/* Used to store offsets of thread and context static fields */
	GHashTable         *special_static_fields;
	/* 
	 * This must be a GHashTable, since these objects can't be finalized
	 * if the hashtable contains a GC visible reference to them.
	 */
	GHashTable         *finalizable_objects_hash;

	/* Protects the three hashes above */
	mono_mutex_t   finalizable_objects_hash_lock;
	/* Used when accessing 'domain_assemblies' */
	mono_mutex_t    assemblies_lock;

	GHashTable	   *generic_virtual_cases;

	/* Information maintained by the JIT engine */
	gpointer runtime_info;
	
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
	MonoClassField *sockaddr_data_length_field;

	/* Cache function pointers for architectures  */
	/* that require wrappers */
	GHashTable *ftnptrs_hash;

	/* Maps MonoMethod* to weak links to DynamicMethod objects */
	GHashTable *method_to_dyn_method;

	/* <ThrowUnobservedTaskExceptions /> support */
	gboolean throw_unobserved_task_exceptions;

	guint32 execution_context_field_offset;
};

typedef struct  {
	guint16 major, minor, build, revision;
} AssemblyVersionSet;

/* MonoRuntimeInfo: Contains information about versions supported by this runtime */
typedef struct  {
	const char runtime_version [12];
	const char framework_version [4];
	const AssemblyVersionSet version_sets [5];
} MonoRuntimeInfo;

#define mono_domain_assemblies_lock(domain) mono_locks_os_acquire(&(domain)->assemblies_lock, DomainAssembliesLock)
#define mono_domain_assemblies_unlock(domain) mono_locks_os_release(&(domain)->assemblies_lock, DomainAssembliesLock)
#define mono_domain_jit_code_hash_lock(domain) mono_locks_os_acquire(&(domain)->jit_code_hash_lock, DomainJitCodeHashLock)
#define mono_domain_jit_code_hash_unlock(domain) mono_locks_os_release(&(domain)->jit_code_hash_lock, DomainJitCodeHashLock)

typedef MonoDomain* (*MonoLoadFunc) (const char *filename, const char *runtime_version);

void mono_domain_lock (MonoDomain *domain) MONO_LLVM_INTERNAL;
void mono_domain_unlock (MonoDomain *domain) MONO_LLVM_INTERNAL;

void
mono_install_runtime_load  (MonoLoadFunc func);

MonoDomain*
mono_runtime_load (const char *filename, const char *runtime_version);

typedef void (*MonoCreateDomainFunc) (MonoDomain *domain);

void
mono_install_create_domain_hook (MonoCreateDomainFunc func);

typedef void (*MonoFreeDomainFunc) (MonoDomain *domain);

void
mono_install_free_domain_hook (MonoFreeDomainFunc func);

void 
mono_cleanup (void);

void
mono_close_exe_image (void);

int
mono_jit_info_size (MonoJitInfoFlags flags, int num_clauses, int num_holes);

void
mono_jit_info_init (MonoJitInfo *ji, MonoMethod *method, guint8 *code, int code_size,
					MonoJitInfoFlags flags, int num_clauses, int num_holes);

MonoJitInfoTable *
mono_jit_info_table_new (MonoDomain *domain);

void
mono_jit_info_table_free (MonoJitInfoTable *table);

void
mono_jit_info_table_add    (MonoDomain *domain, MonoJitInfo *ji);

void
mono_jit_info_table_remove (MonoDomain *domain, MonoJitInfo *ji);

void
mono_jit_info_add_aot_module (MonoImage *image, gpointer start, gpointer end);

MonoGenericJitInfo*
mono_jit_info_get_generic_jit_info (MonoJitInfo *ji);

MonoGenericSharingContext*
mono_jit_info_get_generic_sharing_context (MonoJitInfo *ji);

void
mono_jit_info_set_generic_sharing_context (MonoJitInfo *ji, MonoGenericSharingContext *gsctx);

char *
mono_make_shadow_copy (const char *filename, MonoError *error);

gboolean
mono_is_shadow_copy_enabled (MonoDomain *domain, const gchar *dir_name);

gpointer
mono_domain_alloc  (MonoDomain *domain, guint size);

#define mono_domain_alloc(domain, size) (g_cast (mono_domain_alloc ((domain), (size))))

gpointer
mono_domain_alloc0 (MonoDomain *domain, guint size);

#define mono_domain_alloc0(domain, size) (g_cast (mono_domain_alloc0 ((domain), (size))))

gpointer
mono_domain_alloc0_lock_free (MonoDomain *domain, guint size);

#define mono_domain_alloc0_lock_free(domain, size) (g_cast (mono_domain_alloc0_lock_free ((domain), (size))))

void*
mono_domain_code_reserve (MonoDomain *domain, int size) MONO_LLVM_INTERNAL;

#define mono_domain_code_reserve(domain, size) (g_cast (mono_domain_code_reserve ((domain), (size))))

void*
mono_domain_code_reserve_align (MonoDomain *domain, int size, int alignment);

#define mono_domain_code_reserve_align(domain, size, align) (g_cast (mono_domain_code_reserve_align ((domain), (size), (align))))

void
mono_domain_code_commit (MonoDomain *domain, void *data, int size, int newsize);

void
mono_domain_code_foreach (MonoDomain *domain, MonoCodeManagerFunc func, void *user_data);

void
mono_domain_unset (void);

void
mono_domain_set_internal_with_options (MonoDomain *domain, gboolean migrate_exception);

gboolean
mono_domain_set_config_checked (MonoDomain *domain, const char *base_dir, const char *config_file_name, MonoError *error);

MonoTryBlockHoleTableJitInfo*
mono_jit_info_get_try_block_hole_table_info (MonoJitInfo *ji);

MonoArchEHJitInfo*
mono_jit_info_get_arch_eh_info (MonoJitInfo *ji);

MonoThunkJitInfo*
mono_jit_info_get_thunk_info (MonoJitInfo *ji);

MonoUnwindJitInfo*
mono_jit_info_get_unwind_info (MonoJitInfo *ji);

/* 
 * Installs a new function which is used to return a MonoJitInfo for a method inside
 * an AOT module.
 */
typedef MonoJitInfo *(*MonoJitInfoFindInAot)         (MonoDomain *domain, MonoImage *image, gpointer addr);
void          mono_install_jit_info_find_in_aot (MonoJitInfoFindInAot func);

void
mono_jit_code_hash_init (MonoInternalHashTable *jit_code_hash);

MonoAssembly *
mono_assembly_load_corlib (const MonoRuntimeInfo *runtime, MonoImageOpenStatus *status);

const MonoRuntimeInfo*
mono_get_runtime_info (void);

void
mono_runtime_set_no_exec (gboolean val);

gboolean
mono_runtime_get_no_exec (void);

void
mono_domain_parse_assembly_bindings (MonoDomain *domain, int amajor, int aminor, gchar *domain_config_file_name);

gboolean
mono_assembly_name_parse (const char *name, MonoAssemblyName *aname);

MonoImage *mono_assembly_open_from_bundle (const char *filename,
					   MonoImageOpenStatus *status,
					   gboolean refonly);

MonoAssembly *
mono_try_assembly_resolve (MonoDomain *domain, const char *fname, MonoAssembly *requesting, gboolean refonly, MonoError *error);

MonoAssembly *
mono_domain_assembly_postload_search (MonoAssemblyName *aname, MonoAssembly *requesting, gboolean refonly);

void mono_domain_set_options_from_config (MonoDomain *domain);

int mono_framework_version (void);

void mono_reflection_cleanup_domain (MonoDomain *domain);

void mono_assembly_cleanup_domain_bindings (guint32 domain_id);

MonoJitInfo* mono_jit_info_table_find_internal (MonoDomain *domain, gpointer addr, gboolean try_aot, gboolean allow_trampolines);

void mono_enable_debug_domain_unload (gboolean enable);

void
mono_runtime_init_checked (MonoDomain *domain, MonoThreadStartCB start_cb, MonoThreadAttachCB attach_cb, MonoError *error);

void
mono_context_init_checked (MonoDomain *domain, MonoError *error);

gboolean
mono_assembly_has_reference_assembly_attribute (MonoAssembly *assembly, MonoError *error);

GPtrArray*
mono_domain_get_assemblies (MonoDomain *domain, gboolean refonly);

void
mono_runtime_register_appctx_properties (int nprops, const char **keys,  const char **values);

void
mono_runtime_install_appctx_properties (void);

gboolean 
mono_domain_set_fast (MonoDomain *domain, gboolean force);

#endif /* __MONO_METADATA_DOMAIN_INTERNALS_H__ */
