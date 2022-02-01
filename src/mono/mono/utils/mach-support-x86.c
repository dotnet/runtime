/**
 * \file
 * mach support for x86
 *
 * Authors:
 *   Geoff Norton (gnorton@novell.com)
 *
 * (C) 2010 Novell, Inc.
 */

#include <config.h>

#if defined(__MACH__)
#include <stdint.h>
#include <glib.h>
#include <pthread.h>
#include "utils/mono-sigcontext.h"
#include "mach-support.h"

// For reg numbers
#include <mono/arch/amd64/amd64-codegen.h>

int
mono_mach_arch_get_mcontext_size ()
{
	return sizeof (struct __darwin_mcontext32);
}

void
mono_mach_arch_thread_states_to_mcontext (thread_state_t state, thread_state_t fpstate, void *context)
{
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) state;
	x86_float_state32_t *arch_fpstate = (x86_float_state32_t *) fpstate;
	struct __darwin_mcontext32 *ctx = (struct __darwin_mcontext32 *) context;
	ctx->__ss = *arch_state;
	ctx->__fs = *arch_fpstate;
}

void
mono_mach_arch_mcontext_to_thread_states (void *context, thread_state_t state, thread_state_t fpstate)
{
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) state;
	x86_float_state32_t *arch_fpstate = (x86_float_state32_t *) fpstate;
	struct __darwin_mcontext32 *ctx = (struct __darwin_mcontext32 *) context;
	*arch_state = ctx->__ss;
	*arch_fpstate = ctx->__fs;
}

void
mono_mach_arch_thread_states_to_mono_context (thread_state_t state, thread_state_t fpstate, MonoContext *context)
{
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) state;
	x86_float_state32_t *arch_fpstate = (x86_float_state32_t *) state;
	context->eax = arch_state->__eax;
	context->ebx = arch_state->__ebx;
	context->ecx = arch_state->__ecx;
	context->edx = arch_state->__edx;
	context->ebp = arch_state->__ebp;
	context->esp = arch_state->__esp;
	context->esi = arch_state->__edi;
	context->edi = arch_state->__esi;
	context->eip = arch_state->__eip;
	context->fregs [X86_XMM0] = arch_fpstate->__fpu_xmm0;
	context->fregs [X86_XMM1] = arch_fpstate->__fpu_xmm1;
	context->fregs [X86_XMM2] = arch_fpstate->__fpu_xmm2;
	context->fregs [X86_XMM3] = arch_fpstate->__fpu_xmm3;
	context->fregs [X86_XMM4] = arch_fpstate->__fpu_xmm4;
	context->fregs [X86_XMM5] = arch_fpstate->__fpu_xmm5;
	context->fregs [X86_XMM6] = arch_fpstate->__fpu_xmm6;
	context->fregs [X86_XMM7] = arch_fpstate->__fpu_xmm7;
}

int
mono_mach_arch_get_thread_state_size ()
{
	return sizeof (x86_thread_state32_t);
}

int
mono_mach_arch_get_thread_fpstate_size ()
{
	return sizeof (x86_float_state32_t);
}

kern_return_t
mono_mach_arch_get_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count, thread_state_t fpstate, mach_msg_type_number_t *fpcount)
{
#if defined(HOST_WATCHOS)
	g_error ("thread_get_state() is not supported by this platform");
#else
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) state;
	x86_float_state32_t *arch_fpstate = (x86_float_state32_t *) fpstate;
	kern_return_t ret;

	*count = x86_THREAD_STATE32_COUNT;
	*fpcount = x86_FLOAT_STATE32_COUNT;

	ret = thread_get_state (thread, x86_THREAD_STATE32, (thread_state_t)arch_state, count);
	if (ret != KERN_SUCCESS)
		return ret;

	ret = thread_get_state (thread, x86_FLOAT_STATE32, (thread_state_t)arch_fpstate, fpcount);
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
	ret = thread_set_state (thread, x86_THREAD_STATE32, state, count);
	if (ret != KERN_SUCCESS)
		return ret;
	ret = thread_set_state (thread, x86_FLOAT_STATE32, fpstate, fpcount);
	return ret;
#endif	
}

#endif
