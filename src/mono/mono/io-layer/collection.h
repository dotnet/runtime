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

G_BEGIN_DECLS

#define _WAPI_HANDLE_COLLECTION_UPDATE_INTERVAL		10
#define _WAPI_HANDLE_COLLECTION_EXPIRED_INTERVAL	60

#include <mono/io-layer/shared.h>

extern void _wapi_collection_init (void);
extern void _wapi_handle_collect (void);

G_END_DECLS

#endif /* _WAPI_COLLECTION_H_ */
