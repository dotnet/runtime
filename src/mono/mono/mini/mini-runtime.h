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

struct MonoJitTlsData {
	gpointer          end_of_stack;
	guint32           stack_size;
	MonoLMF          *lmf;
	MonoLMF          *first_lmf;
	gpointer         restore_stack_prot;
	guint32          handling_stack_ovf;
	gpointer         signal_stack;
	guint32          signal_stack_size;
	gpointer         stack_ovf_guard_base;
	guint32          stack_ovf_guard_size;
	guint            stack_ovf_valloced : 1;
	void            (*abort_func) (MonoObject *object);
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
	 * Stores if we need to run a chained exception in Windows.
	 */
	gboolean mono_win_chained_exception_needs_run;

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

/*
 * This structure is an extension of MonoLMF and contains extra information.
 */
typedef struct {
	struct MonoLMF lmf;
	gboolean debugger_invoke;
	gboolean interp_exit;
	MonoContext ctx; /* if debugger_invoke is TRUE */
	/* If interp_exit is TRUE */
	gpointer interp_exit_data;
} MonoLMFExt;

/* main function */
MONO_API int         mono_main                      (int argc, char* argv[]);
MONO_API void        mono_set_defaults              (int verbose_level, guint32 opts);
MONO_API void        mono_parse_env_options         (int *ref_argc, char **ref_argv []);
MONO_API char       *mono_parse_options_from        (const char *options, int *ref_argc, char **ref_argv []);

/* actual definition in interp.h */
typedef struct _MonoInterpCallbacks MonoInterpCallbacks;

void                   mono_interp_stub_init         (void);
void                   mini_install_interp_callbacks (MonoInterpCallbacks *cbs);
MonoInterpCallbacks*   mini_get_interp_callbacks     (void);

MonoDomain* mini_init                      (const char *filename, const char *runtime_version);
void        mini_cleanup                   (MonoDomain *domain);
MONO_API MonoDebugOptions *mini_get_debug_options   (void);
MONO_API gboolean    mini_parse_debug_option (const char *option);

void      mini_jit_init                    (void);
void      mini_jit_cleanup                 (void);
void      mono_disable_optimizations       (guint32 opts);
void      mono_set_optimizations           (guint32 opts);
void      mono_precompile_assemblies        (void);
MONO_API int       mono_parse_default_optimizations  (const char* p);
gboolean          mono_running_on_valgrind (void);

MonoLMF * mono_get_lmf                      (void);
MonoLMF** mono_get_lmf_addr                 (void);
void      mono_set_lmf                      (MonoLMF *lmf);
void      mono_push_lmf                     (MonoLMFExt *ext);
void      mono_pop_lmf                      (MonoLMF *lmf);
MonoJitTlsData* mono_get_jit_tls            (void);
MONO_API MonoDomain* mono_jit_thread_attach (MonoDomain *domain);
MONO_API void      mono_jit_set_domain      (MonoDomain *domain);

#endif /* __MONO_MINI_RUNTIME_H__ */

