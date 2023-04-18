#include <config.h>
#include <glib.h>
#include <mono/utils/lifo-semaphore.h>

#if defined(HOST_BROWSER) && !defined(DISABLE_THREADS)
#include <emscripten/eventloop.h>
#include <emscripten/threading.h>
#endif

LifoSemaphore *
mono_lifo_semaphore_init (void)
{
	LifoSemaphore *semaphore = g_new0 (LifoSemaphore, 1);
	semaphore->base.kind = LIFO_SEMAPHORE_NORMAL;
	if (semaphore == NULL)
		return NULL;

	mono_coop_mutex_init (&semaphore->base.mutex);

	return semaphore;
}

void
mono_lifo_semaphore_delete (LifoSemaphore *semaphore)
{
	g_assert (semaphore->head == NULL);
	mono_coop_mutex_destroy (&semaphore->base.mutex);
	g_free (semaphore);
}

int32_t
mono_lifo_semaphore_timed_wait (LifoSemaphore *semaphore, int32_t timeout_ms)
{
	LifoSemaphoreWaitEntry wait_entry = {0};

	mono_coop_cond_init (&wait_entry.condition);
	mono_coop_mutex_lock (&semaphore->base.mutex);

	if (semaphore->base.pending_signals > 0) {
		--semaphore->base.pending_signals;
		mono_coop_cond_destroy (&wait_entry.condition);
		mono_coop_mutex_unlock (&semaphore->base.mutex);
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
		wait_error = mono_coop_cond_timedwait (&wait_entry.condition, &semaphore->base.mutex, timeout_ms);
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
	mono_coop_mutex_unlock (&semaphore->base.mutex);

	return wait_entry.signaled;
}

void
mono_lifo_semaphore_release (LifoSemaphore *semaphore, uint32_t count)
{
	mono_coop_mutex_lock (&semaphore->base.mutex);

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
			semaphore->base.pending_signals += count;
			count = 0;
		}
	}

	mono_coop_mutex_unlock (&semaphore->base.mutex);
}

#if defined(HOST_BROWSER) && !defined(DISABLE_THREADS)

LifoSemaphoreAsyncWait *
mono_lifo_semaphore_asyncwait_init (void)
{
	LifoSemaphoreAsyncWait *sem = g_new0 (LifoSemaphoreAsyncWait, 1);
	if (sem == NULL)
		return NULL;
	sem->base.kind = LIFO_SEMAPHORE_ASYNCWAIT;

	mono_coop_mutex_init (&sem->base.mutex);

	return sem;
}

void
mono_lifo_semaphore_asyncwait_delete (LifoSemaphoreAsyncWait *sem)
{
	/* FIXME: this is probably hard to guarantee - in-flight signaled semaphores still have wait entries */
	g_assert (sem->head == NULL);
	mono_coop_mutex_destroy (&sem->base.mutex);
	g_free (sem);
}

enum {
	LIFO_JS_WAITING = 0,
	LIFO_JS_SIGNALED = 1,
	LIFO_JS_SIGNALED_TIMEOUT_IGNORED = 2,

};

static void
lifo_js_wait_entry_on_timeout (void *wait_entry_as_user_data);
static void
lifo_js_wait_entry_on_success (void *wait_entry_as_user_data);


static void
lifo_js_wait_entry_push (LifoSemaphoreAsyncWaitWaitEntry **head,
			 LifoSemaphoreAsyncWaitWaitEntry *entry)
{
	LifoSemaphoreAsyncWaitWaitEntry *next = *head;
	*head = entry;
	entry->next = next;
	if (next)
		next->previous = entry;
}

static void
lifo_js_wait_entry_unlink (LifoSemaphoreAsyncWaitWaitEntry **head,
			   LifoSemaphoreAsyncWaitWaitEntry *entry)
{
	if (*head == entry) {
		*head = entry->next;
	}
	if (entry->previous) {
		entry->previous->next = entry->next;
	}
	if (entry->next) {
		entry->next->previous = entry->previous;
	}
}

/* LOCKING: assumes semaphore is locked */
static LifoSemaphoreAsyncWaitWaitEntry *
lifo_js_find_waiter (LifoSemaphoreAsyncWaitWaitEntry *entry)
{
	while (entry) {
		if (entry->state == LIFO_JS_WAITING)
			return entry;
		entry = entry->next;
	}
	return NULL;
}

static gboolean
lifo_js_wait_entry_no_thread (LifoSemaphoreAsyncWaitWaitEntry *entry,
			     pthread_t cur)
{
	while (entry) {
		if (pthread_equal (entry->thread, cur))
			return FALSE;
		entry = entry->next;
	}
	return TRUE;
}

void
mono_lifo_semaphore_asyncwait_prepare_wait (LifoSemaphoreAsyncWait *sem,
				     int32_t timeout_ms,
				     LifoSemaphoreAsyncWaitCallbackFn success_cb,
				     LifoSemaphoreAsyncWaitCallbackFn timeout_cb,
				     intptr_t user_data)
{
	mono_coop_mutex_lock (&sem->base.mutex);
	if (sem->base.pending_signals > 0) {
		sem->base.pending_signals--;
		mono_coop_mutex_unlock (&sem->base.mutex);
		success_cb (sem, user_data); // FIXME: queue microtask
		return;
	}

	pthread_t cur = pthread_self ();

	/* Don't allow the current thread to wait multiple times.
	 * No particular reason for it, except that it makes reasoning a bit easier.
	 * This can probably be relaxed if there's a need.
	 */
	g_assert (lifo_js_wait_entry_no_thread(sem->head, cur));

	LifoSemaphoreAsyncWaitWaitEntry *wait_entry = g_new0 (LifoSemaphoreAsyncWaitWaitEntry, 1);
	wait_entry->success_cb = success_cb;
	wait_entry->timeout_cb = timeout_cb;
	wait_entry->sem = sem;
	wait_entry->user_data = user_data;
	wait_entry->thread = pthread_self();
	wait_entry->state = LIFO_JS_WAITING;
        wait_entry->refcount = 1; // timeout owns the wait entry
	wait_entry->js_timeout_id = emscripten_set_timeout (lifo_js_wait_entry_on_timeout, (double)timeout_ms, wait_entry);
	lifo_js_wait_entry_push (&sem->head, wait_entry);
	mono_coop_mutex_unlock (&sem->base.mutex);
	return;
}

void
mono_lifo_semaphore_asyncwait_release (LifoSemaphoreAsyncWait *sem,
				uint32_t count)
{
	mono_coop_mutex_lock (&sem->base.mutex);

	while (count > 0) {
		LifoSemaphoreAsyncWaitWaitEntry *wait_entry = lifo_js_find_waiter (sem->head);
		if (wait_entry != NULL) {
			/* found one.  set its status and queue some work to run on the signaled thread */
			pthread_t target = wait_entry->thread;
			wait_entry->state = LIFO_JS_SIGNALED;
			wait_entry->refcount++;
			// we're under the mutex - if we got here the timeout hasn't fired yet
			g_assert (wait_entry->refcount == 2); 
			--count;
			/* if we're on the same thread, don't run the callback while holding the lock */
			emscripten_dispatch_to_thread_async (target, EM_FUNC_SIG_VI, lifo_js_wait_entry_on_success, NULL, wait_entry);
		} else {
			sem->base.pending_signals += count;
			count = 0;
		}
	}

	mono_coop_mutex_unlock (&sem->base.mutex);
}

static void
lifo_js_wait_entry_on_timeout (void *wait_entry_as_user_data)
{
	LifoSemaphoreAsyncWaitWaitEntry *wait_entry = (LifoSemaphoreAsyncWaitWaitEntry *)wait_entry_as_user_data;
	g_assert (pthread_equal (wait_entry->thread, pthread_self()));
	g_assert (wait_entry->sem != NULL);
	LifoSemaphoreAsyncWait *sem = wait_entry->sem;
	gboolean call_timeout_cb = FALSE;
	LifoSemaphoreAsyncWaitCallbackFn timeout_cb = NULL;
	intptr_t user_data = 0;
	mono_coop_mutex_lock (&sem->base.mutex);
	switch (wait_entry->state) {
	case LIFO_JS_WAITING:
		/* semaphore timed out before a Release. */
		g_assert (wait_entry->refcount == 1);
		/* unlink and free the wait entry, run the user timeout_cb. */
		lifo_js_wait_entry_unlink (&sem->head, wait_entry);
		timeout_cb = wait_entry->timeout_cb;
		user_data = wait_entry->user_data;
		g_free (wait_entry);
		call_timeout_cb = TRUE;
		break;
	case LIFO_JS_SIGNALED:
		/* seamphore was signaled, but the timeout callback ran before the success callback arrived */
		g_assert (wait_entry->refcount == 2);
		/* set state to LIFO_JS_SIGNALED_TIMEOUT_IGNORED, decrement refcount, return */
		wait_entry->state = LIFO_JS_SIGNALED_TIMEOUT_IGNORED;
		wait_entry->refcount--;		
		break;
	case LIFO_JS_SIGNALED_TIMEOUT_IGNORED:
	default:
		g_assert_not_reached();
	}
	mono_coop_mutex_unlock (&sem->base.mutex);
	if (call_timeout_cb) {
		timeout_cb (sem, user_data);
	}
}

static void
lifo_js_wait_entry_on_success (void *wait_entry_as_user_data)
{
	LifoSemaphoreAsyncWaitWaitEntry *wait_entry = (LifoSemaphoreAsyncWaitWaitEntry *)wait_entry_as_user_data;
	g_assert (pthread_equal (wait_entry->thread, pthread_self()));
	g_assert (wait_entry->sem != NULL);
	LifoSemaphoreAsyncWait *sem = wait_entry->sem;
	gboolean call_success_cb = FALSE;
	LifoSemaphoreAsyncWaitCallbackFn success_cb = NULL;
	intptr_t user_data = 0;
	mono_coop_mutex_lock (&sem->base.mutex);
	switch (wait_entry->state) {
	case LIFO_JS_SIGNALED:
		g_assert (wait_entry->refcount == 2);
		emscripten_clear_timeout (wait_entry->js_timeout_id);
		/* emscripten safeSetTimeout calls keepalive push which is popped by the timeout
		 * callback. If we cancel the timeout, we have to pop the keepalive ourselves. */
		emscripten_runtime_keepalive_pop();
		wait_entry->refcount--;
		/* fallthru */
	case LIFO_JS_SIGNALED_TIMEOUT_IGNORED:
		g_assert (wait_entry->refcount == 1);
		lifo_js_wait_entry_unlink (&sem->head, wait_entry);
		success_cb = wait_entry->success_cb;
		user_data = wait_entry->user_data;
		g_free (wait_entry);
		call_success_cb = TRUE;
		break;
	case LIFO_JS_WAITING:
	default:
		g_assert_not_reached();
	}
	mono_coop_mutex_unlock (&sem->base.mutex);
	g_assert (call_success_cb);
	success_cb (sem, user_data);
}

#endif /* HOST_BROWSER && !DISABLE_THREADS */
