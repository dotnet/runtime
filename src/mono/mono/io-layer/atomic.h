/*
 * atomic.h:  Atomic operations
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _WAPI_ATOMIC_H_
#define _WAPI_ATOMIC_H_

#include <glib.h>

#include "mono/io-layer/wapi.h"

#ifdef __i386__
#define WAPI_ATOMIC_ASM

/*
 * NB: The *Pointer() functions here assume that
 * sizeof(pointer)==sizeof(gint32)
 *
 * NB2: These asm functions assume 486+ (some of the opcodes dont
 * exist on 386).  If this becomes an issue, we can get configure to
 * fall back to the non-atomic C versions of these calls.
 */

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp)
{
	gint32 old;

	__asm__ __volatile__ ("lock; cmpxchgl %2, %0"
			      : "=m" (*dest), "=a" (old)
			      : "r" (exch), "m" (*dest), "a" (comp));	
	return(old);
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
	gpointer old;

	__asm__ __volatile__ ("lock; cmpxchgl %2, %0"
			      : "=m" (*dest), "=a" (old)
			      : "r" (exch), "m" (*dest), "a" (comp));	
	return(old);
}

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("lock; xaddl %0, %1"
			      : "=r" (tmp), "=m" (*val)
			      : "0" (1), "m" (*val));

	return(tmp+1);
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("lock; xaddl %0, %1"
			      : "=r" (tmp), "=m" (*val)
			      : "0" (-1), "m" (*val));

	return(tmp-1);
}

/*
 * See
 * http://msdn.microsoft.com/library/en-us/dnmag00/html/win320700.asp?frame=true
 * for the reasons for using cmpxchg and a loop here.
 *
 * That url is no longer valid, but it's still in the google cache at the
 * moment: http://www.google.com/search?q=cache:http://msdn.microsoft.com/library/en-us/dnmag00/html/win320700.asp?frame=true
 */
static inline gint32 InterlockedExchange(volatile gint32 *val, gint32 new_val)
{
	gint32 ret;
	
	__asm__ __volatile__ ("1:; lock; cmpxchgl %2, %0; jne 1b"
			      : "=m" (*val), "=a" (ret)
			      : "r" (new_val), "m" (*val), "a" (*val));

	return(ret);
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *val,
						  gpointer new_val)
{
	gpointer ret;
	
	__asm__ __volatile__ ("1:; lock; cmpxchgl %2, %0; jne 1b"
			      : "=m" (*val), "=a" (ret)
			      : "r" (new_val), "m" (*val), "a" (*val));

	return(ret);
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
	gint32 ret;
	
	__asm__ __volatile__ ("lock; xaddl %0, %1"
			      : "=r" (ret), "=m" (*val)
			      : "0" (add), "m" (*val));
	
	return(ret);
}

#elif defined(sparc) || defined (__sparc__)
#define WAPI_ATOMIC_ASM

#define BEGIN_SPIN(tmp,lock) \
__asm__ __volatile__("1:        ldstub [%1],%0\n\t"  \
                             "          cmp %0, 0\n\t" \
                             "          bne 1b\n\t" \
                             "          nop" \
                             : "=&r" (tmp) \
                             : "r" (&lock) \
                             : "memory"); 

#define END_SPIN(lock) \
__asm__ __volatile__("stb	%%g0, [%0]"  \
                      : /* no outputs */ \
                      : "r" (&lock)\
                      : "memory");


static inline gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp)
{
	static unsigned char lock;
	int tmp;
	gint32 old;

	BEGIN_SPIN(tmp,lock)

	old = *dest;
	if (old==comp) {
		*dest=exch;
	}

	END_SPIN(lock)

	return(old);
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
        static unsigned char lock;
        int tmp;
        gpointer old;

        BEGIN_SPIN(tmp,lock)

        old = *dest;
        if (old==comp) {
                *dest=exch;
        }

        END_SPIN(lock)

        return(old);
}

static inline gint32 InterlockedIncrement(volatile gint32 *dest)
{
        static unsigned char lock;
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,lock)

        *dest++;
        ret = *dest;

        END_SPIN(lock)

        return(ret);
}

static inline gint32 InterlockedDecrement(volatile gint32 *dest)
{
        static unsigned char lock;
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,lock)

	*dest--;
        ret = *dest;

        END_SPIN(lock)

        return(ret);
}

static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
        static unsigned char lock;
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,lock)

        ret = *dest;
        *dest = exch;

        END_SPIN(lock)

        return(ret);
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch)
{
        static unsigned char lock;
        int tmp;
        gpointer ret;

        BEGIN_SPIN(tmp,lock)

        ret = *dest;
        *dest = exch;

        END_SPIN(lock)

        return(ret);
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
        static unsigned char lock;
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,lock)

        ret = *dest;
        *dest += add;

        END_SPIN(lock)

        return(ret);
}

#elif __s390__

#define WAPI_ATOMIC_ASM

static inline gint32 
InterlockedCompareExchange(volatile gint32 *dest,
			   gint32 exch, gint32 comp)
{
	gint32 old;

	__asm__ __volatile__ ("\tL\t%1,%0\n"
			      "\tCS\t%3,%2,%0\n"
			      : "=m" (*dest), "=r" (old)
			      : "r" (exch), "r" (comp)
			      : "cc");	
	return(old);
}

#define InterlockedCompareExchangePointer InterlockedCompareExchange

static inline gint32 
InterlockedIncrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("0:\tL\t%0,%1\n"
			      "\tLR\t1,%0\n"
			      "\tAHI\t1,1\n"
			      "0:\tCS\t%0,1,%1\n"
			      "\tJNZ\t0b"
			      : "=r" (tmp), "+m" (*val)
			      : : "1", "cc");

	return(tmp+1);
}

static inline gint32 
InterlockedDecrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("0:\tL\t%0,%1\n"
			      "\tLR\t1,%0\n"
			      "\tAHI\t1,-1\n"
			      "0:\tCS\t%0,1,%1\n"
			      "\tJNZ\t0b"
			      : "=r" (tmp), "+m" (*val)
			      : : "1", "cc");

	return(tmp-1);
}


static inline gint32 
InterlockedExchange(volatile gint32 *val, gint32 new_val)
{
	gint32 ret;
	
	__asm__ __volatile__ ("0:\tL\t%1,%0\n"
			      "\tCS\t%1,%2,%0\n"
			      "\tJNZ\t0b"
			      : "+m" (*val), "=r" (ret)
			      : "r" (new_val)
			      : "cc");

	return(ret);
}

#define InterlockedExchangePointer InterlockedExchange

static inline gint32 
InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
	gint32 ret;

	__asm__ __volatile__ ("0:\tL\t%0,%1\n"
			      "\tLR\t1,%0\n"
			      "\tAR\t1,%2\n"
			      "0:\tCS\t%0,1,%1\n"
			      "\tJNZ\t0b"
			      : "=r" (ret), "+m" (*val)
			      : "r" (add) 
			      : "1", "cc");
	
	return(ret);
}

#elif __ppc__
#define WAPI_ATOMIC_ASM

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	gint32 tmp;

	__asm__ __volatile__ ("\nL_ii_loop:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "addi   %0, %0, 1\n\t"
                              "stwcx. %0, 0, %2\n\t"
			      "bne-   L_ii_loop"
			      : "=r" (tmp) : "0" (tmp), "r" (val));
	return(tmp);
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	gint32 tmp;

	__asm__ __volatile__ ("\nL_id_loop:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "addi   %0, %0, -1\n\t"
                              "stwcx. %0, 0, %2\n\t"
			      "bne-   L_id_loop"
			      : "=r" (tmp) : "0" (tmp), "r" (val));
	return(tmp);
}

#define InterlockedCompareExchangePointer InterlockedCompareExchange

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp) {
	gint32 tmp = 0;

	__asm__ __volatile__ ("\nL_ice_loop:\n\t"
			     "lwarx   %0, 0, %1\n\t"
			     "cmpw    %2, %3\n\t" 
			     "bne-    L_ice_diff\n\t"
			     "stwcx.  %4, 0, %1\n\t"
			     "bne-    L_ice_loop\n"
			     "L_ice_diff:"
			     : "=r" (tmp)
			     : "r" (dest), "0" (tmp) ,"r" (comp), "r" (exch));
	return(tmp);
}

static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
	gint32 tmp;

	__asm__ __volatile__ ("\nL_ie_loop:\n\t"
			      "lwarx  %0, 0, %1\n\t"
			      "stwcx. %2, 0, %1\n\t"
			      "bne    L_ie_loop"
			      : "=r" (tmp) : "r" (dest), "r" (exch));
	return(tmp);
}
#define InterlockedExchangePointer InterlockedExchange

static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
	gint32 tmp;

	__asm__ __volatile__ ("\nL_iea_loop:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "add    %1, %3, %4\n\t"
			      "stwcx. %1, 0, %2\n\t"
			      "bne    L_iea_loop"
			      : "=r" (tmp), "=r" (add)
			      : "r" (dest), "0" (tmp), "1" (add));
	return(tmp);
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
