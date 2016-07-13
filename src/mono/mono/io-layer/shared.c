/*
 * shared.c:  Shared memory handling, and daemon launching
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002-2006 Novell, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/io-layer/wapi-private.h>
#include <mono/io-layer/shared.h>
#include <mono/utils/mono-os-mutex.h>

#define DEBUGLOG(...)
//#define DEBUGLOG(...) g_message(__VA_ARGS__);

static mono_mutex_t noshm_sems[_WAPI_SHARED_SEM_COUNT];

void
_wapi_shm_semaphores_init (void)
{
	int i;
	for (i = 0; i < _WAPI_SHARED_SEM_COUNT; i++) 
		mono_os_mutex_init (&noshm_sems [i]);
}

int
_wapi_shm_sem_lock (int sem)
{
	DEBUGLOG ("%s: locking nosem %d", __func__, sem);
	mono_os_mutex_lock (&noshm_sems[sem]);
	return 0;
}

int
_wapi_shm_sem_trylock (int sem)
{
	DEBUGLOG ("%s: trying to lock nosem %d", __func__, sem);
	return mono_os_mutex_trylock (&noshm_sems[sem]);
}

int
_wapi_shm_sem_unlock (int sem)
{
	DEBUGLOG ("%s: unlocking nosem %d", __func__, sem);
	mono_os_mutex_unlock (&noshm_sems[sem]);
	return 0;
}
