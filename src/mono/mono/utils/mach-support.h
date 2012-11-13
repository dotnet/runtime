#ifndef __MONO_MACH_SUPPORT_H__
#define __MONO_MACH_SUPPORT_H__

#include "config.h"
#if defined(__MACH__)
#include <glib.h>
#include <pthread.h>
#include "mono/utils/mono-compiler.h"
#include <mach/task.h>
#include <mach/mach_port.h>
#include <mach/mach_init.h>
#include <mach/thread_act.h>
#include <mach/thread_status.h>

#define MONO_MACH_ARCH_SUPPORTED 1
#if defined(__arm__)
typedef _STRUCT_MCONTEXT *mcontext_t;
#endif

// We need to define this here since we need _XOPEN_SOURCE for mono
// and the pthread header guards against this
extern pthread_t pthread_from_mach_thread_np(mach_port_t);

void *mono_mach_arch_get_ip (thread_state_t state) MONO_INTERNAL;
void *mono_mach_arch_get_sp (thread_state_t state) MONO_INTERNAL;

int mono_mach_arch_get_mcontext_size (void) MONO_INTERNAL;
void mono_mach_arch_thread_state_to_mcontext (thread_state_t state, void *context) MONO_INTERNAL;
void mono_mach_arch_mcontext_to_thread_state (void *context, thread_state_t state) MONO_INTERNAL;

int mono_mach_arch_get_thread_state_size (void) MONO_INTERNAL;
kern_return_t mono_mach_get_threads (thread_act_array_t *threads, guint32 *count) MONO_INTERNAL;
kern_return_t mono_mach_free_threads (thread_act_array_t threads, guint32 count) MONO_INTERNAL;
kern_return_t mono_mach_arch_get_thread_state (thread_port_t thread, thread_state_t state, mach_msg_type_number_t *count) MONO_INTERNAL;
kern_return_t mono_mach_arch_set_thread_state (thread_port_t thread, thread_state_t state, mach_msg_type_number_t count) MONO_INTERNAL;
void *mono_mach_arch_get_tls_value_from_thread (pthread_t thread, guint32 key) MONO_INTERNAL;
void *mono_mach_get_tls_address_from_thread (pthread_t thread, pthread_key_t key) MONO_INTERNAL;

#endif
#endif /* __MONO_MACH_SUPPORT_H__ */
