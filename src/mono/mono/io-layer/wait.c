#include <config.h>
#include <glib.h>
#include <string.h>

#include "mono/io-layer/wapi.h"
#include "wait-private.h"
#include "timed-thread.h"
#include "handles-private.h"
#include "wapi-private.h"

#include "mono-mutex.h"

#undef DEBUG

static pthread_once_t wait_once=PTHREAD_ONCE_INIT;

static GPtrArray *WaitQueue=NULL;

static pthread_t wait_monitor_thread_id;
static gboolean wait_monitor_thread_running=FALSE;
static mono_mutex_t wait_monitor_mutex=MONO_MUTEX_INITIALIZER;
static sem_t wait_monitor_sem;

static void launch_tidy(guint32 exitcode G_GNUC_UNUSED, gpointer user)
{
	WaitQueueItem *item=(WaitQueueItem *)user;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Informing monitor thread");
#endif

	/* Update queue item */
	mono_mutex_lock(&item->mutex);
	item->update++;
	mono_mutex_unlock(&item->mutex);
	
	/* Signal monitor */
	sem_post(&wait_monitor_sem);
}

/* This function is called by the monitor thread to launch handle-type
 * specific threads to block in particular ways.
 *
 * The item mutex is held by the monitor thread when this function is
 * called.
 */
static void launch_blocker_threads(WaitQueueItem *item)
{
	int i, ret;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Launching blocker threads");
#endif

	for(i=0; i<WAPI_HANDLE_COUNT; i++) {
		if(item->handles[i]->len>0) {
			WapiHandle *handle;

			handle=g_ptr_array_index(item->handles[i], 0);
			g_assert(handle!=NULL);
			g_assert(handle->ops->wait_multiple!=NULL);
			
#ifdef DEBUG
			g_message("Handle type %d active", i);
#endif
			item->waited[i]=FALSE;
			
			ret=_wapi_timed_thread_create(
				&item->thread[i], NULL,
				handle->ops->wait_multiple, launch_tidy, item,
				item);
			if(ret!=0) {
				g_warning(G_GNUC_PRETTY_FUNCTION
					  ": Thread create error: %s",
					  strerror(ret));
				return;
			}
		} else {
			/* Pretend to have already waited for the
			 * thread; it makes life easier for the
			 * monitor thread.
			 */
			item->waited[i]=TRUE;
		}
	}
}

static gboolean launch_threads_done(WaitQueueItem *item)
{
	int i;
	
	for(i=0; i<WAPI_HANDLE_COUNT; i++) {
		if(item->waited[i]==FALSE) {
			return(FALSE);
		}
	}

	return(TRUE);
}

/* This is the main loop for the monitor thread.  It waits for a
 * signal to check the wait queue, then takes any action necessary on
 * any queue items that have indicated readiness.
 */
static void *wait_monitor_thread(void *unused G_GNUC_UNUSED)
{
	guint i;
	
	/* Signal that the monitor thread is ready */
	wait_monitor_thread_running=TRUE;
	
	while(TRUE) {
		/* Use a semaphore rather than a cond so we dont miss
		 * any signals
		 */
		sem_wait(&wait_monitor_sem);
		
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Blocking thread doing stuff");
#endif
		
		/* We've been signalled, so scan the wait queue for
		 * activity.
		 */
		mono_mutex_lock(&wait_monitor_mutex);
		for(i=0; i<WaitQueue->len; i++) {
			WaitQueueItem *item=g_ptr_array_index(WaitQueue, i);
			
			if(item->update > item->ack) {
				/* Something changed */
				mono_mutex_lock(&item->mutex);
				item->ack=item->update;
				
				switch(item->state) {
				case WQ_NEW:
					/* Launch a new thread for each type of
					 * handle to be waited for here.
					 */
					launch_blocker_threads(item);
					
					item->state=WQ_WAITING;
					break;
					
				case WQ_WAITING:
					/* See if we have waited for
					 * the last blocker thread.
					 */
					if(launch_threads_done(item)) {
						/* All handles have
						 * been signalled, so
						 * signal the waiting
						 * thread.  Let the
						 * waiting thread
						 * remove this item
						 * from the queue,
						 * because it makes
						 * the logic a lot
						 * easier here.
						 */
						item->state=WQ_SIGNALLED;
						sem_post(&item->wait_sem);
					}
					break;
					
				case WQ_SIGNALLED:
					/* This shouldn't happen. Prod
					 * the blocking thread again
					 * just to make sure.
					 */
					g_warning(G_GNUC_PRETTY_FUNCTION
						  ": Prodding blocker again");
					sem_post(&item->wait_sem);
					break;
				}
				
				mono_mutex_unlock(&item->mutex);
			}
		}

		mono_mutex_unlock(&wait_monitor_mutex);
	}
	
	return(NULL);
}

static void wait_init(void)
{
	int ret;
	
#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": Starting monitor thread");
#endif
	
	WaitQueue=g_ptr_array_new();
	
	sem_init(&wait_monitor_sem, 0, 0);
	
	/* Launch a thread which manages the wait queue, and deals
	 * with waiting for handles of various types.
	 */
	ret=pthread_create(&wait_monitor_thread_id, NULL,
			   wait_monitor_thread, NULL);
	if(ret!=0) {
		g_warning(G_GNUC_PRETTY_FUNCTION
			  ": Couldn't start handle monitor thread: %s",
			  strerror(ret));
	}
	
	/* Wait for the monitor thread to get going */
	while(wait_monitor_thread_running==FALSE) {
		sched_yield();
	}
}

static WaitQueueItem *wait_item_new(guint32 timeout, gboolean waitall)
{
	WaitQueueItem *new;
	int i;
	
	new=g_new0(WaitQueueItem, 1);
	
	mono_mutex_init(&new->mutex, NULL);
	sem_init(&new->wait_sem, 0, 0);

	new->update=1;		/* As soon as this item is queued it
				 * will need attention.
				 */
	new->state=WQ_NEW;
	new->timeout=timeout;
	new->waitall=waitall;
	new->lowest_signal=MAXIMUM_WAIT_OBJECTS;
	
	for(i=0; i<WAPI_HANDLE_COUNT; i++) {
		new->handles[i]=g_ptr_array_new();
		new->waitindex[i]=g_array_new(FALSE, FALSE, sizeof(guint32));
	}
	
	return(new);
}

/* Adds our queue item to the work queue, and blocks until the monitor
 * thread thinks it's done the work.  Returns TRUE for done, FALSE for
 * timed out.  Sets lowest to the index of the first signalled handle
 * in the list.
 */
static gboolean wait_for_item(WaitQueueItem *item, guint32 *lowest)
{
	gboolean ret;
	int i;
	
	/* Add the wait item to the monitor queue, and signal the
	 * monitor thread */
	mono_mutex_lock(&wait_monitor_mutex);
	g_ptr_array_add(WaitQueue, item);
	sem_post(&wait_monitor_sem);
	mono_mutex_unlock(&wait_monitor_mutex);
	
	/* Wait for the item to become ready */
	sem_wait(&item->wait_sem);
	
	mono_mutex_lock(&item->mutex);
	
	/* If waitall is TRUE, then the number signalled in each handle type
	 * must be the length of the handle type array for the wait to be
	 * successful.  Otherwise, any signalled handle is good enough
	 */
	if(item->waitall==TRUE) {
		ret=TRUE;
		for(i=0; i<WAPI_HANDLE_COUNT; i++) {
			if(item->waitcount[i]!=item->handles[i]->len) {
				ret=FALSE;
				break;
			}
		}
	} else {
		ret=FALSE;
		for(i=0; i<WAPI_HANDLE_COUNT; i++) {
			if(item->waitcount[i]>0) {
				ret=TRUE;
				break;
			}
		}
	}

	*lowest=item->lowest_signal;
	
	mono_mutex_unlock(&item->mutex);

	return(ret);
}

static gboolean wait_dequeue_item(WaitQueueItem *item)
{
	gboolean ret;
	
	g_assert(WaitQueue!=NULL);
	
	mono_mutex_lock(&wait_monitor_mutex);
	ret=g_ptr_array_remove_fast(WaitQueue, item);
	mono_mutex_unlock(&wait_monitor_mutex);
	
	return(ret);
}

static void wait_item_destroy(WaitQueueItem *item)
{
	int i;
	
	mono_mutex_destroy(&item->mutex);
	sem_destroy(&item->wait_sem);
	
	for(i=0; i<WAPI_HANDLE_COUNT; i++) {
		if(item->thread[i]!=NULL) {
			g_free(item->thread[i]);
		}
		g_ptr_array_free(item->handles[i], FALSE);
		g_array_free(item->waitindex[i], FALSE);
	}
}


/**
 * WaitForSingleObject:
 * @handle: an object to wait for
 * @timeout: the maximum time in milliseconds to wait for
 *
 * This function returns when either @handle is signalled, or @timeout
 * ms elapses.  If @timeout is zero, the object's state is tested and
 * the function returns immediately.  If @timeout is %INFINITE, the
 * function waits forever.
 *
 * Return value: %WAIT_ABANDONED - @handle is a mutex that was not
 * released by the owning thread when it exited.  Ownership of the
 * mutex object is granted to the calling thread and the mutex is set
 * to nonsignalled.  %WAIT_OBJECT_0 - The state of @handle is
 * signalled.  %WAIT_TIMEOUT - The @timeout interval elapsed and
 * @handle's state is still not signalled.  %WAIT_FAILED - an error
 * occurred.
 */
guint32 WaitForSingleObject(WapiHandle *handle, guint32 timeout)
{
	gboolean wait;
	
	if(handle->ops->wait==NULL) {
		return(WAIT_FAILED);
	}

	wait=handle->ops->wait(handle, NULL, timeout);
	if(wait==TRUE) {
		/* Object signalled before timeout expired */
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Object %p signalled",
			  handle);
#endif
		return(WAIT_OBJECT_0);
	} else {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Object %p wait timed out",
			  handle);
#endif
		return(WAIT_TIMEOUT);
	}
}

/**
 * WaitForMultipleObjects:
 * @numobjects: The number of objects in @handles. The maximum allowed
 * is %MAXIMUM_WAIT_OBJECTS.
 * @handles: An array of object handles.  Duplicates are not allowed.
 * @waitall: If %TRUE, this function waits until all of the handles
 * are signalled.  If %FALSE, this function returns when any object is
 * signalled.
 * @timeout: The maximum time in milliseconds to wait for.
 * 
 * This function returns when either one or more of @handles is
 * signalled, or @timeout ms elapses.  If @timeout is zero, the state
 * of each item of @handles is tested and the function returns
 * immediately.  If @timeout is %INFINITE, the function waits forever.
 *
 * Return value: %WAIT_OBJECT_0 to %WAIT_OBJECT_0 + @numobjects - 1 -
 * if @waitall is %TRUE, indicates that all objects are signalled.  If
 * @waitall is %FALSE, the return value minus %WAIT_OBJECT_0 indicates
 * the first index into @handles of the objects that are signalled.
 * %WAIT_ABANDONED_0 to %WAIT_ABANDONED_0 + @numobjects - 1 - if
 * @waitall is %TRUE, indicates that all objects are signalled, and at
 * least one object is an abandoned mutex object (See
 * WaitForSingleObject() for a description of abandoned mutexes.)  If
 * @waitall is %FALSE, the return value minus %WAIT_ABANDONED_0
 * indicates the first index into @handles of an abandoned mutex.
 * %WAIT_TIMEOUT - The @timeout interval elapsed and no objects in
 * @handles are signalled.  %WAIT_FAILED - an error occurred.
 */
guint32 WaitForMultipleObjects(guint32 numobjects, WapiHandle **handles,
			       gboolean waitall, guint32 timeout)
{
	WaitQueueItem *item;
	GHashTable *dups;
	gboolean duplicate=FALSE, bogustype=FALSE;
	gboolean wait;
	guint i;
	guint32 lowest;
	
	pthread_once(&wait_once, wait_init);
	
	if(numobjects>MAXIMUM_WAIT_OBJECTS) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Too many handles: %d",
			  numobjects);
#endif

		return(WAIT_FAILED);
	}
	
	/* Check for duplicates */
	dups=g_hash_table_new(g_direct_hash, g_direct_equal);
	for(i=0; i<numobjects; i++) {
		gpointer exists=g_hash_table_lookup(dups, handles[i]);
		if(exists!=NULL) {
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": Handle %p duplicated", handles[i]);
#endif

			duplicate=TRUE;
			break;
		}

		if(handles[i]->ops->wait_multiple==NULL) {
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": Handle %p can't be waited for (type %d)",
				  handles[i], handles[i]->type);
#endif

			bogustype=TRUE;
		}

		g_hash_table_insert(dups, handles[i], handles[i]);
	}
	g_hash_table_destroy(dups);

	if(duplicate==TRUE) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Returning due to duplicates");
#endif

		return(WAIT_FAILED);
	}

	if(bogustype==TRUE) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION
			  ": Returning due to bogus type");
#endif

		return(WAIT_FAILED);
	}
	
	item=wait_item_new(timeout, waitall);

	/* Sort the handles by type */
	for(i=0; i<numobjects; i++) {
		g_ptr_array_add(item->handles[handles[i]->type], handles[i]);
		g_array_append_val(item->waitindex[handles[i]->type], i);
	}
	
	wait=wait_for_item(item, &lowest);
	wait_dequeue_item(item);
	wait_item_destroy(item);

	if(wait==FALSE) {
		/* Wait timed out */
		return(WAIT_TIMEOUT);
	}

	return(WAIT_OBJECT_0+lowest);
}
