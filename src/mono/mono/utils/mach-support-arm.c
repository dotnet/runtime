/*
 * mach-support-arm.c: mach support for ARM
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
#include "mach-support.h"

void *
mono_mach_arch_get_ip (thread_state_t state)
{
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;

	return (void *) arch_state->__pc;
}

void *
mono_mach_arch_get_sp (thread_state_t state)
{
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;

	return (void *) arch_state->__sp;
}

int
mono_mach_arch_get_mcontext_size ()
{
	return sizeof (struct __darwin_mcontext);
}

void
mono_mach_arch_thread_state_to_mcontext (thread_state_t state, mcontext_t context)
{
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;
	struct __darwin_mcontext *ctx = (struct __darwin_mcontext *) context;

	ctx->__ss = *arch_state;
}

void
mono_mach_arch_mcontext_to_thread_state (mcontext_t context, thread_state_t state)
{
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;
	struct __darwin_mcontext *ctx = (struct __darwin_mcontext *) context;

	*arch_state = ctx->__ss;
}

int
mono_mach_arch_get_thread_state_size ()
{
	return sizeof (arm_thread_state_t);
}

kern_return_t
mono_mach_arch_get_thread_state (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count)
{
	arm_thread_state_t *arch_state = (arm_thread_state_t *) state;
	kern_return_t ret;

	*count = ARM_THREAD_STATE_COUNT;

	ret = thread_get_state (thread, ARM_THREAD_STATE_COUNT, (thread_state_t) arch_state, count);

	return ret;
}

kern_return_t
mono_mach_arch_set_thread_state (thread_port_t thread, thread_state_t state, mach_msg_type_number_t count)
{
	return thread_set_state (thread, ARM_THREAD_STATE_COUNT, state, count);
}

void *
mono_mach_get_tls_address_from_thread (pthread_t thread, pthread_key_t key)
{
	/* OSX stores TLS values in a hidden array inside the pthread_t structure
	 * They are keyed off a giant array offset 0x48 into the pointer.  This value
	 * is baked into their pthread_getspecific implementation
	 */
	intptr_t *p = (intptr_t *) thread;
	intptr_t **tsd = (intptr_t **) ((char*)p + 0x48 + (key << 2));

	return (void *)tsd;
}

void *
mono_mach_arch_get_tls_value_from_thread (pthread_t thread, guint32 key)
{
	return *(void**)mono_mach_get_tls_address_from_thread (thread, key);
}

#endif
