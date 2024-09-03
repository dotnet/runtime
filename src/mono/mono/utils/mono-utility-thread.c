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

#include <mono/utils/mono-utility-thread.h>

typedef struct {
	MonoLockFreeQueueNode node;

	// For cleanup metadata
	MonoUtilityThread *thread;

	// For synch calls
	gboolean *finished;
	MonoSemType *response_sem;

	// Variably-sized, size is thread->payload_size
	gpointer payload [MONO_ZERO_LEN_ARRAY];
} UtilityThreadQueueEntry;

static void
free_queue_entry (gpointer p)
{
	UtilityThreadQueueEntry *util = (UtilityThreadQueueEntry *) p;
	mono_lock_free_free (p, util->thread->message_block_size);
}

static gboolean
utility_thread_handle_inbox (MonoUtilityThread *thread, gboolean at_shutdown)
{
	UtilityThreadQueueEntry *entry = (UtilityThreadQueueEntry *) mono_lock_free_queue_dequeue (&thread->work_queue);
	if (!entry)
		return FALSE;

	thread->callbacks.command (thread->state_ptr, &entry->payload, at_shutdown);
	if (entry->response_sem) {
		*entry->finished = TRUE;
		mono_os_sem_post (entry->response_sem);
	}

	mono_thread_hazardous_try_free (entry, free_queue_entry);

	return TRUE;
}

static void *
utility_thread (void *arg)
{
	MonoUtilityThread *thread = (MonoUtilityThread *) arg;
	if (thread->callbacks.early_init)
		thread->callbacks.early_init (&thread->state_ptr);

	mono_thread_info_wait_inited ();
	mono_thread_info_attach ();

	thread->callbacks.init (&thread->state_ptr);

	while (mono_atomic_load_i32 (&thread->run_thread)) {
		MONO_ENTER_GC_SAFE;
		mono_os_sem_timedwait (&thread->work_queue_sem, 1000, MONO_SEM_FLAGS_NONE);
		MONO_EXIT_GC_SAFE;
		utility_thread_handle_inbox (thread, FALSE);
	}

	/* Drain any remaining entries on shutdown. */
	while (utility_thread_handle_inbox (thread, TRUE));

	mono_os_sem_destroy (&thread->work_queue_sem);

	thread->callbacks.cleanup (thread->state_ptr);

	return NULL;
}

MonoUtilityThread *
mono_utility_thread_launch (size_t payload_size, MonoUtilityThreadCallbacks *callbacks, MonoMemAccountType accountType)
{
	MonoUtilityThread *thread = (MonoUtilityThread*)g_malloc0 (sizeof (MonoUtilityThread));
	size_t entry_size = offsetof (UtilityThreadQueueEntry, payload) + payload_size;

	thread->message_block_size = mono_pagesize ();
	thread->payload_size = payload_size;
	thread->callbacks = *callbacks;

	mono_lock_free_queue_init (&thread->work_queue);
	mono_lock_free_allocator_init_size_class (&thread->message_size_class, (unsigned int)entry_size, (unsigned int)thread->message_block_size);
	mono_lock_free_allocator_init_allocator (&thread->message_allocator, &thread->message_size_class, accountType);
	mono_os_sem_init (&thread->work_queue_sem, 0);
	mono_atomic_store_i32 (&thread->run_thread, 1);

	if (!mono_native_thread_create (&thread->thread_id, (gpointer)utility_thread, thread))
		g_error ("Could not create utility thread");

	return thread;
}

static void
mono_utility_thread_send_internal (MonoUtilityThread *thread, UtilityThreadQueueEntry *entry)
{
	mono_lock_free_queue_node_init (&entry->node, FALSE);
	mono_lock_free_queue_enqueue (&thread->work_queue, &entry->node);
	mono_os_sem_post (&thread->work_queue_sem);
}

void
mono_utility_thread_send (MonoUtilityThread *thread, gpointer message)
{
	int small_id = mono_thread_info_get_small_id ();
	if (small_id < 0) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping message send because thread not attached yet\n");
#endif
		return;
	} else if (!thread->run_thread) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping message send because thread killed\n");
#endif
		return;
	}

	UtilityThreadQueueEntry *entry = (UtilityThreadQueueEntry*)mono_lock_free_alloc (&thread->message_allocator);
	entry->response_sem = NULL;
	entry->thread = thread;
	memcpy (entry->payload, message, thread->payload_size);
	mono_utility_thread_send_internal (thread, entry);
}

gboolean
mono_utility_thread_send_sync (MonoUtilityThread *thread, gpointer message)
{
	int small_id = mono_thread_info_get_small_id ();
	if (small_id < 0) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping message send because thread not attached yet\n");
#endif
		return FALSE;
	} else if (!thread->run_thread) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping message send because thread killed\n");
#endif
		return FALSE;
	}

	MonoSemType sem;
	mono_os_sem_init (&sem, 0);

	UtilityThreadQueueEntry *entry = (UtilityThreadQueueEntry*)mono_lock_free_alloc (&thread->message_allocator);
	gboolean done;

	entry->finished = &done;
	entry->response_sem = &sem;
	entry->thread = thread;
	memcpy (entry->payload, message, thread->payload_size);
	mono_utility_thread_send_internal (thread, entry);

	while (thread->run_thread && !done) {
		// After returns, the entry is filled out with results
		gboolean timedout = mono_os_sem_timedwait (&sem, 1000, MONO_SEM_FLAGS_NONE) == MONO_SEM_TIMEDWAIT_RET_TIMEDOUT;
		if (!timedout)
			break;
		mono_os_sem_post (&thread->work_queue_sem);
	}

	mono_os_sem_destroy (&sem);

	// Return whether we ended successfully
	return done;
}

void
mono_utility_thread_stop (MonoUtilityThread *thread)
{
	int small_id = mono_thread_info_get_small_id ();
	if (small_id < 0) {
#if MONO_PRINT_DROPPED_MESSAGES
		fprintf (stderr, "Dropping attempt to stop thread, calling thread not attached yet\n");
#endif
		return;
	} else if (!thread->run_thread) {
		return;
	}

	mono_atomic_store_i32 (&thread->run_thread, 0);
	mono_os_sem_post (&thread->work_queue_sem);
}
