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

#if defined(__i386__) || defined(__x86_64__)
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

	__asm__ __volatile__ ("lock; "
#ifdef __x86_64__
			      "cmpxchgq"
#else
			      "cmpxchgl"
#endif
			      " %2, %0"
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
 *
 * For the time being, http://msdn.microsoft.com/msdnmag/issues/0700/Win32/
 * might work.  Bet it will change soon enough though.
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
	
	__asm__ __volatile__ ("1:; lock; "
#ifdef __x86_64__
			      "cmpxchgq"
#else
			      "cmpxchgl"
#endif
			      " %2, %0; jne 1b"
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

#ifdef __GNUC__
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
#else
static inline void begin_spin(volatile unsigned char *lock)
{
	asm("1: ldstub [%i0], %l0");
	asm("cmp %l0,0");
	asm("bne 1b");
	asm("nop");
}
#define BEGIN_SPIN(tmp,lock) begin_spin(&lock);
#define END_SPIN(lock) ((lock) = 0);
#endif

extern volatile unsigned char _wapi_sparc_lock;

G_GNUC_UNUSED 
static inline gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp)
{
	int tmp;
	gint32 old;

	BEGIN_SPIN(tmp,_wapi_sparc_lock)

	old = *dest;
	if (old==comp) {
		*dest=exch;
	}

	END_SPIN(_wapi_sparc_lock)

	return(old);
}

G_GNUC_UNUSED 
static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
        int tmp;
        gpointer old;

        BEGIN_SPIN(tmp,_wapi_sparc_lock)

        old = *dest;
        if (old==comp) {
                *dest=exch;
        }

        END_SPIN(_wapi_sparc_lock)

        return(old);
}

G_GNUC_UNUSED 
static inline gint32 InterlockedIncrement(volatile gint32 *dest)
{
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,_wapi_sparc_lock)

        (*dest)++;
        ret = *dest;

        END_SPIN(_wapi_sparc_lock)

        return(ret);
}

G_GNUC_UNUSED 
static inline gint32 InterlockedDecrement(volatile gint32 *dest)
{
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,_wapi_sparc_lock)

	(*dest)--;
        ret = *dest;

        END_SPIN(_wapi_sparc_lock)

        return(ret);
}

G_GNUC_UNUSED
static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,_wapi_sparc_lock)

        ret = *dest;
        *dest = exch;

        END_SPIN(_wapi_sparc_lock)

        return(ret);
}

G_GNUC_UNUSED
static inline gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch)
{
        int tmp;
        gpointer ret;

        BEGIN_SPIN(tmp,_wapi_sparc_lock)

        ret = *dest;
        *dest = exch;

        END_SPIN(_wapi_sparc_lock)

        return(ret);
}

G_GNUC_UNUSED
static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
        int tmp;
        gint32 ret;

        BEGIN_SPIN(tmp,_wapi_sparc_lock)

        ret = *dest;
        *dest += add;

        END_SPIN(_wapi_sparc_lock)

        return(ret);
}

#elif __s390__

#define WAPI_ATOMIC_ASM

static inline gint32 
InterlockedCompareExchange(volatile gint32 *dest,
			   gint32 exch, gint32 comp)
{
	gint32 old;

	__asm__ __volatile__ ("\tLA\t1,%0\n"
			      "0:\tL\t%1,%0\n"
			      "\tCR\t%1,%3\n"
			      "\tJNE\t1f\n"
			      "\tCS\t%1,%2,0(1)\n"
			      "\tJNZ\t0b\n"
			      "1:\n"
			      : "+m" (*dest), "+r" (old)
			      : "r" (exch), "r" (comp)
			      : "1", "cc");	
	return(old);
}

#ifndef __s390x__
#  define InterlockedCompareExchangePointer InterlockedCompareExchange
# else
static inline gpointer 
InterlockedCompareExchangePointer(volatile gpointer *dest, 
				  gpointer exch, 
			          gpointer comp)
{
	gpointer old;

	__asm__ __volatile__ ("\tLA\t1,%0\n"
			      "0:\tLG\t%1,%0\n"
			      "\tCGR\t%1,%3\n"
			      "\tJNE\t1f\n"
			      "\tCSG\t%1,%2,0(1)\n"
			      "\tJNZ\t0b\n"
			      "1:\n"
			      : "+m" (*dest), "+r" (old)
			      : "r" (exch), "r" (comp)
			      : "1", "cc");

	return(comp);
}
# endif


static inline gint32 
InterlockedIncrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("\tLA\t2,%1\n"
			      "\tL\t%0,%1\n"
			      "\tLR\t1,%0\n"
			      "\tAHI\t1,1\n"
			      "0:\tCS\t%0,1,0(2)\n"
			      "\tJNZ\t0b"
			      : "=r" (tmp), "+m" (*val)
			      : : "1", "2", "cc");

	return(tmp+1);
}

static inline gint32 
InterlockedDecrement(volatile gint32 *val)
{
	gint32 tmp;
	
	__asm__ __volatile__ ("\tLA\t2,%1\n"
			      "\tL\t%0,%1\n"
			      "\tLR\t1,%0\n"
			      "\tAHI\t1,-1\n"
			      "0:\tCS\t%0,1,0(2)\n"
			      "\tJNZ\t0b"
			      : "=r" (tmp), "+m" (*val)
			      : : "1", "2", "cc");

	return(tmp-1);
}


static inline gint32 
InterlockedExchange(volatile gint32 *val, gint32 new_val)
{
	gint32 ret;
	
	__asm__ __volatile__ ("\tLA\t1,%1\n"
			      "0:\tL\t%1,%0\n"
			      "\tCS\t%1,%2,0(1)\n"
			      "\tJNZ\t0b"
			      : "+m" (*val), "+r" (ret)
			      : "r" (new_val)
			      : "1", "cc");

	return(ret);
}

# ifndef __s390x__
#  define InterlockedExchangePointer InterlockedExchange
# else
static inline gpointer
InterlockedExchangePointer(volatile gpointer *val, gpointer new_val)
{
	gpointer ret;
	
	__asm__ __volatile__ ("\tLA\t1,%1\n"
			      "0:\tLG\t%1,%0\n"
			      "\tCSG\t%1,%2,0(1)\n"
			      "\tJNZ\t0b"
			      : "+m" (*val), "+r" (ret)
			      : "r" (new_val)
			      : "1", "cc");

	return(ret);
}
# endif

static inline gint32 
InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
	gint32 ret;

	__asm__ __volatile__ ("\tL\t%0,%1\n"
			      "\tLR\t1,%0\n"
			      "\tAR\t1,%2\n"
			      "\tLA\t2,%1\n"
			      "0:\tCS\t%0,1,0(2)\n"
			      "\tJNZ\t0b"
			      : "=r" (ret), "+m" (*val)
			      : "r" (add) 
			      : "1", "2", "cc");
	
	return(ret);
}

#elif defined(__ppc__) || defined (__powerpc__)
#define WAPI_ATOMIC_ASM

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	gint32 tmp;

	__asm__ __volatile__ ("\n1:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "addi   %1, %0, 1\n\t"
                              "stwcx. %1, 0, %2\n\t"
			      "bne-   1b"
			      : "=&b" (tmp): "r" (tmp), "r" (val): "cc", "memory");
	return tmp;
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	gint32 tmp;

	__asm__ __volatile__ ("\n1:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "addi   %1, %0, -1\n\t"
                              "stwcx. %1, 0, %2\n\t"
			      "bne-   1b"
			      : "=&b" (tmp) : "r" (tmp), "r" (val): "cc", "memory");
	return(tmp);
}

#define InterlockedCompareExchangePointer InterlockedCompareExchange

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp) {
	gint32 tmp = 0;

	__asm__ __volatile__ ("\n1:\n\t"
			     "lwarx   %0, 0, %1\n\t"
			     "cmpw    %2, %3\n\t" 
			     "bne-    2f\n\t"
			     "stwcx.  %4, 0, %1\n\t"
			     "bne-    1b\n"
			     "2:"
			     : "=r" (tmp)
			     : "r" (dest), "0" (tmp) ,"r" (comp), "r" (exch): "cc", "memory");
	return(tmp);
}

static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
	gint32 tmp;

	__asm__ __volatile__ ("\n1:\n\t"
			      "lwarx  %0, 0, %1\n\t"
			      "stwcx. %2, 0, %1\n\t"
			      "bne    1b"
			      : "=r" (tmp) : "r" (dest), "r" (exch): "cc", "memory");
	return(tmp);
}
#define InterlockedExchangePointer InterlockedExchange

static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
	gint32 tmp;

	__asm__ __volatile__ ("\n1:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "add    %1, %3, %4\n\t"
			      "stwcx. %1, 0, %2\n\t"
			      "bne    1b"
			      : "=r" (tmp), "=r" (add)
			      : "r" (dest), "0" (tmp), "1" (add) : "cc", "memory");
	return(tmp);
}

#elif defined(__arm__)
#define WAPI_ATOMIC_ASM

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp)
{
	int a, b;

	__asm__ __volatile__ (    "0:\n\t"
				  "ldr %1, [%2]\n\t"
				  "cmp %1, %4\n\t"
				  "bne 1f\n\t"
				  "swp %0, %3, [%2]\n\t"
				  "cmp %0, %1\n\t"
				  "swpne %3, %0, [%2]\n\t"
				  "bne 0b\n\t"
				  "1:"
				  : "=&r" (a), "=&r" (b)
				  : "r" (dest), "r" (exch), "r" (comp)
				  : "cc", "memory");

	return a;
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
	gpointer a, b;

	__asm__ __volatile__ (    "0:\n\t"
				  "ldr %1, [%2]\n\t"
				  "cmp %1, %4\n\t"
				  "bne 1f\n\t"
				  "swpeq %0, %3, [%2]\n\t"
				  "cmp %0, %1\n\t"
				  "swpne %3, %0, [%2]\n\t"
				  "bne 0b\n\t"
				  "1:"
				  : "=&r" (a), "=&r" (b)
				  : "r" (dest), "r" (exch), "r" (comp)
				  : "cc", "memory");

	return a;
}

static inline gint32 InterlockedIncrement(volatile gint32 *dest)
{
	int a, b, c;

	__asm__ __volatile__ (  "0:\n\t"
				"ldr %0, [%3]\n\t"
				"add %1, %0, %4\n\t"
				"swp %2, %1, [%3]\n\t"
				"cmp %0, %2\n\t"
				"swpne %1, %2, [%3]\n\t"
				"bne 0b"
				: "=&r" (a), "=&r" (b), "=&r" (c)
				: "r" (dest), "r" (1)
				: "cc", "memory");

	return b;
}

static inline gint32 InterlockedDecrement(volatile gint32 *dest)
{
	int a, b, c;

	__asm__ __volatile__ (  "0:\n\t"
				"ldr %0, [%3]\n\t"
				"add %1, %0, %4\n\t"
				"swp %2, %1, [%3]\n\t"
				"cmp %0, %2\n\t"
				"swpne %1, %2, [%3]\n\t"
				"bne 0b"
				: "=&r" (a), "=&r" (b), "=&r" (c)
				: "r" (dest), "r" (-1)
				: "cc", "memory");

	return b;
}

static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
	int a;

	__asm__ __volatile__ (  "swp %0, %2, [%1]"
				: "=&r" (a)
				: "r" (dest), "r" (exch));

	return a;
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch)
{
	gpointer a;

	__asm__ __volatile__ (	"swp %0, %2, [%1]"
				: "=&r" (a)
				: "r" (dest), "r" (exch));

	return a;
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
	int a, b, c;

	__asm__ __volatile__ (  "0:\n\t"
				"ldr %0, [%3]\n\t"
				"add %1, %0, %4\n\t"
				"swp %2, %1, [%3]\n\t"
				"cmp %0, %2\n\t"
				"swpne %1, %2, [%3]\n\t"
				"bne 0b"
				: "=&r" (a), "=&r" (b), "=&r" (c)
				: "r" (dest), "r" (add)
				: "cc", "memory");

	return a;
}

#else

extern gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp);
extern gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp);
extern gint32 InterlockedIncrement(volatile gint32 *dest);
extern gint32 InterlockedDecrement(volatile gint32 *dest);
extern gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch);
extern gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch);
extern gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add);

#if defined(__hpux) && !defined(__GNUC__)
#define WAPI_ATOMIC_ASM
#endif

#endif

#endif /* _WAPI_ATOMIC_H_ */
