/*
 * shared.h:  Shared memory handle, and daemon launching
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#ifndef _WAPI_SHARED_H_
#define _WAPI_SHARED_H_

#include <mono/io-layer/wapi-private.h>

typedef enum {
	WAPI_SHM_DATA,
	WAPI_SHM_FILESHARE
} _wapi_shm_t;

extern gboolean _wapi_shm_disabled;

extern gpointer _wapi_shm_attach (_wapi_shm_t type);
extern void _wapi_shm_detach (_wapi_shm_t type);
extern gboolean _wapi_shm_enabled (void);
extern void _wapi_shm_semaphores_init (void);
extern void _wapi_shm_semaphores_remove (void);
extern int _wapi_shm_sem_lock (int sem);
extern int _wapi_shm_sem_trylock (int sem);
extern int _wapi_shm_sem_unlock (int sem);

#endif /* _WAPI_SHARED_H_ */
