#ifndef __MONO_SGENGC_H__
#define __MONO_SGENGC_H__

/* pthread impl */
#include <pthread.h>

#define ARCH_THREAD_TYPE pthread_t
#define ARCH_GET_THREAD pthread_self
#define ARCH_THREAD_EQUALS(a,b) pthread_equal (a, b)

/*
 * Recursion is not allowed for the thread lock.
 */
#define LOCK_DECLARE(name) pthread_mutex_t name = PTHREAD_MUTEX_INITIALIZER
#define LOCK_INIT(name)
#define LOCK_GC pthread_mutex_lock (&gc_mutex)
#define UNLOCK_GC pthread_mutex_unlock (&gc_mutex)

/* non-pthread will need to provide their own version of start/stop */
#define USE_SIGNAL_BASED_START_STOP_WORLD 1
/* we intercept pthread_create calls to know which threads exist */
#define USE_PTHREAD_INTERCEPT 1

#endif /* __MONO_SGENGC_H__ */

