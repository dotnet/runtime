#ifndef __MONO_LIFO_SEMAPHORE_H__
#define __MONO_LIFO_SEMAPHORE_H__

#include <mono/utils/mono-coop-mutex.h>

typedef struct _LifoSemaphoreBase LifoSemaphoreBase;

struct _LifoSemaphoreBase
{
	MonoCoopMutex mutex;
	uint32_t pending_signals;
	uint8_t       kind;
};

enum {
	LIFO_SEMAPHORE_NORMAL = 1,
#if defined(HOST_BROWSER) && !defined(DISABLE_THREADS)
	LIFO_SEMAPHORE_ASYNCWAIT,
#endif
};
	
typedef struct _LifoSemaphore LifoSemaphore;
typedef struct _LifoSemaphoreWaitEntry LifoSemaphoreWaitEntry;

struct _LifoSemaphoreWaitEntry {
	LifoSemaphoreWaitEntry *previous;
	LifoSemaphoreWaitEntry *next;
	MonoCoopCond condition;
	int signaled;
};

struct _LifoSemaphore {
	LifoSemaphoreBase base;
	LifoSemaphoreWaitEntry *head;
};

LifoSemaphore *
mono_lifo_semaphore_init (void);

void
mono_lifo_semaphore_delete (LifoSemaphore *semaphore);

int32_t
mono_lifo_semaphore_timed_wait (LifoSemaphore *semaphore, int32_t timeout_ms);

void
mono_lifo_semaphore_release (LifoSemaphore *semaphore, uint32_t count);

#if defined(HOST_BROWSER) && !defined(DISABLE_THREADS)
/* A type of lifo semaphore that can be waited from the JS event loop.
 *
 * Instead of a blocking timed_wait function, it uses a pair of callbacks: a success callback and a
 * timeout callback.  The wait function returns immediately and the callbacks will fire on the JS
 * event loop when the semaphore is released or the timeout expires.
 */
typedef struct _LifoSemaphoreAsyncWait LifoSemaphoreAsyncWait;
/*
 * Because the callbacks are asynchronous, it's possible for the same thread to attempt to wait
 * multiple times for the same semaphore.  For simplicity of reasoning, we dissallow that and
 * assert.  In principle we could support it, but we haven't implemented that.
 */
typedef struct _LifoSemaphoreAsyncWaitWaitEntry LifoSemaphoreAsyncWaitWaitEntry;

typedef void (*LifoSemaphoreAsyncWaitCallbackFn)(LifoSemaphoreAsyncWait *semaphore, intptr_t user_data);

struct _LifoSemaphoreAsyncWaitWaitEntry {
	LifoSemaphoreAsyncWaitWaitEntry *previous;
	LifoSemaphoreAsyncWaitWaitEntry *next;
	LifoSemaphoreAsyncWaitCallbackFn success_cb;
	LifoSemaphoreAsyncWaitCallbackFn timeout_cb;
	LifoSemaphoreAsyncWait *sem;
	intptr_t user_data;
	pthread_t thread;
	int32_t js_timeout_id; // only valid to access from the waiting thread
	/* state and refcount are protected by the semaphore mutex */
	uint16_t state; /* 0 waiting, 1 signaled, 2 signaled - timeout ignored */
	uint16_t refcount; /* 1 if waiting, 2 if signaled, 1 if timeout fired while signaled and we're ignoring the timeout */
};
	
struct _LifoSemaphoreAsyncWait {
	LifoSemaphoreBase base;
	LifoSemaphoreAsyncWaitWaitEntry *head;
};

LifoSemaphoreAsyncWait *
mono_lifo_semaphore_asyncwait_init (void);

/* what to do with waiters?
 * might be kind of academic - we don't expect to destroy these
 */
void
mono_lifo_semaphore_asyncwait_delete (LifoSemaphoreAsyncWait *semaphore);

/*
 * the timeout_cb is triggered by a JS setTimeout callback
 *
 * the success_cb is triggered using Emscripten's capability to push async work from one thread to
 * another.  That means the main thread will need to be able to process JS events (in order to
 * assist threads in pushing work from one thread to another) in order for success callbacks to
 * function.  Emscripten also pumps the async work queues in other circumstances (during sleeps) but
 * the main thread still needs to participate.
 *
 * There's a potential race the implementation needs to be careful about:
 *   when one thread releases a semaphore and queues the success callback to run,
 *   while the success callback is in flight, the timeout callback can fire.
 *   It is important that the called back functions don't destroy the wait entry until either both
 *   callbacks have fired, or the success callback has a chance to cancel the timeout callback.
 * 
 * We use a refcount to delimit the lifetime of the wait entry. When the wait is created, the
 * refcount is 1 and it is notionally owned by the timeout callback.  When a sempahore is released,
 * the refcount goes to 2.  When a continuation fires, it decreases the refcount.  If the timeout
 * callback fires first if it sees a refcount of 2 it can decrement and return - we know a success
 * continuation is in flight and we can allow it to complete. If the refcount is 1 we need to take the semaphore's mutex and remove the wait entry. (With double check locking - the refcount could go up).
 *
 * When the success continuation fires,it will examine the refcount. If the refcount is 1 at the
 * outset, then the cancelation already tried to fire while we were in flight.  If the refcount is 2
 * at the outset, then the success contination fired before the timeout, so we can cancel the
 * timeout.  In either case we can remove the wait entry.
 *
 * Both the success and timeout code only calls the user provided callbacks after the wait entry is
 * destroyed.
 *
 * FIXME: should we just always use the mutex to protect the wait entry status+refcount?
 *
 * TODO: when we call emscripten_set_timeout it implicitly calls emscripten_runtime_keepalive_push which is
 * popped when the timeout runs.  But emscripten_clear_timeout doesn't pop - we need to pop ourselves
 */
void
mono_lifo_semaphore_asyncwait_prepare_wait (LifoSemaphoreAsyncWait *semaphore, int32_t timeout_ms,
				     LifoSemaphoreAsyncWaitCallbackFn success_cb,
				     LifoSemaphoreAsyncWaitCallbackFn timeout_cb,
				     intptr_t user_data);

void
mono_lifo_semaphore_asyncwait_release (LifoSemaphoreAsyncWait *semaphore, uint32_t count);

#endif /* HOST_BROWSER && !DISABLE_THREADS */

#endif // __MONO_LIFO_SEMAPHORE_H__
