#ifndef __MONO_LIFO_SEMAPHORE_H__
#define __MONO_LIFO_SEMAPHORE_H__

#include <mono/utils/mono-coop-mutex.h>

typedef struct _LifoSemaphore LifoSemaphore;
typedef struct _LifoSemaphoreWaitEntry LifoSemaphoreWaitEntry;

struct _LifoSemaphoreWaitEntry {
	LifoSemaphoreWaitEntry *previous;
	LifoSemaphoreWaitEntry *next;
	MonoCoopCond condition;
	int signaled;
};

struct _LifoSemaphore {
	MonoCoopMutex mutex;
	LifoSemaphoreWaitEntry *head;
	uint32_t pending_signals;
};

LifoSemaphore *
mono_lifo_semaphore_init (void);

void
mono_lifo_semaphore_delete (LifoSemaphore *semaphore);

int32_t
mono_lifo_semaphore_timed_wait (LifoSemaphore *semaphore, int32_t timeout_ms);

void
mono_lifo_semaphore_release (LifoSemaphore *semaphore, uint32_t count);

#endif // __MONO_LIFO_SEMAPHORE_H__
