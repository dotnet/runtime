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

#include <mono/utils/atomic.h>

#if defined (WAPI_NO_ATOMIC_ASM) || defined (BROKEN_64BIT_ATOMICS_INTRINSIC)

#include <pthread.h>

static pthread_mutex_t spin = PTHREAD_MUTEX_INITIALIZER;

#define NEED_64BIT_CMPXCHG_FALLBACK

#endif

#ifdef WAPI_NO_ATOMIC_ASM

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

gint32 InterlockedAdd(volatile gint32 *dest, gint32 add)
{
	gint32 ret;
	int thr_ret;

	mono_once(&spin_once, spin_init);

	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dest += add;
	ret= *dest;

	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);

	pthread_cleanup_pop (0);

	return(ret);
}

gint64 InterlockedAdd64(volatile gint64 *dest, gint64 add)
{
	gint64 ret;
	int thr_ret;

	mono_once(&spin_once, spin_init);

	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dest += add;
	ret= *dest;

	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);

	pthread_cleanup_pop (0);

	return(ret);
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

gint64 InterlockedIncrement64(volatile gint64 *dest)
{
	gint64 ret;
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

gint64 InterlockedDecrement64(volatile gint64 *dest)
{
	gint64 ret;
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

gint64 InterlockedExchange64(volatile gint64 *dest, gint64 exch)
{
	gint64 ret;
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

gint64 InterlockedExchangeAdd64(volatile gint64 *dest, gint64 add)
{
	gint64 ret;
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

gint32 InterlockedRead(volatile gint32 *src)
{
	gint32 ret;
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	ret= *src;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);

	pthread_cleanup_pop (0);

	return(ret);
}

gint64 InterlockedRead64(volatile gint64 *src)
{
	gint64 ret;
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	ret= *src;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);

	pthread_cleanup_pop (0);

	return(ret);
}

void InterlockedWrite(volatile gint32 *dst, gint32 val)
{
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dst=val;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
}

void InterlockedWrite64(volatile gint64 *dst, gint64 val)
{
	int thr_ret;
	
	mono_once(&spin_once, spin_init);
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dst=val;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
}

#endif

#if defined (NEED_64BIT_CMPXCHG_FALLBACK)

#if defined (TARGET_OSX)

gint64
InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return __sync_val_compare_and_swap (dest, comp, exch);
}

#elif defined (HAVE_64BIT_CMPXCHG_FALLBACK)

#ifdef ENABLE_EXTENSION_MODULE
#include "../../../mono-extensions/mono/utils/atomic.c"
#endif

#else

gint64
InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	gint64 old;

	pthread_mutex_lock (&spin);

	old = *dest;
	if(old == comp)
		*dest = exch;

	pthread_mutex_unlock (&spin);
	return old;
}

#endif

#endif
