/*
 * mono-spinlock.h:  Lightweight spinlocks, for internal use only
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_MONO_SPINLOCK_H_
#define _WAPI_MONO_SPINLOCK_H_

#include <glib.h>

#include <mono/io-layer/wapi.h>

#define MONO_SPIN_LOCK(lock)	while((InterlockedCompareExchange(&lock, 1, 0))!=0)
#define MONO_SPIN_UNLOCK(lock)	lock=0

#endif /* _WAPI_MONO_SPINLOCK_H_ */
