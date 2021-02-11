#include <mono/utils/lifo-semaphore.h>

LifoSemaphore *
mono_lifo_semaphore_init (void)
{
	LifoSemaphore *semaphore = g_new0 (LifoSemaphore, 1);
	if (semaphore == NULL)
		return NULL;

	mono_coop_mutex_init (&semaphore->mutex);

	return semaphore;
}

void
mono_lifo_semaphore_delete (LifoSemaphore *semaphore)
{
	g_assert (semaphore->head == NULL);
	mono_coop_mutex_destroy (&semaphore->mutex);
	g_free (semaphore);
}

int32_t
mono_lifo_semaphore_timed_wait (LifoSemaphore *semaphore, int32_t timeout_ms)
{
	LifoSemaphoreWaitEntry wait_entry = {0};

	mono_coop_cond_init (&wait_entry.condition);
	mono_coop_mutex_lock (&semaphore->mutex);

	if (semaphore->pending_signals > 0) {
		--semaphore->pending_signals;
		mono_coop_cond_destroy (&wait_entry.condition);
		mono_coop_mutex_unlock (&semaphore->mutex);
		return 1;
	}

	// Enqueue out entry into the LIFO wait list
	wait_entry.previous = NULL;
	wait_entry.next = semaphore->head;
	if (semaphore->head != NULL)
		semaphore->head->previous = &wait_entry;
	semaphore->head = &wait_entry;

	// Wait for a signal or timeout
	int wait_error = 0;
	do {
		wait_error = mono_coop_cond_timedwait (&wait_entry.condition, &semaphore->mutex, timeout_ms);
	} while (wait_error == 0 && !wait_entry.signaled);

	if (wait_error == -1) {
		if (semaphore->head == &wait_entry)
			semaphore->head = wait_entry.next;
		if (wait_entry.next != NULL)
			wait_entry.next->previous = wait_entry.previous;
		if (wait_entry.previous != NULL)
			wait_entry.previous->next = wait_entry.next;
	}

	mono_coop_cond_destroy (&wait_entry.condition);
	mono_coop_mutex_unlock (&semaphore->mutex);

	return wait_entry.signaled;
}

void
mono_lifo_semaphore_release (LifoSemaphore *semaphore, uint32_t count)
{
	mono_coop_mutex_lock (&semaphore->mutex);

	while (count > 0) {
		LifoSemaphoreWaitEntry *wait_entry = semaphore->head;
		if (wait_entry != NULL) {
			semaphore->head = wait_entry->next;
			if (semaphore->head != NULL)
				semaphore->head->previous = NULL;
			wait_entry->previous = NULL;
			wait_entry->next = NULL;
			wait_entry->signaled = 1;
			mono_coop_cond_signal (&wait_entry->condition);
			--count;
		} else {
			semaphore->pending_signals += count;
			count = 0;
		}
	}

	mono_coop_mutex_unlock (&semaphore->mutex);
}
