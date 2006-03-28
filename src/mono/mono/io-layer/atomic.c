/*
 * atomic.c:  Workarounds for atomic operations for platforms that dont have
 *	      really atomic asm functions in atomic.h
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>

#include "mono/io-layer/wapi.h"

#ifndef WAPI_ATOMIC_ASM

static pthread_mutex_t spin = PTHREAD_MUTEX_INITIALIZER;
static mono_once_t spin_once=MONO_ONCE_INIT;

static void spin_init(void)
{
	g_warning("Using non-atomic functions!  Expect race conditions when using process-shared handles!");
}

gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch,
				  gint32 comp)
{
	gint32 old;
	int ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	ret = pthread_mutex_lock(&spin);
	g_assert (ret == 0);
	
	old= *dest;
	if(old==comp) {
		*dest=exch;
	}
	
	ret = pthread_mutex_unlock(&spin);
	g_assert (ret == 0);
	
	pthread_cleanup_pop (0);

	return(old);
}

gpointer InterlockedCompareExchangePointer(volatile gpointer *dest,
					   gpointer exch, gpointer comp)
{
	gpointer old;
	int ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	ret = pthread_mutex_lock(&spin);
	g_assert (ret == 0);
	
	old= *dest;
	if(old==comp) {
		*dest=exch;
	}
	
	ret = pthread_mutex_unlock(&spin);
	g_assert (ret == 0);
	
	pthread_cleanup_pop (0);

	return(old);
}

gint32 InterlockedIncrement(volatile gint32 *dest)
{
	gint32 ret;
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	(*dest)++;
	ret= *dest;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	return(ret);
}

gint32 InterlockedDecrement(volatile gint32 *dest)
{
	gint32 ret;
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);
	
	(*dest)--;
	ret= *dest;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	return(ret);
}

gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
	gint32 ret;
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	ret=*dest;
	*dest=exch;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	return(ret);
}

gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch)
{
	gpointer ret;
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);
	
	ret=*dest;
	*dest=exch;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
	
	return(ret);
}

gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
	gint32 ret;
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	ret= *dest;
	*dest+=add;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);

	pthread_cleanup_pop (0);

	return(ret);
}

#endif
