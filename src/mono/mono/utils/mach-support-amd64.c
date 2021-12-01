/**
 * \file
 * mach support for x86
 *
 * Authors:
 *   Geoff Norton (gnorton@novell.com)
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2010 Novell, Inc.
 * (C) 2013 Xamarin, Inc.
 */

#include <config.h>

#if defined(__MACH__)
#include <stdint.h>
#include <glib.h>
#include <pthread.h>
#include "utils/mono-sigcontext.h"
#include "mach-support.h"

//For reg numbers
#include <mono/arch/amd64/amd64-codegen.h>

int
mono_mach_arch_get_mcontext_size ()
{
	return sizeof (struct __darwin_mcontext64);
}

void
mono_mach_arch_thread_states_to_mcontext (thread_state_t state, thread_state_t fpstate, void *context)
{
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *) state;
	x86_float_state64_t *arch_fpstate = (x86_float_state64_t *) fpstate;
	struct __darwin_mcontext64 *ctx = (struct __darwin_mcontext64 *) context;
	ctx->__ss = *arch_state;
	ctx->__fs = *arch_fpstate;
}

void
mono_mach_arch_mcontext_to_thread_states (void *context, thread_state_t state, thread_state_t fpstate)
{
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *) state;
	x86_float_state64_t *arch_fpstate = (x86_float_state64_t *) fpstate;
	struct __darwin_mcontext64 *ctx = (struct __darwin_mcontext64 *) context;
	*arch_state = ctx->__ss;
	*arch_fpstate = ctx->__fs;
}

void
mono_mach_arch_thread_states_to_mono_context (thread_state_t state, thread_state_t fpstate, MonoContext *context)
{
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *) state;
	x86_float_state64_t *arch_fpstate = (x86_float_state64_t *) fpstate;
	context->gregs [AMD64_RAX] = arch_state->__rax;
	context->gregs [AMD64_RBX] = arch_state->__rbx;
	context->gregs [AMD64_RCX] = arch_state->__rcx;
	context->gregs [AMD64_RDX] = arch_state->__rdx;
	context->gregs [AMD64_RDI] = arch_state->__rdi;
	context->gregs [AMD64_RSI] = arch_state->__rsi;
	context->gregs [AMD64_RBP] = arch_state->__rbp;
	context->gregs [AMD64_RSP] = arch_state->__rsp;
	context->gregs [AMD64_R8] = arch_state->__r8;
	context->gregs [AMD64_R9] = arch_state->__r9;
	context->gregs [AMD64_R10] = arch_state->__r10;
	context->gregs [AMD64_R11] = arch_state->__r11;
	context->gregs [AMD64_R12] = arch_state->__r12;
	context->gregs [AMD64_R13] = arch_state->__r13;
	context->gregs [AMD64_R14] = arch_state->__r14;
	context->gregs [AMD64_R15] = arch_state->__r15;
	context->gregs [AMD64_RIP] = arch_state->__rip;
	context->fregs [AMD64_XMM0] = arch_fpstate->__fpu_xmm0;
	context->fregs [AMD64_XMM1] = arch_fpstate->__fpu_xmm1;
	context->fregs [AMD64_XMM2] = arch_fpstate->__fpu_xmm2;
	context->fregs [AMD64_XMM3] = arch_fpstate->__fpu_xmm3;
	context->fregs [AMD64_XMM4] = arch_fpstate->__fpu_xmm4;
	context->fregs [AMD64_XMM5] = arch_fpstate->__fpu_xmm5;
	context->fregs [AMD64_XMM6] = arch_fpstate->__fpu_xmm6;
	context->fregs [AMD64_XMM7] = arch_fpstate->__fpu_xmm7;
	context->fregs [AMD64_XMM8] = arch_fpstate->__fpu_xmm8;
	context->fregs [AMD64_XMM9] = arch_fpstate->__fpu_xmm9;
	context->fregs [AMD64_XMM10] = arch_fpstate->__fpu_xmm10;
	context->fregs [AMD64_XMM11] = arch_fpstate->__fpu_xmm11;
	context->fregs [AMD64_XMM12] = arch_fpstate->__fpu_xmm12;
	context->fregs [AMD64_XMM13] = arch_fpstate->__fpu_xmm13;
	context->fregs [AMD64_XMM14] = arch_fpstate->__fpu_xmm14;
	context->fregs [AMD64_XMM15] = arch_fpstate->__fpu_xmm15;
}

int
mono_mach_arch_get_thread_state_size ()
{
	return sizeof (x86_thread_state64_t);
}

int
mono_mach_arch_get_thread_fpstate_size ()
{
	return sizeof (x86_float_state64_t);
}

kern_return_t
mono_mach_arch_get_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count, thread_state_t fpstate, mach_msg_type_number_t *fpcount)
{
#if defined(HOST_WATCHOS)
	g_error ("thread_get_state() is not supported by this platform");
#else
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *)state;
	x86_float_state64_t *arch_fpstate = (x86_float_state64_t *)fpstate;
	kern_return_t ret;

	*count = x86_THREAD_STATE64_COUNT;
	*fpcount = x86_FLOAT_STATE64_COUNT;

	ret = thread_get_state (thread, x86_THREAD_STATE64, (thread_state_t)arch_state, count);
	if (ret != KERN_SUCCESS)
		return ret;

	ret = thread_get_state (thread, x86_FLOAT_STATE64, (thread_state_t)arch_fpstate, fpcount);
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
	ret = thread_set_state (thread, x86_THREAD_STATE64, state, count);
	if (ret != KERN_SUCCESS)
		return ret;
	ret = thread_set_state (thread, x86_FLOAT_STATE64, fpstate, fpcount);
	return ret;
#endif
}

#endif
