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
#define _WAPI_HANDLE_COLLECTION_EXPIRED_INTERVAL	300

#define _WAPI_HANDLE_COLLECTION_UNSAFE				\
	{							\
		guint32 _wapi_save_start;			\
		int _wapi_thr_ret;					\
									\
		do {							\
			_wapi_save_start = (guint32)(time(NULL) & 0xFFFFFFFF);\
									\
			_wapi_thr_ret = _wapi_timestamp_exclusion (&_wapi_shared_layout->master_timestamp, _wapi_save_start); \
			if (_wapi_thr_ret == EBUSY) {			\
				_wapi_handle_spin (100);		\
			}						\
		} while (_wapi_thr_ret == EBUSY);			\
		g_assert (_wapi_thr_ret == 0);

		
#define _WAPI_HANDLE_COLLECTION_SAFE				\
		_wapi_thr_ret = _wapi_timestamp_release (&_wapi_shared_layout->master_timestamp, _wapi_save_start); \
	}
	

extern void _wapi_collection_init (void);
extern void _wapi_handle_collect (void);

#endif /* _WAPI_COLLECTION_H_ */
