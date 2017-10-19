/**
 * \file
 * Workarounds for atomic operations for platforms that dont have
 * really atomic asm functions in atomic.h
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/utils/atomic.h>
#include <mono/utils/mono-compiler.h>

#if defined (WAPI_NO_ATOMIC_ASM) || defined (BROKEN_64BIT_ATOMICS_INTRINSIC)

#include <pthread.h>

static pthread_mutex_t spin G_GNUC_UNUSED = PTHREAD_MUTEX_INITIALIZER;

#define NEED_64BIT_CMPXCHG_FALLBACK

#endif

#ifdef WAPI_NO_ATOMIC_ASM

gint32 mono_atomic_cas_i32(volatile gint32 *dest, gint32 exch,
				  gint32 comp)
{
	gint32 old;
	int ret;
	
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

gpointer mono_atomic_cas_ptr(volatile gpointer *dest,
					   gpointer exch, gpointer comp)
{
	gpointer old;
	int ret;
	
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

gint32 mono_atomic_add_i32(volatile gint32 *dest, gint32 add)
{
	gint32 ret;
	int thr_ret;

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

gint64 mono_atomic_add_i64(volatile gint64 *dest, gint64 add)
{
	gint64 ret;
	int thr_ret;

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

gint32 mono_atomic_inc_i32(volatile gint32 *dest)
{
	gint32 ret;
	int thr_ret;
	
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

gint64 mono_atomic_inc_i64(volatile gint64 *dest)
{
	gint64 ret;
	int thr_ret;

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

gint32 mono_atomic_dec_i32(volatile gint32 *dest)
{
	gint32 ret;
	int thr_ret;
	
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

gint64 mono_atomic_dec_i64(volatile gint64 *dest)
{
	gint64 ret;
	int thr_ret;

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

gint32 mono_atomic_xchg_i32(volatile gint32 *dest, gint32 exch)
{
	gint32 ret;
	int thr_ret;
	
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

gint64 mono_atomic_xchg_i64(volatile gint64 *dest, gint64 exch)
{
	gint64 ret;
	int thr_ret;

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

gpointer mono_atomic_xchg_ptr(volatile gpointer *dest, gpointer exch)
{
	gpointer ret;
	int thr_ret;
	
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

gint32 mono_atomic_fetch_add_i32(volatile gint32 *dest, gint32 add)
{
	gint32 ret;
	int thr_ret;
	
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

gint64 mono_atomic_fetch_add_i64(volatile gint64 *dest, gint64 add)
{
	gint64 ret;
	int thr_ret;
	
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

gint8 mono_atomic_load_i8(volatile gint8 *src)
{
	gint8 ret;
	int thr_ret;
	
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

gint16 mono_atomic_load_i16(volatile gint16 *src)
{
	gint16 ret;
	int thr_ret;
	
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

gint32 mono_atomic_load_i32(volatile gint32 *src)
{
	gint32 ret;
	int thr_ret;
	
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

gint64 mono_atomic_load_i64(volatile gint64 *src)
{
	gint64 ret;
	int thr_ret;
	
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

gpointer mono_atomic_load_ptr(volatile gpointer *src)
{
	gpointer ret;
	int thr_ret;
	
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

void mono_atomic_store_i8(volatile gint8 *dst, gint8 val)
{
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dst=val;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
}

void mono_atomic_store_i16(volatile gint16 *dst, gint16 val)
{
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dst=val;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
}

void mono_atomic_store_i32(volatile gint32 *dst, gint32 val)
{
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dst=val;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
}

void mono_atomic_store_i64(volatile gint64 *dst, gint64 val)
{
	int thr_ret;
	
	pthread_cleanup_push ((void(*)(void *))pthread_mutex_unlock,
			      (void *)&spin);
	thr_ret = pthread_mutex_lock(&spin);
	g_assert (thr_ret == 0);

	*dst=val;
	
	thr_ret = pthread_mutex_unlock(&spin);
	g_assert (thr_ret == 0);
	
	pthread_cleanup_pop (0);
}

void mono_atomic_store_ptr(volatile gpointer *dst, gpointer val)
{
	int thr_ret;
	
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

/* The compiler breaks if this code is in the header... */

gint64
mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return __sync_val_compare_and_swap (dest, comp, exch);
}

#elif defined (__arm__) && defined (HAVE_ARMV7) && (defined(TARGET_IOS) || defined(TARGET_WATCHOS) || defined(TARGET_ANDROID))

#if defined (TARGET_IOS) || defined (TARGET_WATCHOS)

#ifndef __clang__
#error "Not supported."
#endif

gint64
mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return  __sync_val_compare_and_swap (dest, comp, exch);
}

#elif defined (TARGET_ANDROID)

/* Some Android systems can't find the 64-bit CAS intrinsic at runtime,
 * so we have to roll our own...
 */

gint64 mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp) __attribute__ ((__naked__));

gint64
mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	__asm__ (
		"push		{r4, r5, r6, r7}\n"
		"ldrd		r4, [sp, #16]\n"
		"dmb		sy\n"
	"1:\n"
		"ldrexd		r6, [r0]\n"
		"cmp		r7, r5\n"
		"cmpeq		r6, r4\n"
		"bne		2f\n"
		"strexd		r1, r2, [r0]\n"
		"cmp		r1, #0\n"
		"bne		1b\n"
	"2:\n"
		"dmb		sy\n"
		"mov		r0, r6\n"
		"mov		r1, r7\n"
		"pop		{r4, r5, r6, r7}\n"
		"bx			lr\n"
	);
}

#else

#error "Need a 64-bit CAS fallback!"

#endif

#else

gint64
mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	gint64 old;
	int ret;
	
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

#endif
#endif

#if !defined (WAPI_NO_ATOMIC_ASM) && !defined (BROKEN_64BIT_ATOMICS_INTRINSIC) && !defined (NEED_64BIT_CMPXCHG_FALLBACK)
MONO_EMPTY_SOURCE_FILE (atomic);
#endif
