/*
 * shared.h:  Shared memory handle, and daemon launching
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_SHARED_H_
#define _WAPI_SHARED_H_

#include <mono/io-layer/wapi-private.h>

extern gpointer _wapi_shm_attach (void);
extern gpointer _wapi_fileshare_shm_attach (void);

#endif /* _WAPI_SHARED_H_ */
