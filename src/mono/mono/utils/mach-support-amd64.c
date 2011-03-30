/*
 * mach-support-x86.c: mach support for x86
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

void *
mono_mach_arch_get_ip (thread_state_t state)
{
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *) state;

	return (void *) arch_state->__rip;
}

void *
mono_mach_arch_get_sp (thread_state_t state)
{
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *) state;

	return (void *) arch_state->__rsp;
}

int
mono_mach_arch_get_mcontext_size ()
{
	return sizeof (struct __darwin_mcontext64);
}

void
mono_mach_arch_thread_state_to_mcontext (thread_state_t state, mcontext_t context)
{
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *) state;
	struct __darwin_mcontext64 *ctx = (struct __darwin_mcontex64 *) context;

	ctx->__ss = *arch_state;
}

int
mono_mach_arch_get_thread_state_size ()
{
	return sizeof (x86_thread_state64_t);
}

kern_return_t
mono_mach_arch_get_thread_state (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count)
{
	x86_thread_state64_t *arch_state = (x86_thread_state64_t *) state;
	kern_return_t ret;

	*count = x86_THREAD_STATE64_COUNT;

	ret = thread_get_state (thread, x86_THREAD_STATE64, (thread_state_t) arch_state, count);

	return ret;
}

void *
mono_mach_arch_get_tls_value_from_thread (pthread_t thread, guint32 key)
{
	/* OSX stores TLS values in a hidden array inside the pthread_t structure
	 * They are keyed off a giant array offset 0x60 into the pointer.  This value
	 * is baked into their pthread_getspecific implementation
	 */
	intptr_t *p = (intptr_t *)thread;
	intptr_t **tsd = (intptr_t **) ((char*)p + 0x60);

	return (void *) tsd [key];
}
#endif
