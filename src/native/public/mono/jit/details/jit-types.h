// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_JIT_TYPES_H
#define _MONO_JIT_TYPES_H

#include <mono/metadata/details/appdomain-types.h>

MONO_BEGIN_DECLS

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
	/* Same as LLVMONLY_INTERP */
	MONO_AOT_MODE_LLVMONLY,
	/*
	 * Use interpreter only, no native code is generated
	 * at runtime. Trampolines are loaded from corlib aot image.
	 */
	MONO_AOT_MODE_INTERP,
	/* Same as INTERP, but use only llvm compiled code */
	MONO_AOT_MODE_INTERP_LLVMONLY,
	/* Use only llvm compiled code, fall back to the interpeter */
	MONO_AOT_MODE_LLVMONLY_INTERP,
	/* Same as --interp */
	MONO_AOT_MODE_INTERP_ONLY,
	/* Sentinel value used internally by the runtime. We use a large number to avoid clashing with some internal values. */
	MONO_AOT_MODE_LAST = 1000,
} MonoAotMode;

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

MONO_END_DECLS

#endif /* _MONO_JIT_TYPES_H */
