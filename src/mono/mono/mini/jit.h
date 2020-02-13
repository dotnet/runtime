/**
 * \file
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001, 2002, 2003 Ximian, Inc.
 */

#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include <mono/metadata/appdomain.h>

MONO_BEGIN_DECLS

MONO_API MONO_RT_EXTERNAL_ONLY MonoDomain * 
mono_jit_init              (const char *file);

MONO_API MONO_RT_EXTERNAL_ONLY MonoDomain * 
mono_jit_init_version      (const char *root_domain_name, const char *runtime_version);

MONO_API MonoDomain * 
mono_jit_init_version_for_test_only      (const char *root_domain_name, const char *runtime_version);

MONO_API int
mono_jit_exec              (MonoDomain *domain, MonoAssembly *assembly, 
			    int argc, char *argv[]);
MONO_API void        
mono_jit_cleanup           (MonoDomain *domain);

MONO_API mono_bool
mono_jit_set_trace_options (const char* options);

MONO_API void
mono_set_signal_chaining   (mono_bool chain_signals);

MONO_API void
mono_set_crash_chaining   (mono_bool chain_signals);

/**
 * This function is deprecated, use mono_jit_set_aot_mode instead.
 */
MONO_API void
mono_jit_set_aot_only      (mono_bool aot_only);

/**
 * Allows control over our AOT (Ahead-of-time) compilation mode.
 */
typedef enum {
	/* Disables AOT mode */
	MONO_AOT_MODE_NONE,
	/* Enables normal AOT mode, equivalent to mono_jit_set_aot_only (false) */
	MONO_AOT_MODE_NORMAL,
	/* Enables hybrid AOT mode, JIT can still be used for wrappers */
	MONO_AOT_MODE_HYBRID,
	/* Enables full AOT mode, JIT is disabled and not allowed,
	 * equivalent to mono_jit_set_aot_only (true) */
	MONO_AOT_MODE_FULL,
	/* Same as full, but use only llvm compiled code */
	MONO_AOT_MODE_LLVMONLY,
	/* Uses Interpreter, JIT is disabled and not allowed,
	 * equivalent to "--full-aot --interpreter" */
	MONO_AOT_MODE_INTERP,
	/* Same as INTERP, but use only llvm compiled code */
	MONO_AOT_MODE_INTERP_LLVMONLY,
	/* Use only llvm compiled code, fall back to the interpeter */
	MONO_AOT_MODE_LLVMONLY_INTERP,
	/* Sentinel value used internally by the runtime. We use a large number to avoid clashing with some internal values. */
	MONO_AOT_MODE_LAST = 1000,
} MonoAotMode;

MONO_API void
mono_jit_set_aot_mode      (MonoAotMode mode);

/*
 * Returns whether the runtime was invoked for the purpose of AOT-compiling an
 * assembly, i.e. no managed code will run.
 */
MONO_API mono_bool
mono_jit_aot_compiling (void);

/* Allow embedders to decide wherther to actually obey breakpoint instructions
 * in specific methods (works for both break IL instructions and Debugger.Break ()
 * method calls).
 */
typedef enum {
	/* the default is to always obey the breakpoint */
	MONO_BREAK_POLICY_ALWAYS,
	/* a nop is inserted instead of a breakpoint */
	MONO_BREAK_POLICY_NEVER,
	/* the breakpoint is executed only if the program has ben started under
	 * the debugger (that is if a debugger was attached at the time the method
	 * was compiled).
	 */
	MONO_BREAK_POLICY_ON_DBG
} MonoBreakPolicy;

typedef MonoBreakPolicy (*MonoBreakPolicyFunc) (MonoMethod *method);
MONO_API void mono_set_break_policy (MonoBreakPolicyFunc policy_callback);

MONO_API void
mono_jit_parse_options     (int argc, char * argv[]);

MONO_API char*       mono_get_runtime_build_info    (void);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_set_use_llvm (mono_bool use_llvm);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_aot_register_module (void **aot_info);

MONO_API MONO_RT_EXTERNAL_ONLY
MonoDomain* mono_jit_thread_attach (MonoDomain *domain);


MONO_END_DECLS

#endif

