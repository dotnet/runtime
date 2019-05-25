/**
 * \file
 *
 *   Runtime declarations for the JIT.
 *
 * Copyright 2002-2003 Ximian Inc
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_MINI_RUNTIME_H__
#define __MONO_MINI_RUNTIME_H__

#include "mini.h"
#include "ee.h"

/* Per-domain information maintained by the JIT */
typedef struct
{
	/* Maps MonoMethod's to a GSList of GOT slot addresses pointing to its code */
	GHashTable *jump_target_got_slot_hash;
	GHashTable *jump_target_hash;
	/* Maps methods/klasses to the address of the given type of trampoline */
	GHashTable *class_init_trampoline_hash;
	GHashTable *jump_trampoline_hash;
	GHashTable *jit_trampoline_hash;
	GHashTable *delegate_trampoline_hash;
	/* Maps ClassMethodPair -> MonoDelegateTrampInfo */
	GHashTable *static_rgctx_trampoline_hash;
	GHashTable *llvm_vcall_trampoline_hash;
	/* maps MonoMethod -> MonoJitDynamicMethodInfo */
	GHashTable *dynamic_code_hash;
	GHashTable *method_code_hash;
	/* Maps methods to a RuntimeInvokeInfo structure, protected by the associated MonoDomain lock */
	MonoConcurrentHashTable *runtime_invoke_hash;
	/* Maps MonoMethod to a GPtrArray containing sequence point locations */
	/* Protected by the domain lock */
	GHashTable *seq_points;
	/* Debugger agent data */
	gpointer agent_info;
	/* Maps MonoMethod to an arch-specific structure */
	GHashTable *arch_seq_points;
	/* Maps a GSharedVtTrampInfo structure to a trampoline address */
	GHashTable *gsharedvt_arg_tramp_hash;
	/* memcpy/bzero methods specialized for small constant sizes */
	gpointer *memcpy_addr [17];
	gpointer *bzero_addr [17];
	gpointer llvm_module;
	/* Maps MonoMethod -> GSlist of addresses */
	GHashTable *llvm_jit_callees;
	/* Maps MonoMethod -> RuntimeMethod */
	MonoInternalHashTable interp_code_hash;
	/* Maps MonoMethod -> 	MonoMethodRuntimeGenericContext */
	GHashTable *mrgctx_hash;
	GHashTable *method_rgctx_hash;
	/* Maps gpointer -> InterpMethod */
	GHashTable *interp_method_pointer_hash;
} MonoJitDomainInfo;

#define domain_jit_info(domain) ((MonoJitDomainInfo*)((domain)->runtime_info))

/*
 * Stores state need to resume exception handling when using LLVM
 */
typedef struct {
	MonoJitInfo *ji;
	int clause_index;
	MonoContext ctx, new_ctx;
	/* FIXME: GC */
	gpointer        ex_obj;
	MonoLMF *lmf;
	int first_filter_idx, filter_idx;
} ResumeState;

typedef void (*MonoAbortFunction)(MonoObject*);

struct MonoJitTlsData {
	gpointer          end_of_stack;
	guint32           stack_size;
	MonoLMF          *lmf;
	MonoLMF          *first_lmf;
	guint            handling_stack_ovf : 1;
	gpointer         signal_stack;
	guint32          signal_stack_size;
	gpointer         stack_ovf_guard_base;
	guint32          stack_ovf_guard_size;
	guint            stack_ovf_valloced : 1;
	guint            stack_ovf_pending : 1;
	MonoAbortFunction abort_func;
	/* Used to implement --debug=casts */
	MonoClass       *class_cast_from, *class_cast_to;

	/* Stores state needed by handler block with a guard */
	MonoContext     ex_ctx;
	ResumeState resume_state;

	/* handler block been guarded. It's safe to store this even for dynamic methods since there
	is an activation on stack making sure it will remain alive.*/
	MonoJitExceptionInfo *handler_block;

	/* context to be used by the guard trampoline when resuming interruption.*/
	MonoContext handler_block_context;
	/* 
	 * Stores the state at the exception throw site to be used by mono_stack_walk ()
	 * when it is called from profiler functions during exception handling.
	 */
	MonoContext orig_ex_ctx;
	gboolean orig_ex_ctx_set;

	/* 
	 * The current exception in flight
	 */
	guint32 thrown_exc;
	/*
	 * If the current exception is not a subclass of Exception,
	 * the original exception.
	 */
	guint32 thrown_non_exc;

	/*
	 * The calling assembly in llvmonly mode.
	 */
	MonoImage *calling_image;

	/*
	 * The stack frame "high water mark" for ThreadAbortExceptions.
	 * We will rethrow the exception upon exiting a catch clause that's
	 * in a function stack frame above the water mark(isn't being called by
	 * the catch block that caught the ThreadAbortException).
	 */
	gpointer abort_exc_stack_threshold;

	/*
	 * List of methods being JIT'd in the current thread.
	 */
	int active_jit_methods;

	gpointer interp_context;

#if defined(TARGET_WIN32)
	MonoContext stack_restore_ctx;
#endif
};

#define MONO_LMFEXT_DEBUGGER_INVOKE 1
#define MONO_LMFEXT_INTERP_EXIT 2
#define MONO_LMFEXT_INTERP_EXIT_WITH_CTX 3

/*
 * This structure is an extension of MonoLMF and contains extra information.
 */
typedef struct {
	struct MonoLMF lmf;
	int kind;
	MonoContext ctx; /* valid if kind == DEBUGGER_INVOKE || kind == INTERP_EXIT_WITH_CTX */
	gpointer interp_exit_data; /* valid if kind == INTERP_EXIT || kind == INTERP_EXIT_WITH_CTX */
} MonoLMFExt;

typedef void (*MonoFtnPtrEHCallback) (guint32 gchandle);

typedef struct MonoDebugOptions {
	gboolean handle_sigint;
	gboolean keep_delegates;
	gboolean reverse_pinvoke_exceptions;
	gboolean collect_pagefault_stats;
	gboolean break_on_unverified;
	gboolean better_cast_details;
	gboolean mdb_optimizations;
	gboolean no_gdb_backtrace;
	gboolean suspend_on_native_crash;
	gboolean suspend_on_exception;
	gboolean suspend_on_unhandled;
	gboolean dyn_runtime_invoke;
	gboolean gdb;
	gboolean lldb;

	/*
	 * With LLVM codegen, this option will cause methods to be called indirectly through the
	 * PLT (As they are in other FullAOT modes, without LLVM). 
	 *
	 * Enable this to debug problems with direct calls in llvm
	 */
	gboolean llvm_disable_self_init;
	gboolean use_fallback_tls;
	/*
	 * Whenever data such as next sequence points and flags is required.
	 * Next sequence points and flags are required by the debugger agent.
	 */
	gboolean gen_sdb_seq_points;
	gboolean no_seq_points_compact_data;
	/*
	 * Setting single_imm_size should guarantee that each time managed code is compiled
	 * the same instructions and registers are used, regardless of the size of used values.
	 */
	gboolean single_imm_size;
	gboolean explicit_null_checks;
	/*
	 * Fill stack frames with 0x2a in method prologs. This helps with the
	 * debugging of the stack marking code in the GC.
	 */
	gboolean init_stacks;

	/*
	 * Whenever to implement single stepping and breakpoints without signals in the
	 * soft debugger. This is useful on platforms without signals, like the ps3, or during
	 * runtime debugging, since it avoids SIGSEGVs when a single step location or breakpoint
	 * is hit.
	 */
	gboolean soft_breakpoints;
	/*
	 * Whenever to break in the debugger using G_BREAKPOINT on unhandled exceptions.
	 */
	gboolean break_on_exc;
	/*
	 * Load AOT JIT info eagerly.
	 */
	gboolean load_aot_jit_info_eagerly;
	/*
	 * Check for pinvoke calling convention mismatches.
	 */
	gboolean check_pinvoke_callconv;
	/*
	 * Translate Debugger.Break () into a native breakpoint signal
	 */
	gboolean native_debugger_break;
	/*
	 * Disabling the frame pointer emit optimization can allow debuggers to more easily
	 * identify the stack on some platforms
	 */
	gboolean disable_omit_fp;
	/*
	 * Make gdb output on native crashes more verbose.
	 */
	gboolean verbose_gdb;

	// Internal testing feature.
	gboolean test_tailcall_require;

	/*
	 * Internal testing feature
	 * Testing feature, skip loading the Nth aot loadable method.
	 */
	gboolean aot_skip_set;
	int aot_skip;
} MonoDebugOptions;


/*
 * We need to store the image which the token refers to along with the token,
 * since the image might not be the same as the image of the method which
 * contains the relocation, because of inlining.
 */
typedef struct MonoJumpInfoToken {
	MonoImage *image;
	guint32 token;
	gboolean has_context;
	MonoGenericContext context;
} MonoJumpInfoToken;

typedef struct MonoJumpInfoBBTable {
	MonoBasicBlock **table;
	int table_size;
} MonoJumpInfoBBTable;

/* Contains information describing an LLVM IMT trampoline */
typedef struct MonoJumpInfoImtTramp {
	MonoMethod *method;
	int vt_offset;
} MonoJumpInfoImtTramp;

/*
 * Contains information for computing the
 * property given by INFO_TYPE of the runtime
 * object described by DATA.
 */
struct MonoJumpInfoRgctxEntry {
	union {
		/* If in_mrgctx is TRUE */
		MonoMethod *method;
		/* If in_mrgctx is FALSE */
		MonoClass *klass;
	} d;
	gboolean in_mrgctx;
	MonoJumpInfo *data; /* describes the data to be loaded */
	MonoRgctxInfoType info_type;
};

/* Contains information about a gsharedvt call */
struct MonoJumpInfoGSharedVtCall {
	/* The original signature of the call */
	MonoMethodSignature *sig;
	/* The method which is called */
	MonoMethod *method;
};

/*
 * Represents the method which is called when a virtual call is made to METHOD
 * on a receiver of type KLASS.
 */
typedef struct {
	/* Receiver class */
	MonoClass *klass;
	/* Virtual method */
	MonoMethod *method;
} MonoJumpInfoVirtMethod;

struct MonoJumpInfo {
	MonoJumpInfo *next;
	/* Relocation type for patching */
	int relocation;
	union {
		int i;
		guint8 *p;
		MonoInst *label;
	} ip;

	MonoJumpInfoType type;
	union {
		gconstpointer   target;
		int index;
		guint uindex;
		MonoBasicBlock *bb;
		MonoInst       *inst;
		MonoMethod     *method;
		MonoClass      *klass;
		MonoClassField *field;
		MonoImage      *image;
		MonoVTable     *vtable;
		const char     *name;
		MonoJitICallId jit_icall_id; // Or just use index?
		MonoJumpInfoToken  *token;
		MonoJumpInfoBBTable *table;
		MonoJumpInfoRgctxEntry *rgctx_entry;
		MonoJumpInfoImtTramp *imt_tramp;
		MonoJumpInfoGSharedVtCall *gsharedvt;
		MonoGSharedVtMethodInfo *gsharedvt_method;
		MonoMethodSignature *sig;
		MonoDelegateClassMethodPair *del_tramp;
		/* MONO_PATCH_INFO_VIRT_METHOD */
		MonoJumpInfoVirtMethod *virt_method;
	} data;
};

extern gboolean mono_break_on_exc;
extern gboolean mono_compile_aot;
extern gboolean mono_aot_only;
extern gboolean mono_llvm_only;
extern MonoAotMode mono_aot_mode;
MONO_API_DATA const char *mono_build_date;
extern gboolean mono_do_signal_chaining;
extern gboolean mono_do_crash_chaining;
MONO_API_DATA gboolean mono_use_llvm;
MONO_API_DATA gboolean mono_use_interpreter;
extern const char* mono_interp_opts_string;
extern gboolean mono_do_single_method_regression;
extern guint32 mono_single_method_regression_opt;
extern MonoMethod *mono_current_single_method;
extern GSList *mono_single_method_list;
extern GHashTable *mono_single_method_hash;
extern GList* mono_aot_paths;
extern MonoDebugOptions mini_debug_options;
extern GSList *mono_interp_only_classes;
extern char *sdb_options;

/*
This struct describes what execution engine feature to use.
This subsume, and will eventually sunset, mono_aot_only / mono_llvm_only and friends.
The goal is to transition us to a place were we can more easily compose/describe what features we need for a given execution mode.

A good feature flag is checked alone, a bad one described many things and keeps breaking some of the modes
*/
typedef struct {
	/*
	 * If true, trampolines are to be fetched from the AOT runtime instead of JIT compiled
	 */
	gboolean use_aot_trampolines;

	/*
	 * If true, the runtime will try to use the interpreter before looking for compiled code.
	 */
	gboolean force_use_interpreter;
} MonoEEFeatures;

extern MonoEEFeatures mono_ee_features;

//XXX this enum *MUST extend MonoAotMode as they are consumed together.
typedef enum {
	/* Always execute with interp, will use JIT to produce trampolines */
	MONO_EE_MODE_INTERP = MONO_AOT_MODE_LAST,
} MonoEEMode;


static inline MonoMethod*
jinfo_get_method (MonoJitInfo *ji)
{
	return mono_jit_info_get_method (ji);
}

/* main function */
MONO_API int         mono_main                      (int argc, char* argv[]);
MONO_API void        mono_set_defaults              (int verbose_level, guint32 opts);
MONO_API void        mono_parse_env_options         (int *ref_argc, char **ref_argv []);
MONO_API char       *mono_parse_options_from        (const char *options, int *ref_argc, char **ref_argv []);
MONO_API int         mono_regression_test_step      (int verbose_level, const char *image, const char *method_name);


void                   mono_interp_stub_init         (void);
void                   mini_install_interp_callbacks (MonoEECallbacks *cbs);
MonoEECallbacks*       mini_get_interp_callbacks     (void);

typedef struct _MonoDebuggerCallbacks MonoDebuggerCallbacks;

void                   mini_install_dbg_callbacks (MonoDebuggerCallbacks *cbs);
MonoDebuggerCallbacks  *mini_get_dbg_callbacks (void);

MonoDomain* mini_init                      (const char *filename, const char *runtime_version);
void        mini_cleanup                   (MonoDomain *domain);
MONO_API MonoDebugOptions *mini_get_debug_options   (void);
MONO_API gboolean    mini_parse_debug_option (const char *option);

MONO_API void
mono_install_ftnptr_eh_callback (MonoFtnPtrEHCallback callback);

void      mini_jit_init                    (void);
void      mini_jit_cleanup                 (void);
void      mono_disable_optimizations       (guint32 opts);
void      mono_set_optimizations           (guint32 opts);
void      mono_precompile_assemblies        (void);
MONO_API int       mono_parse_default_optimizations  (const char* p);
gboolean          mono_running_on_valgrind (void);

MonoLMF * mono_get_lmf                      (void);
#define mono_get_lmf_addr mono_tls_get_lmf_addr
MonoLMF** mono_get_lmf_addr                 (void);
void      mono_set_lmf                      (MonoLMF *lmf);
void      mono_push_lmf                     (MonoLMFExt *ext);
void      mono_pop_lmf                      (MonoLMF *lmf);
#define mono_get_jit_tls mono_tls_get_jit_tls
MonoJitTlsData* mono_get_jit_tls            (void);
MONO_API MONO_RT_EXTERNAL_ONLY
MonoDomain* mono_jit_thread_attach (MonoDomain *domain);
MONO_API void      mono_jit_set_domain      (MonoDomain *domain);

gboolean  mono_method_same_domain           (MonoJitInfo *caller, MonoJitInfo *callee);
gpointer  mono_create_ftnptr                (MonoDomain *domain, gpointer addr);
MonoMethod* mono_icall_get_wrapper_method    (MonoJitICallInfo* callinfo) MONO_LLVM_INTERNAL;
gconstpointer     mono_icall_get_wrapper       (MonoJitICallInfo* callinfo) MONO_LLVM_INTERNAL;
gconstpointer     mono_icall_get_wrapper_full  (MonoJitICallInfo* callinfo, gboolean do_compile) MONO_LLVM_INTERNAL;

MonoJumpInfo* mono_patch_info_dup_mp        (MonoMemPool *mp, MonoJumpInfo *patch_info);
guint     mono_patch_info_hash (gconstpointer data) MONO_LLVM_INTERNAL;
gint      mono_patch_info_equal (gconstpointer ka, gconstpointer kb) MONO_LLVM_INTERNAL;
MonoJumpInfo *mono_patch_info_list_prepend  (MonoJumpInfo *list, int ip, MonoJumpInfoType type, gconstpointer target);
MonoJumpInfoToken* mono_jump_info_token_new (MonoMemPool *mp, MonoImage *image, guint32 token);
MonoJumpInfoToken* mono_jump_info_token_new2 (MonoMemPool *mp, MonoImage *image, guint32 token, MonoGenericContext *context);
gpointer  mono_resolve_patch_target         (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *patch_info, gboolean run_cctors, MonoError *error) MONO_LLVM_INTERNAL;
void mini_register_jump_site                (MonoDomain *domain, MonoMethod *method, gpointer ip);
void mini_patch_jump_sites                  (MonoDomain *domain, MonoMethod *method, gpointer addr);
void mini_patch_llvm_jit_callees            (MonoDomain *domain, MonoMethod *method, gpointer addr);
gpointer  mono_jit_search_all_backends_for_jit_info (MonoDomain *domain, MonoMethod *method, MonoJitInfo **ji);
gpointer  mono_jit_find_compiled_method_with_jit_info (MonoDomain *domain, MonoMethod *method, MonoJitInfo **ji);
gpointer  mono_jit_find_compiled_method     (MonoDomain *domain, MonoMethod *method);
gpointer  mono_jit_compile_method           (MonoMethod *method, MonoError *error);
gpointer  mono_jit_compile_method_jit_only  (MonoMethod *method, MonoError *error);

void      mono_set_bisect_methods          (guint32 opt, const char *method_list_filename);
guint32   mono_get_optimizations_for_method (MonoMethod *method, guint32 default_opt);
char*     mono_opt_descr                   (guint32 flags);
void      mono_set_verbose_level           (guint32 level);
const char*mono_ji_type_to_string           (MonoJumpInfoType type) MONO_LLVM_INTERNAL;
void      mono_print_ji                     (const MonoJumpInfo *ji);
MONO_API void      mono_print_method_from_ip         (void *ip);
MONO_API char     *mono_pmip                         (void *ip);
MONO_API int mono_ee_api_version (void);
gboolean  mono_debug_count                  (void);

#ifdef __linux__
#define XDEBUG_ENABLED 1
#endif

#ifdef __linux__
/* maybe enable also for other systems? */
#define ENABLE_JIT_MAP 1
void mono_enable_jit_map (void);
void mono_emit_jit_map   (MonoJitInfo *jinfo);
void mono_emit_jit_tramp (void *start, int size, const char *desc);
gboolean mono_jit_map_is_enabled (void);
#else
#define mono_enable_jit_map()
#define mono_emit_jit_map(ji)
#define mono_emit_jit_tramp(s,z,d)
#define mono_jit_map_is_enabled() (0)
#endif

/*
 * Per-OS implementation functions.
 */
void
mono_runtime_install_handlers (void);

gboolean
mono_runtime_install_custom_handlers (const char *handlers);

void
mono_runtime_install_custom_handlers_usage (void);

void
mono_runtime_cleanup_handlers (void);

void
mono_runtime_setup_stat_profiler (void);

void
mono_runtime_shutdown_stat_profiler (void);

void
mono_runtime_posix_install_handlers (void);

void
mono_gdb_render_native_backtraces (pid_t crashed_pid);

void
mono_cross_helpers_run (void);

void
mono_init_native_crash_info (void);

void
mono_cleanup_native_crash_info (void);

void
mono_dump_native_crash_info (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info);

void
mono_post_native_crash_handler (const char *signal, MonoContext *mctx, MONO_SIG_HANDLER_INFO_TYPE *info, gboolean crash_chaining);

/*
 * Signal handling
 */

#if defined(DISABLE_HW_TRAPS) || defined(MONO_ARCH_DISABLE_HW_TRAPS)
 // Signal handlers not available
#define MONO_ARCH_NEED_DIV_CHECK 1
#endif

void MONO_SIG_HANDLER_SIGNATURE (mono_sigfpe_signal_handler) ;
void MONO_SIG_HANDLER_SIGNATURE (mono_sigill_signal_handler) ;
void MONO_SIG_HANDLER_SIGNATURE (mono_sigsegv_signal_handler);
void MONO_SIG_HANDLER_SIGNATURE (mono_sigint_signal_handler) ;
gboolean MONO_SIG_HANDLER_SIGNATURE (mono_chain_signal);

#if defined (HOST_WASM)

#define MONO_RETURN_ADDRESS_N(N) NULL
#define MONO_RETURN_ADDRESS() MONO_RETURN_ADDRESS_N(0)


#elif defined (__GNUC__)

#define MONO_RETURN_ADDRESS_N(N) (__builtin_extract_return_addr (__builtin_return_address (N)))
#define MONO_RETURN_ADDRESS() MONO_RETURN_ADDRESS_N(0)

#elif defined(_MSC_VER)

#include <intrin.h>
#pragma intrinsic(_ReturnAddress)

#define MONO_RETURN_ADDRESS() _ReturnAddress()
#define MONO_RETURN_ADDRESS_N(N) NULL

#else

#error "Missing return address intrinsics implementation"

#endif

//have a global view of sdb disable
#if !defined(MONO_ARCH_SOFT_DEBUG_SUPPORTED) || defined (DISABLE_DEBUGGER_AGENT)
#define DISABLE_SDB 1
#endif

void mini_register_sigterm_handler (void);

#endif /* __MONO_MINI_RUNTIME_H__ */

