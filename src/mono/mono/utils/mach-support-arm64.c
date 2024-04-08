/**
 * \file
 * mach support for ARM
 *
 * Authors:
 *   Geoff Norton (gnorton@novell.com)
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2010 Novell, Inc.
 * (C) 2011 Xamarin, Inc.
 */

#include <config.h>

#if defined(__MACH__)
#include <stdint.h>
#include <glib.h>
#include <pthread.h>
#include "utils/mono-sigcontext.h"
#include "utils/mono-compiler.h"
#include "mach-support.h"

/* _mcontext.h now defines __darwin_mcontext32, not __darwin_mcontext, starting with Xcode 5.1 */
#ifdef _STRUCT_MCONTEXT32
       #define __darwin_mcontext       __darwin_mcontext32
#endif

int
mono_mach_arch_get_mcontext_size (void)
{
	return sizeof (struct __darwin_mcontext64);
}

void
mono_mach_arch_thread_states_to_mcontext (thread_state_t state, thread_state_t fpstate, void *context)
{
	arm_unified_thread_state_t *arch_state = (arm_unified_thread_state_t *) state;
	arm_neon_state64_t *arch_fpstate = (arm_neon_state64_t*) fpstate;
	struct __darwin_mcontext64 *ctx = (struct __darwin_mcontext64 *) context;

	ctx->__ss = arch_state->ts_64;
	ctx->__ns = *arch_fpstate;
}

void
mono_mach_arch_mcontext_to_thread_states (void *context, thread_state_t state, thread_state_t fpstate)
{
	arm_unified_thread_state_t *arch_state = (arm_unified_thread_state_t *) state;
	arm_neon_state64_t *arch_fpstate = (arm_neon_state64_t*) fpstate;
	struct __darwin_mcontext64 *ctx = (struct __darwin_mcontext64 *) context;

	arch_state->ts_64 = ctx->__ss;
	*arch_fpstate = ctx->__ns;
}

void
mono_mach_arch_thread_states_to_mono_context (thread_state_t state, thread_state_t fpstate, MonoContext *context)
{
	int i;
	arm_unified_thread_state_t *arch_state = (arm_unified_thread_state_t *) state;
	arm_neon_state64_t *arch_fpstate = (arm_neon_state64_t*) fpstate;

	for (i = 0; i < 29; ++i)
		context->regs [i] = arch_state->ts_64.__x [i];

#if __has_feature(ptrauth_calls)
	/* arm64e */
	context->regs [ARMREG_R29] = __darwin_arm_thread_state64_get_fp (arch_state->ts_64);
	context->regs [ARMREG_R30] = __darwin_arm_thread_state64_get_lr (arch_state->ts_64);
	context->regs [ARMREG_SP] = __darwin_arm_thread_state64_get_sp (arch_state->ts_64);
	context->pc = (host_mgreg_t)__darwin_arm_thread_state64_get_pc_fptr (arch_state->ts_64);
#else
	context->regs [ARMREG_R29] = arch_state->ts_64.__fp;
	context->regs [ARMREG_R30] = arch_state->ts_64.__lr;
	context->regs [ARMREG_SP] = arch_state->ts_64.__sp;
	context->pc = arch_state->ts_64.__pc;
#endif

	for (i = 0; i < 32; ++i)
		context->fregs [i] = arch_fpstate->__v [i];
}

int
mono_mach_arch_get_thread_state_size (void)
{
	return sizeof (arm_unified_thread_state_t);
}

int
mono_mach_arch_get_thread_fpstate_size (void)
{
	return sizeof (arm_neon_state64_t);
}

kern_return_t
mono_mach_arch_get_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count, thread_state_t fpstate, mach_msg_type_number_t *fpcount)
{
#if defined(HOST_WATCHOS)
	g_error ("thread_get_state() is not supported by this platform");
#else
	arm_unified_thread_state_t *arch_state = (arm_unified_thread_state_t *) state;
	arm_neon_state64_t *arch_fpstate = (arm_neon_state64_t *) fpstate;
	kern_return_t ret;

	*count = ARM_UNIFIED_THREAD_STATE_COUNT;
	ret = thread_get_state (thread, ARM_UNIFIED_THREAD_STATE, (thread_state_t) arch_state, count);
	if (ret != KERN_SUCCESS)
		return ret;

	*fpcount = ARM_NEON_STATE64_COUNT;
	ret = thread_get_state (thread, ARM_NEON_STATE64, (thread_state_t) arch_fpstate, fpcount);
	return ret;
#endif
}

kern_return_t
mono_mach_arch_set_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t count, thread_state_t fpstate, mach_msg_type_number_t fpcount)
{
#if defined(HOST_WATCHOS)
	g_error ("thread_set_state() is not supported by this platform");
#else
	kern_return_t ret;
	ret = thread_set_state (thread, ARM_UNIFIED_THREAD_STATE, state, count);
	if (ret != KERN_SUCCESS)
		return ret;
	ret = thread_set_state (thread, ARM_NEON_STATE64, fpstate, fpcount);
	return ret;
#endif
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mach_support_arm64);

#endif
