/**
 * \file
 */

#ifndef __MONO_MACH_SUPPORT_H__
#define __MONO_MACH_SUPPORT_H__

#include "config.h"
#if defined(__MACH__)
#include <glib.h>
#include <pthread.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-context.h"
#include <mach/task.h>
#include <mach/mach_port.h>
#include <mach/mach_init.h>
#include <mach/thread_act.h>
#include <mach/thread_status.h>

#define MONO_MACH_ARCH_SUPPORTED 1
#if defined(__arm__)
typedef _STRUCT_MCONTEXT *mcontext_t;
#elif defined(__aarch64__)
typedef _STRUCT_MCONTEXT64 *mcontext_t;
#endif

// We need to define this here since we need _XOPEN_SOURCE for mono
// and the pthread header guards against this
extern pthread_t pthread_from_mach_thread_np(mach_port_t);

void *mono_mach_arch_get_ip (thread_state_t state);
void *mono_mach_arch_get_sp (thread_state_t state);
void mono_mach_init (pthread_key_t key);

int mono_mach_arch_get_mcontext_size (void);
void mono_mach_arch_thread_states_to_mcontext (thread_state_t state, thread_state_t fpstate, void *context);
void mono_mach_arch_mcontext_to_thread_states (void *context, thread_state_t state, thread_state_t fpstate);
void mono_mach_arch_thread_states_to_mono_context (thread_state_t state, thread_state_t fpstate, MonoContext *context);

/* FIXME: Should return size_t, not int. */
int mono_mach_arch_get_thread_state_size (void);
int mono_mach_arch_get_thread_fpstate_size (void);
kern_return_t mono_mach_get_threads (thread_act_array_t *threads, guint32 *count);
kern_return_t mono_mach_free_threads (thread_act_array_t threads, guint32 count);
kern_return_t mono_mach_arch_get_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count, thread_state_t fpstate, mach_msg_type_number_t *fpcount);
kern_return_t mono_mach_arch_set_thread_states (thread_port_t thread, thread_state_t state, mach_msg_type_number_t count, thread_state_t fpstate, mach_msg_type_number_t fpcount);
void *mono_mach_arch_get_tls_value_from_thread (pthread_t thread, guint32 key);
void *mono_mach_get_tls_address_from_thread (pthread_t thread, pthread_key_t key);

#endif
#endif /* __MONO_MACH_SUPPORT_H__ */
