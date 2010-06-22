/*
 * mach-support-x86.c: mach support for x86
 *
 * Authors:
 *   Geoff Norton (gnorton@novell.com)
 *
 * (C) 2010 Ximian, Inc.
 */

#include <config.h>

#if defined(__MACH__)
#include <stdint.h>
#include <glib.h>
#include <pthread.h>
#include "utils/mono-sigcontext.h"
#include "mach-support.h"

void *
mono_mach_arch_get_ip (thread_state_t state)
{
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) state;

	return (void *) arch_state->__eip;
}

void *
mono_mach_arch_get_sp (thread_state_t state)
{
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) state;

	return (void *) arch_state->__esp;
}

void *
mono_mach_arch_thread_state_to_context (thread_state_t state)
{
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) state;
	struct __darwin_mcontext32 *ctx;

	ctx = (struct __darwin_mcontext32 *) g_new0 (struct __darwin_mcontext32, 1);
	ctx->__ss = *arch_state;

	return ctx;
}

kern_return_t
mono_mach_arch_get_thread_state (thread_port_t thread, thread_state_t *state, mach_msg_type_number_t *count)
{
	x86_thread_state32_t *arch_state = (x86_thread_state32_t *) g_new0 (x86_thread_state32_t, 1);
	kern_return_t ret;

	*count = x86_THREAD_STATE32_COUNT;

	ret = thread_get_state (thread, x86_THREAD_STATE32, (thread_state_t) arch_state, count);

	*state = (thread_state_t) arch_state;

	return ret;
}

void *
mono_mach_arch_get_tls_value_from_thread (thread_port_t thread, guint32 key)
{
	/* OSX stores TLS values in a hidden array inside the pthread_t structure
	 * They are keyed off a giant array offset 0x48 into the pointer.  This value
	 * is baked into their pthread_getspecific implementation
	 */
	intptr_t *p = (intptr_t *) pthread_from_mach_thread_np (thread);
	intptr_t **tsd = (intptr_t **) (p + 0x48);

	return (void *) tsd [key];
}
#endif
