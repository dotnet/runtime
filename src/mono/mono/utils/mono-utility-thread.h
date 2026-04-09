/**
 * \file
 *     A lightweight worker thread with lockless messaging
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#ifndef __MONO_UTILITY_THREAD_H__
#define __MONO_UTILITY_THREAD_H__

#include <glib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/lock-free-queue.h>
#include <mono/utils/lock-free-alloc.h>
#include <mono/utils/mono-os-semaphore.h>

#define MONO_PRINT_DROPPED_MESSAGES 0

typedef struct {
	void (*early_init) (gpointer *state_ptr);
	void (*init) (gpointer *state_ptr);
	void (*command) (gpointer state_ptr, gpointer message_ptr, gboolean at_shutdown);
	void (*cleanup) (gpointer state_ptr);
} MonoUtilityThreadCallbacks;

typedef struct {
	MonoNativeThreadId thread_id;

	MonoLockFreeQueue work_queue;
	MonoSemType work_queue_sem;
	gboolean run_thread;

	MonoLockFreeAllocator message_allocator;
	MonoLockFreeAllocSizeClass message_size_class;

	size_t message_block_size;
	size_t payload_size;

	gpointer state_ptr;
	MonoUtilityThreadCallbacks callbacks;
} MonoUtilityThread;

MonoUtilityThread *
mono_utility_thread_launch (size_t payload_size, MonoUtilityThreadCallbacks *callbacks, MonoMemAccountType accountType);

void
mono_utility_thread_send (MonoUtilityThread *thread, gpointer message);

gboolean
mono_utility_thread_send_sync (MonoUtilityThread *thread, gpointer message);

void
mono_utility_thread_stop (MonoUtilityThread *thread);

#endif /* __MONO_UTILITY_THREAD_H__ */
