/*
 * collection.h:  Garbage collection for handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2004 Novell, Inc.
 */

#ifndef _WAPI_COLLECTION_H_
#define _WAPI_COLLECTION_H_

#include <glib.h>

#define _WAPI_HANDLE_COLLECTION_UPDATE_INTERVAL		10
#define _WAPI_HANDLE_COLLECTION_EXPIRED_INTERVAL	60

#include <mono/io-layer/shared.h>

#define _WAPI_HANDLE_COLLECTION_UNSAFE		\
	{					\
		int _wapi_thr_ret;		\
						\
		_wapi_thr_ret = _wapi_shm_sem_lock (_WAPI_SHARED_SEM_COLLECTION);	\
		g_assert(_wapi_thr_ret == 0);

#define _WAPI_HANDLE_COLLECTION_SAFE		\
		_wapi_thr_ret = _wapi_shm_sem_unlock (_WAPI_SHARED_SEM_COLLECTION); \
		g_assert (_wapi_thr_ret == 0);	\
	}
	
extern void _wapi_collection_init (void);
extern void _wapi_handle_collect (void);

#endif /* _WAPI_COLLECTION_H_ */
