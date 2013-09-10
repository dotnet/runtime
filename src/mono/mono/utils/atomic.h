/*
 * atomic.h:  Atomic operations
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2012 Xamarin Inc
 */

#ifndef _WAPI_ATOMIC_H_
#define _WAPI_ATOMIC_H_

#if defined(__NetBSD__)
#include <sys/param.h>

#if __NetBSD_Version__ > 499004000
#include <sys/atomic.h>
#define HAVE_ATOMIC_OPS
#endif

#endif

#include "config.h"
#include <glib.h>

#ifdef ENABLE_EXTENSION_MODULE
#include "../../../mono-extensions/mono/utils/atomic.h"
#endif

/* On Windows, we always use the functions provided by the Windows API. */
#if defined(__WIN32__) || defined(_WIN32)

#include <windows.h>
#define HAS_64BITS_ATOMICS 1

/* mingw is missing InterlockedCompareExchange64 () from winbase.h */
#ifdef __MINGW32__
static inline gint64 InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return __sync_val_compare_and_swap (dest, comp, exch);
}
#endif

/* Prefer GCC atomic ops if the target supports it (see configure.in). */
#elif defined(USE_GCC_ATOMIC_OPS)

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp)
{
	return __sync_val_compare_and_swap (dest, comp, exch);
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
	return __sync_val_compare_and_swap (dest, comp, exch);
}

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	return __sync_add_and_fetch (val, 1);
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	return __sync_add_and_fetch (val, -1);
}

static inline gint32 InterlockedExchange(volatile gint32 *val, gint32 new_val)
{
	gint32 old_val;
	do {
		old_val = *val;
	} while (__sync_val_compare_and_swap (val, old_val, new_val) != old_val);
	return old_val;
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *val,
						  gpointer new_val)
{
	gpointer old_val;
	do {
		old_val = *val;
	} while (__sync_val_compare_and_swap (val, old_val, new_val) != old_val);
	return old_val;
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
	return __sync_fetch_and_add (val, add);
}

#if defined (TARGET_OSX)
#define BROKEN_64BIT_ATOMICS_INTRINSIC 1
#endif


#if !defined (BROKEN_64BIT_ATOMICS_INTRINSIC)
#define HAS_64BITS_ATOMICS 1

static inline gint64 InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return __sync_val_compare_and_swap (dest, comp, exch);
}

#endif


#elif defined(__NetBSD__) && defined(HAVE_ATOMIC_OPS)

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
       gint32 exch, gint32 comp)
{
       return atomic_cas_32((uint32_t*)dest, comp, exch);
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
       return atomic_cas_ptr(dest, comp, exch);
}

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
       return atomic_inc_32_nv((uint32_t*)val);
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
       return atomic_dec_32_nv((uint32_t*)val);
}

static inline gint32 InterlockedExchange(volatile gint32 *val, gint32 new_val)
{
       return atomic_swap_32((uint32_t*)val, new_val);
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *val,
               gpointer new_val)
{
       return atomic_swap_ptr(val, new_val);
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
       return atomic_add_32_nv((uint32_t*)val, add) - add;
}

#elif (defined(sparc) || defined (__sparc__)) && defined(__GNUC__)

G_GNUC_UNUSED 
static inline gint32 InterlockedCompareExchange(volatile gint32 *_dest, gint32 _exch, gint32 _comp)
{
       register volatile gint32 *dest asm("g1") = _dest;
       register gint32 comp asm("o4") = _comp;
       register gint32 exch asm("o5") = _exch;

       __asm__ __volatile__(
               /* cas [%%g1], %%o4, %%o5 */
               ".word 0xdbe0500c"
               : "=r" (exch)
               : "0" (exch), "r" (dest), "r" (comp)
               : "memory");

       return exch;
}

G_GNUC_UNUSED 
static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *_dest, gpointer _exch, gpointer _comp)
{
       register volatile gpointer *dest asm("g1") = _dest;
       register gpointer comp asm("o4") = _comp;
       register gpointer exch asm("o5") = _exch;

       __asm__ __volatile__(
#ifdef SPARCV9
               /* casx [%%g1], %%o4, %%o5 */
               ".word 0xdbf0500c"
#else
               /* cas [%%g1], %%o4, %%o5 */
               ".word 0xdbe0500c"
#endif
               : "=r" (exch)
               : "0" (exch), "r" (dest), "r" (comp)
               : "memory");

       return exch;
}

G_GNUC_UNUSED 
static inline gint32 InterlockedIncrement(volatile gint32 *_dest)
{
       register volatile gint32 *dest asm("g1") = _dest;
       register gint32 tmp asm("o4");
       register gint32 ret asm("o5");

       __asm__ __volatile__(
               "1:     ld      [%%g1], %%o4\n\t"
               "       add     %%o4, 1, %%o5\n\t"
               /*      cas     [%%g1], %%o4, %%o5 */
               "       .word   0xdbe0500c\n\t"
               "       cmp     %%o4, %%o5\n\t"
               "       bne     1b\n\t"
               "        add    %%o5, 1, %%o5"
               : "=&r" (tmp), "=&r" (ret)
               : "r" (dest)
               : "memory", "cc");

        return ret;
}

G_GNUC_UNUSED 
static inline gint32 InterlockedDecrement(volatile gint32 *_dest)
{
       register volatile gint32 *dest asm("g1") = _dest;
       register gint32 tmp asm("o4");
       register gint32 ret asm("o5");

       __asm__ __volatile__(
               "1:     ld      [%%g1], %%o4\n\t"
               "       sub     %%o4, 1, %%o5\n\t"
               /*      cas     [%%g1], %%o4, %%o5 */
               "       .word   0xdbe0500c\n\t"
               "       cmp     %%o4, %%o5\n\t"
               "       bne     1b\n\t"
               "        sub    %%o5, 1, %%o5"
               : "=&r" (tmp), "=&r" (ret)
               : "r" (dest)
               : "memory", "cc");

        return ret;
}

G_GNUC_UNUSED
static inline gint32 InterlockedExchange(volatile gint32 *_dest, gint32 exch)
{
       register volatile gint32 *dest asm("g1") = _dest;
       register gint32 tmp asm("o4");
       register gint32 ret asm("o5");

       __asm__ __volatile__(
               "1:     ld      [%%g1], %%o4\n\t"
               "       mov     %3, %%o5\n\t"
               /*      cas     [%%g1], %%o4, %%o5 */
               "       .word   0xdbe0500c\n\t"
               "       cmp     %%o4, %%o5\n\t"
               "       bne     1b\n\t"
               "        nop"
               : "=&r" (tmp), "=&r" (ret)
               : "r" (dest), "r" (exch)
               : "memory", "cc");

        return ret;
}

G_GNUC_UNUSED
static inline gpointer InterlockedExchangePointer(volatile gpointer *_dest, gpointer exch)
{
       register volatile gpointer *dest asm("g1") = _dest;
       register gpointer tmp asm("o4");
       register gpointer ret asm("o5");

       __asm__ __volatile__(
#ifdef SPARCV9
               "1:     ldx     [%%g1], %%o4\n\t"
#else
               "1:     ld      [%%g1], %%o4\n\t"
#endif
               "       mov     %3, %%o5\n\t"
#ifdef SPARCV9
               /*      casx    [%%g1], %%o4, %%o5 */
               "       .word   0xdbf0500c\n\t"
#else
               /*      cas     [%%g1], %%o4, %%o5 */
               "       .word   0xdbe0500c\n\t"
#endif
               "       cmp     %%o4, %%o5\n\t"
               "       bne     1b\n\t"
               "        nop"
               : "=&r" (tmp), "=&r" (ret)
               : "r" (dest), "r" (exch)
               : "memory", "cc");

        return ret;
}

G_GNUC_UNUSED
static inline gint32 InterlockedExchangeAdd(volatile gint32 *_dest, gint32 add)
{
       register volatile gint32 *dest asm("g1") = _dest;
       register gint32 tmp asm("o4");
       register gint32 ret asm("o5");

       __asm__ __volatile__(
               "1:     ld      [%%g1], %%o4\n\t"
               "       add     %%o4, %3, %%o5\n\t"
               /*      cas     [%%g1], %%o4, %%o5 */
               "       .word   0xdbe0500c\n\t"
               "       cmp     %%o4, %%o5\n\t"
               "       bne     1b\n\t"
               "        add    %%o5, %3, %%o5"
               : "=&r" (tmp), "=&r" (ret)
               : "r" (dest), "r" (add)
               : "memory", "cc");

        return ret;
}

#elif __s390x__

static inline gint32 
InterlockedCompareExchange(volatile gint32 *dest,
			   gint32 exch, gint32 comp)
{
	gint32 old;

	__asm__ __volatile__ ("\tLA\t1,%0\n"
			      "\tLR\t%1,%3\n"
			      "\tCS\t%1,%2,0(1)\n"
			      : "+m" (*dest), "=&r" (old)
			      : "r" (exch), "r" (comp)
			      : "1", "cc");	
	return(old);
}

static inline gpointer 
InterlockedCompareExchangePointer(volatile gpointer *dest, 
				  gpointer exch, 
			          gpointer comp)
{
	gpointer old;

	__asm__ __volatile__ ("\tLA\t1,%0\n"
			      "\tLGR\t%1,%3\n"
			      "\tCSG\t%1,%2,0(1)\n"
			      : "+m" (*dest), "=&r" (old)
			      : "r" (exch), "r" (comp)
			      : "1", "cc");

	return(old);
}

static inline gint32 
InterlockedIncrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("\tLA\t2,%1\n"
			      "0:\tLGF\t%0,%1\n"
			      "\tLGFR\t1,%0\n"
			      "\tAGHI\t1,1\n"
			      "\tCS\t%0,1,0(2)\n"
			      "\tJNZ\t0b\n"
			      "\tLGFR\t%0,1"
			      : "=r" (tmp), "+m" (*val)
			      : : "1", "2", "cc");

	return(tmp);
}

static inline gint32 
InterlockedDecrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("\tLA\t2,%1\n"
			      "0:\tLGF\t%0,%1\n"
			      "\tLGFR\t1,%0\n"
			      "\tAGHI\t1,-1\n"
			      "\tCS\t%0,1,0(2)\n"
			      "\tJNZ\t0b\n"
			      "\tLGFR\t%0,1"
			      : "=r" (tmp), "+m" (*val)
			      : : "1", "2", "cc");

	return(tmp);
}

static inline gint32 
InterlockedExchange(volatile gint32 *val, gint32 new_val)
{
	gint32 ret;
	
	__asm__ __volatile__ ("\tLA\t1,%0\n"
			      "0:\tL\t%1,%0\n"
			      "\tCS\t%1,%2,0(1)\n"
			      "\tJNZ\t0b"
			      : "+m" (*val), "=&r" (ret)
			      : "r" (new_val)
			      : "1", "cc");

	return(ret);
}

static inline gpointer
InterlockedExchangePointer(volatile gpointer *val, gpointer new_val)
{
	gpointer ret;
	
	__asm__ __volatile__ ("\tLA\t1,%0\n"
			      "0:\tLG\t%1,%0\n"
			      "\tCSG\t%1,%2,0(1)\n"
			      "\tJNZ\t0b"
			      : "+m" (*val), "=&r" (ret)
			      : "r" (new_val)
			      : "1", "cc");

	return(ret);
}

static inline gint32 
InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
	gint32 ret;

	__asm__ __volatile__ ("\tLA\t2,%1\n"
			      "0:\tLGF\t%0,%1\n"
			      "\tLGFR\t1,%0\n"
			      "\tAGR\t1,%2\n"
			      "\tCS\t%0,1,0(2)\n"
			      "\tJNZ\t0b"
			      : "=&r" (ret), "+m" (*val)
			      : "r" (add) 
			      : "1", "2", "cc");
	
	return(ret);
}

#elif defined(__ia64__)

#ifdef __INTEL_COMPILER
#include <ia64intrin.h>
#endif

static inline gint32 InterlockedCompareExchange(gint32 volatile *dest,
						gint32 exch, gint32 comp)
{
	gint32 old;
	guint64 real_comp;

#ifdef __INTEL_COMPILER
	old = _InterlockedCompareExchange (dest, exch, comp);
#else
	/* cmpxchg4 zero extends the value read from memory */
	real_comp = (guint64)(guint32)comp;
	asm volatile ("mov ar.ccv = %2 ;;\n\t"
				  "cmpxchg4.acq %0 = [%1], %3, ar.ccv\n\t"
				  : "=r" (old) : "r" (dest), "r" (real_comp), "r" (exch));
#endif

	return(old);
}

static inline gpointer InterlockedCompareExchangePointer(gpointer volatile *dest,
						gpointer exch, gpointer comp)
{
	gpointer old;

#ifdef __INTEL_COMPILER
	old = _InterlockedCompareExchangePointer (dest, exch, comp);
#else
	asm volatile ("mov ar.ccv = %2 ;;\n\t"
				  "cmpxchg8.acq %0 = [%1], %3, ar.ccv\n\t"
				  : "=r" (old) : "r" (dest), "r" (comp), "r" (exch));
#endif

	return(old);
}

static inline gint32 InterlockedIncrement(gint32 volatile *val)
{
#ifdef __INTEL_COMPILER
	return _InterlockedIncrement (val);
#else
	gint32 old;

	do {
		old = *val;
	} while (InterlockedCompareExchange (val, old + 1, old) != old);

	return old + 1;
#endif
}

static inline gint32 InterlockedDecrement(gint32 volatile *val)
{
#ifdef __INTEL_COMPILER
	return _InterlockedDecrement (val);
#else
	gint32 old;

	do {
		old = *val;
	} while (InterlockedCompareExchange (val, old - 1, old) != old);

	return old - 1;
#endif
}

static inline gint32 InterlockedExchange(gint32 volatile *dest, gint32 new_val)
{
#ifdef __INTEL_COMPILER
	return _InterlockedExchange (dest, new_val);
#else
	gint32 res;

	do {
		res = *dest;
	} while (InterlockedCompareExchange (dest, new_val, res) != res);

	return res;
#endif
}

static inline gpointer InterlockedExchangePointer(gpointer volatile *dest, gpointer new_val)
{
#ifdef __INTEL_COMPILER
	return (gpointer)_InterlockedExchange64 ((gint64*)dest, (gint64)new_val);
#else
	gpointer res;

	do {
		res = *dest;
	} while (InterlockedCompareExchangePointer (dest, new_val, res) != res);

	return res;
#endif
}

static inline gint32 InterlockedExchangeAdd(gint32 volatile *val, gint32 add)
{
	gint32 old;

#ifdef __INTEL_COMPILER
	old = _InterlockedExchangeAdd (val, add);
#else
	do {
		old = *val;
	} while (InterlockedCompareExchange (val, old + add, old) != old);

	return old;
#endif
}

#else

#define WAPI_NO_ATOMIC_ASM

extern gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp);
extern gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp);
extern gint32 InterlockedIncrement(volatile gint32 *dest);
extern gint32 InterlockedDecrement(volatile gint32 *dest);
extern gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch);
extern gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch);
extern gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add);

#endif

#ifndef HAS_64BITS_ATOMICS
extern gint64 InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp);
#endif

#endif /* _WAPI_ATOMIC_H_ */
