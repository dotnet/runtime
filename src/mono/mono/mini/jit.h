/*
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001, 2002, 2003 Ximian, Inc.
 */

#ifndef _MONO_JIT_JIT_H_
#define _MONO_JIT_JIT_H_

#include <mono/metadata/appdomain.h>

MONO_BEGIN_DECLS

MONO_API MonoDomain * 
mono_jit_init              (const char *file);

MONO_API MonoDomain * 
mono_jit_init_version      (const char *root_domain_name, const char *runtime_version);

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

MONO_API void
mono_jit_set_aot_only      (mono_bool aot_only);

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

/* The following APIs are not stable. Avoid if possible. */

MONO_API MonoJitInfo *
mono_get_jit_info_from_method (MonoDomain *domain, MonoMethod *method);

MONO_API void *
mono_aot_get_method (MonoDomain *domain, MonoMethod *method);

MONO_END_DECLS

#endif

