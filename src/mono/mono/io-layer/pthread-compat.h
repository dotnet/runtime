#ifndef _WAPI_PTHREAD_COMPAT_H_
#define _WAPI_PTHREAD_COMPAT_H_

#include <config.h>
#include <pthread.h>
#include <time.h>

#ifndef HAVE_PTHREAD_MUTEX_TIMEDLOCK
extern int pthread_mutex_timedlock(pthread_mutex_t *mutex,
				   const struct timespec *timeout);
#endif /* HAVE_PTHREAD_MUTEX_TIMEDLOCK */

#endif /* _WAPI_PTHREAD_COMPAT_H_ */
