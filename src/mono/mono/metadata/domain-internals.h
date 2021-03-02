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

G_BEGIN_DECLS

/*
 * If this is set, the memory belonging to appdomains is not freed when a domain is
 * unloaded, and assemblies loaded by the appdomain are not unloaded either. This
 * allows us to use typed gc in non-default appdomains too, leading to increased
 * performance.
 */ 
extern gboolean mono_dont_free_domains;

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
	MonoGCHandle gc_handle;
} ContextStaticData;

struct _MonoAppContext {
	MonoObject obj;
	gint32 domain_id;
	gint32 context_id;
	gpointer *static_data;
	ContextStaticData *data;
};

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

	/*
	 * keep all the managed objects close to each other for the precise GC
	 * For the Boehm GC we additionally keep close also other GC-tracked pointers.
	 */
#define MONO_DOMAIN_FIRST_OBJECT domain
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
#define MONO_DOMAIN_LAST_GC_TRACKED ldstr_table
	guint32            state;
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
	MonoAssembly       *entry_assembly;
	char               *friendly_name;
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
	
	/* Used by remoting proxies */
	MonoMethod         *create_proxy_for_type_method;
	MonoMethod         *private_invoke_method;
	/* Used to store offsets of thread and context static fields */
	GHashTable         *special_static_fields;
	/* 
	 * This must be a GHashTable, since these objects can't be finalized
	 * if the hashtable contains a GC visible reference to them.
	 */
	GHashTable         *finalizable_objects_hash; // TODO: this needs to be moved for unloadability with non-sgen gc

	/* Protects the three hashes above */
	mono_mutex_t   finalizable_objects_hash_lock;
	/* Used when accessing 'domain_assemblies' */
	MonoCoopMutex  assemblies_lock;

	/* Contains the compiled runtime invoke wrapper used by finalizers */
	gpointer            finalize_runtime_invoke;

	/* Cache function pointers for architectures  */
	/* that require wrappers */
	GHashTable *ftnptrs_hash; // TODO: need to move?

	/* Maps MonoMethod* to weak links to DynamicMethod objects */
	GHashTable *method_to_dyn_method;

	/* <ThrowUnobservedTaskExceptions /> support */
	gboolean throw_unobserved_task_exceptions;

	guint32 execution_context_field_offset;

	GSList *alcs;
	MonoAssemblyLoadContext *default_alc;
	MonoCoopMutex alcs_lock; /* Used when accessing 'alcs' */
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

#define mono_domain_jit_code_hash_lock(domain) mono_locks_os_acquire(&(domain)->jit_code_hash_lock, DomainJitCodeHashLock)
#define mono_domain_jit_code_hash_unlock(domain) mono_locks_os_release(&(domain)->jit_code_hash_lock, DomainJitCodeHashLock)

typedef MonoDomain* (*MonoLoadFunc) (const char *filename, const char *runtime_version);

void mono_domain_lock (MonoDomain *domain);
void mono_domain_unlock (MonoDomain *domain);

void
mono_install_runtime_load  (MonoLoadFunc func);

MonoDomain*
mono_runtime_load (const char *filename, const char *runtime_version);

void
mono_runtime_quit_internal (void);

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

void
mono_domain_unset (void);

void
mono_domain_set_internal_with_options (MonoDomain *domain, gboolean migrate_exception);

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

void
mono_context_init_checked (MonoDomain *domain, MonoError *error);

gboolean
mono_assembly_has_reference_assembly_attribute (MonoAssembly *assembly, MonoError *error);

GPtrArray*
mono_domain_get_assemblies (MonoDomain *domain);

void
mono_runtime_register_appctx_properties (int nprops, const char **keys,  const char **values);

void
mono_runtime_install_appctx_properties (void);

gboolean 
mono_domain_set_fast (MonoDomain *domain, gboolean force);

MonoAssemblyLoadContext *
mono_domain_default_alc (MonoDomain *domain);

static inline void
mono_domain_alcs_lock (MonoDomain *domain)
{
	mono_coop_mutex_lock (&domain->alcs_lock);
}

static inline void
mono_domain_alcs_unlock (MonoDomain *domain)
{
	mono_coop_mutex_unlock (&domain->alcs_lock);
}

static inline
MonoAssemblyLoadContext *
mono_domain_ambient_alc (MonoDomain *domain)
{
	/*
	 * FIXME: All the callers of mono_domain_ambient_alc should get an ALC
	 * passed to them from their callers.
	 */
	return mono_domain_default_alc (domain);
}

static inline MonoMemoryManager *
mono_domain_memory_manager (MonoDomain *domain)
{
	return (MonoMemoryManager *)mono_domain_default_alc (domain)->memory_manager;
}

static inline MonoMemoryManager *
mono_domain_ambient_memory_manager (MonoDomain *domain)
{
	// FIXME: All callers of mono_domain_ambient_memory_manager should get a MemoryManager from their callers or context
	return mono_domain_memory_manager (domain);
}

G_END_DECLS

#endif /* __MONO_METADATA_DOMAIN_INTERNALS_H__ */
