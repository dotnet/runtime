#ifndef _WAPI_WAIT_PRIVATE_H_
#define _WAPI_WAIT_PRIVATE_H_

/* This is an internal, private header file */

#include <glib.h>
#include <pthread.h>
#include <semaphore.h>

#include "wapi-private.h"
#include "timed-thread.h"

typedef enum {
	WQ_NEW,
	WQ_WAITING,
	WQ_SIGNALLED,
} WaitQueueState;

typedef struct _WaitQueueItem 
{
	pthread_mutex_t mutex;
	sem_t wait_sem;
	WaitQueueState state;
	guint32 update, ack;
	guint32 timeout;
	gboolean waitall;
	GPtrArray *handles[WAPI_HANDLE_COUNT];
	TimedThread *thread[WAPI_HANDLE_COUNT];
	gboolean waited[WAPI_HANDLE_COUNT];
	guint32 waitcount[WAPI_HANDLE_COUNT];
	/* waitindex must be kept synchronised with the handles array,
	 * such that index n of one matches index n of the other
	 */
	GArray *waitindex[WAPI_HANDLE_COUNT];
	guint32 lowest_signal;
} WaitQueueItem;

#endif /* _WAPI_WAIT_PRIVATE_H_ */
