#include <config.h>
#include <pthread.h>
#include <time.h>
#include <errno.h>
#include <sys/time.h>
#include <unistd.h>

#include "pthread-compat.h"

#ifndef HAVE_PTHREAD_MUTEX_TIMEDLOCK
/*
 * Implementation of timed mutex locking from the P1003.1d/D14 (July
 * 1999) draft spec, figure B-4.
 */
/**
 * pthread_mutex_timedlock:
 * @mutex: A pointer to a mutex to lock
 * @timeout: A pointer to a struct timespec giving an absolute time to
 * time out at.
 *
 * This is a compatibility implementation for C libraries that are
 * lacking this function.
 *
 * Tries to lock @mutex, but times out when the time specified in
 * @timeout is reached.
 *
 * Return value: zero on success, ETIMEDOUT on timeout.
 */
int pthread_mutex_timedlock(pthread_mutex_t *mutex,
			    const struct timespec *timeout)
{
	struct timeval timenow;
	struct timespec sleepytime;

	/* This is just to avoid a completely busy wait */
	sleepytime.tv_sec=0;
	sleepytime.tv_nsec=10000;	/* 10ms */
	
	while(pthread_mutex_trylock(mutex)==EBUSY) {
		gettimeofday(&timenow, NULL);
		
		if(timenow.tv_sec >= timeout->tv_sec &&
		   (timenow.tv_usec * 1000) >= timeout->tv_nsec) {
			return(ETIMEDOUT);
		}
		
		nanosleep(&sleepytime, NULL);
	}
	
	return(0);
}
#endif /* !HAVE_PTHREAD_MUTEX_TIMEDLOCK */

