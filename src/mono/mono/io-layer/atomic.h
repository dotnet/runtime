#ifndef _WAPI_ATOMIC_H_
#define _WAPI_ATOMIC_H_

#include <glib.h>

#include "mono/io-layer/wapi.h"

#ifdef __i386__
#define WAPI_ATOMIC_ASM

/*
 * NB: The *Pointer() functions here assume that
 * sizeof(pointer)==sizeof(gint32)
 */

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp)
{
	gint32 old;

	__asm__ __volatile__ ("lock; cmpxchgl %2, %0"
			      : "=m" (*dest), "=a" (old)
			      : "r" (exch), "0" (*dest), "a" (comp));	
	return(old);
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
	gpointer old;

	__asm__ __volatile__ ("lock; cmpxchgl %2, %0"
			      : "=m" (*dest), "=a" (old)
			      : "r" (exch), "0" (*dest), "a" (comp));	
	return(old);
}

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	__asm__ __volatile__ ("lock; incl %0"
			      : "=m" (*val)
			      : "0" (*val));

	/* Potential race condition here if *val gets incremented again between
	 * the asm and the return.
	 */
	return(*val);
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	__asm__ __volatile__ ("lock; decl %0"
			      : "=m" (*val)
			      : "0" (*val));

	/* Potential race condition here if *val gets decremented again between
	 * the asm and the return.
	 */
	return(*val);
}

static inline gint32 InterlockedExchange(volatile gint32 *val, gint32 new)
{
	gint32 ret;
	
	__asm__ __volatile__ ("lock; xchgl %0, %1"
			      : "=r" (ret), "=m" (*val)
			      : "0" (new), "1" (*val));

	return(ret);
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *val,
						  gpointer new)
{
	gpointer ret;
	
	__asm__ __volatile__ ("lock; xchgl %0, %1"
			      : "=r" (ret), "=m" (*val)
			      : "0" (new), "1" (*val));

	return(ret);
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
	gint32 ret;
	
	__asm__ __volatile__ ("lock; xaddl %0, %1"
			      : "=r" (ret), "=m" (*val)
			      : "0" (add), "1" (*val));
	
	return(ret);
}
#else
extern gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp);
extern gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp);
extern gint32 InterlockedIncrement(volatile gint32 *dest);
extern gint32 InterlockedDecrement(volatile gint32 *dest);
extern gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch);
extern gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch);
extern gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add);
#endif

#endif /* _WAPI_ATOMIC_H_ */
