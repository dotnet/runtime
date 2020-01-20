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
mono_mach_arch_get_mcontext_size ()
{
	return sizeof (struct __darwin_mcontext);
}

void
mono_mach_arch_thread_states_to_mcontext (thread_state_t state, thread_state_t fpstate, void *context)
{
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;
	struct __darwin_mcontext *ctx = (struct __darwin_mcontext *) context;

	ctx->__ss = *arch_state;
}

void
mono_mach_arch_mcontext_to_thread_states (void *context, thread_state_t state, thread_state_t fpstate)
{
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;
	struct __darwin_mcontext *ctx = (struct __darwin_mcontext *) context;

	*arch_state = ctx->__ss;
}

void
mono_mach_arch_thread_states_to_mono_context (thread_state_t state, thread_state_t fpstate, MonoContext *context)
{
	int i;
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;
	for (i = 0; i < 13; ++i)
		context->regs [i] = arch_state->__r [i];
	context->regs [ARMREG_R13] = arch_state->__sp;
	context->regs [ARMREG_R14] = arch_state->__lr;
	context->regs [ARMREG_R15] = arch_state->__pc;
	context->pc = arch_state->__pc;
	context->cpsr = arch_state->__cpsr;
}

int
mono_mach_arch_get_thread_state_size ()
{
	return sizeof (arm_thread_state_t);
}

int
mono_mach_arch_get_thread_fpstate_size ()
{
	return sizeof (arm_neon_state_t);
}

kern_return_t
mono_mach_arch_get_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count, thread_state_t fpstate, mach_msg_type_number_t *fpcount)
{
#if defined(HOST_WATCHOS)
	g_error ("thread_get_state() is not supported by this platform");
#else	
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;
	kern_return_t ret;

	*count = ARM_THREAD_STATE_COUNT;

	ret = thread_get_state (thread, ARM_THREAD_STATE, (thread_state_t) arch_state, count);
	return ret;
#endif
}

kern_return_t
mono_mach_arch_set_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t count, thread_state_t fpstate, mach_msg_type_number_t fpcount)
{
#if defined(HOST_WATCHOS)
	g_error ("thread_set_state() is not supported by this platform");
#else
	return thread_set_state (thread, ARM_THREAD_STATE, state, count);
#endif
}

#endif
