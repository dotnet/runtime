#include <config.h>
#include <glib.h>
#include <pthread.h>

#include "mono/io-layer/wapi.h"
#include "wapi-private.h"
#include "handles-private.h"

#define DEBUG

guint32 _wapi_handle_count_signalled(WaitQueueItem *item, WapiHandleType type)
{
	guint32 i, ret=0;
	
	/* Count how many of the interesting thread handles are signalled */
	for(i=0; i<item->handles[type]->len; i++) {
		WapiHandle *handle;

		handle=(WapiHandle *)g_ptr_array_index(item->handles[type], i);
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Checking handle %p",
			  handle);
#endif
		
		if(handle->signalled==TRUE) {
			guint32 idx;
			
#ifdef DEBUG
			g_message(G_GNUC_PRETTY_FUNCTION
				  ": Handle %p signalled", handle);
#endif

			idx=g_array_index(item->waitindex[type], guint32, i);
			_wapi_handle_set_lowest(item, idx);
			
			ret++;
		}
	}

#ifdef DEBUG
	g_message(G_GNUC_PRETTY_FUNCTION ": %d signalled handles", ret);
#endif

	return(ret);
}

void _wapi_handle_set_lowest(WaitQueueItem *item, guint32 idx)
{
	pthread_mutex_lock(&item->mutex);

	if(item->lowest_signal>idx) {
#ifdef DEBUG
		g_message(G_GNUC_PRETTY_FUNCTION ": Set %p lowest index to %d",
			  item, idx);
#endif
	
		item->lowest_signal=idx;
	}
	
	pthread_mutex_unlock(&item->mutex);
}

/**
 * CloseHandle:
 * @handle: The handle to release
 *
 * Closes and invalidates @handle, releasing any resources it
 * consumes.  When the last handle to a temporary or non-persistent
 * object is closed, that object can be deleted.  Closing the same
 * handle twice is an error.
 *
 * Return value: %TRUE on success, %FALSE otherwise.
 */
gboolean CloseHandle(WapiHandle *handle)
{
	g_return_val_if_fail(handle->ref>0, FALSE);
	
	handle->ref--;
	if(handle->ref==0) {
		if(handle->ops->close!=NULL) {
			handle->ops->close(handle);
		}
		
		g_free(handle);		/* maybe this should be in
					 * ops, cuurently ops->close()
					 * is being used to free
					 * handle data
					 */
	}
	
	return(TRUE);
}
