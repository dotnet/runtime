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

extern void _wapi_shm_semaphores_init (void);
extern int _wapi_shm_sem_lock (int sem);
extern int _wapi_shm_sem_trylock (int sem);
extern int _wapi_shm_sem_unlock (int sem);

#endif /* _WAPI_SHARED_H_ */
