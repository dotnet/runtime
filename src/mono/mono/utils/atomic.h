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

#include <glib.h>

#if defined(__WIN32__) || defined(_WIN32)

#include <windows.h>

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

#elif defined(__i386__) || defined(__x86_64__)

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
#if defined(__x86_64__)  && !defined(__native_client__)
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
 * http://msdn.microsoft.com/msdnmag/issues/0700/Win32/
 * for the reasons for using cmpxchg and a loop here.
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
#if defined(__x86_64__)  && !defined(__native_client__)
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

#elif defined(__mono_ppc__)

#ifdef G_COMPILER_CODEWARRIOR
static inline gint32 InterlockedIncrement(volatile register gint32 *val)
{
	gint32 result = 0, tmp;
	register gint32 result = 0;
	register gint32 tmp;

	asm
	{
		@1:
			lwarx	tmp, 0, val
			addi	result, tmp, 1
			stwcx.	result, 0, val
			bne-	@1
	}
 
	return result;
}

static inline gint32 InterlockedDecrement(register volatile gint32 *val)
{
	register gint32 result = 0;
	register gint32 tmp;

	asm
	{
		@1:
			lwarx	tmp, 0, val
			addi	result, tmp, -1
			stwcx.	result, 0, val
			bne-	@1
	}

	return result;
}
#define InterlockedCompareExchangePointer(dest,exch,comp) (void*)InterlockedCompareExchange((volatile gint32 *)(dest), (gint32)(exch), (gint32)(comp))

static inline gint32 InterlockedCompareExchange(volatile register gint32 *dest, register gint32 exch, register gint32 comp)
{
	register gint32 tmp = 0;

	asm
	{
		@1:
			lwarx	tmp, 0, dest
			cmpw	tmp, comp
			bne-	@2
			stwcx.	exch, 0, dest
			bne-	@1
		@2:
	}

	return tmp;
}
static inline gint32 InterlockedExchange(register volatile gint32 *dest, register gint32 exch)
{
	register gint32 tmp = 0;

	asm
	{
		@1:
			lwarx	tmp, 0, dest
			stwcx.	exch, 0, dest
			bne-	@1
	}

	return tmp;
}
#define InterlockedExchangePointer(dest,exch) (void*)InterlockedExchange((volatile gint32 *)(dest), (gint32)(exch))
#else

#if defined(__mono_ppc64__) && !defined(__mono_ilp32__)
#define LDREGX "ldarx"
#define STREGCXD "stdcx."
#define CMPREG "cmpd"
#else
#define LDREGX "lwarx"
#define STREGCXD "stwcx."
#define CMPREG "cmpw"
#endif

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	gint32 result = 0, tmp;

	__asm__ __volatile__ ("\n1:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "addi   %1, %0, 1\n\t"
                              "stwcx. %1, 0, %2\n\t"
			      "bne-   1b"
			      : "=&b" (result), "=&b" (tmp): "r" (val): "cc", "memory");
	return result + 1;
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	gint32 result = 0, tmp;

	__asm__ __volatile__ ("\n1:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "addi   %1, %0, -1\n\t"
                              "stwcx. %1, 0, %2\n\t"
			      "bne-   1b"
			      : "=&b" (result), "=&b" (tmp): "r" (val): "cc", "memory");
	return result - 1;
}

static inline gpointer InterlockedCompareExchangePointer (volatile gpointer *dest,
						gpointer exch, gpointer comp)
{
	gpointer tmp = NULL;

	__asm__ __volatile__ ("\n1:\n\t"
			     LDREGX " %0, 0, %1\n\t"
			     CMPREG " %0, %2\n\t" 
			     "bne-    2f\n\t"
			     STREGCXD " %3, 0, %1\n\t"
			     "bne-    1b\n"
			     "2:"
			     : "=&r" (tmp)
			     : "b" (dest), "r" (comp), "r" (exch): "cc", "memory");
	return(tmp);
}

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp) {
	gint32 tmp = 0;

	__asm__ __volatile__ ("\n1:\n\t"
			     "lwarx   %0, 0, %1\n\t"
			     "cmpw    %0, %2\n\t" 
			     "bne-    2f\n\t"
			     "stwcx.  %3, 0, %1\n\t"
			     "bne-    1b\n"
			     "2:"
			     : "=&r" (tmp)
			     : "b" (dest), "r" (comp), "r" (exch): "cc", "memory");
	return(tmp);
}

static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
	gint32 tmp = 0;

	__asm__ __volatile__ ("\n1:\n\t"
			      "lwarx  %0, 0, %2\n\t"
			      "stwcx. %3, 0, %2\n\t"
			      "bne    1b"
			      : "=r" (tmp) : "0" (tmp), "b" (dest), "r" (exch): "cc", "memory");
	return(tmp);
}

static inline gpointer InterlockedExchangePointer (volatile gpointer *dest, gpointer exch)
{
	gpointer tmp = NULL;

	__asm__ __volatile__ ("\n1:\n\t"
			      LDREGX " %0, 0, %2\n\t"
			      STREGCXD " %3, 0, %2\n\t"
			      "bne    1b"
			      : "=r" (tmp) : "0" (tmp), "b" (dest), "r" (exch): "cc", "memory");
	return(tmp);
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
        gint32 result, tmp;
        __asm__ __volatile__ ("\n1:\n\t"
                              "lwarx  %0, 0, %2\n\t"
                              "add    %1, %0, %3\n\t"
                              "stwcx. %1, 0, %2\n\t"
                              "bne    1b"
                              : "=&r" (result), "=&r" (tmp)
                              : "r" (dest), "r" (add) : "cc", "memory");
        return(result);
}

#undef LDREGX
#undef STREGCXD
#undef CMPREG

#endif /* !G_COMPILER_CODEWARRIOR */

#elif defined(__arm__)

#ifdef __native_client__
#define MASK_REGISTER(reg, cond) "bic" cond " " reg ", " reg ", #0xc0000000\n"
#define NACL_ALIGN() ".align 4\n"
#else
#define MASK_REGISTER(reg, cond)
#define NACL_ALIGN()
#endif

/*
 * Atomic operations on ARM doesn't contain memory barriers, and the runtime code
 * depends on this, so we add them explicitly.
 */

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp)
{
#if defined(__ARM_ARCH_6__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7S__)
	gint32 ret, tmp;
	__asm__ __volatile__ (	"1:\n"
				NACL_ALIGN()
				"dmb\n"
				"mov	%0, #0\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "al")
				"ldrex %1, [%2]\n"
				"teq	%1, %3\n"
				"it eq\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "eq")
				"strexeq %0, %4, [%2]\n"
				"teq %0, #0\n"
				"bne 1b\n"
				"dmb\n"
				: "=&r" (tmp), "=&r" (ret)
				: "r" (dest), "r" (comp), "r" (exch)
				: "memory", "cc");

	return ret;
#else
	gint32 a, b;

	__asm__ __volatile__ (    "0:\n\t"
				  NACL_ALIGN()
				  MASK_REGISTER("%2", "al")
				  "ldr %1, [%2]\n\t"
				  "cmp %1, %4\n\t"
				  "mov %0, %1\n\t"
				  "bne 1f\n\t"
				  NACL_ALIGN()
				  MASK_REGISTER("%2", "al")
				  "swp %0, %3, [%2]\n\t"
				  "cmp %0, %1\n\t"
				  NACL_ALIGN()
				  MASK_REGISTER("%2", "ne")
				  "swpne %3, %0, [%2]\n\t"
				  "bne 0b\n\t"
				  "1:"
				  : "=&r" (a), "=&r" (b)
				  : "r" (dest), "r" (exch), "r" (comp)
				  : "cc", "memory");

	return a;
#endif
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
#if defined(__ARM_ARCH_6__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7S__)
	gpointer ret, tmp;
	__asm__ __volatile__ (
				"dmb\n"
				"1:\n"
				NACL_ALIGN()
				"mov	%0, #0\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "al")
				"ldrex %1, [%2]\n"
				"teq	%1, %3\n"
				"it eq\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "eq")
				"strexeq %0, %4, [%2]\n"
				"teq %0, #0\n"
				"bne 1b\n"
				"dmb\n"
				: "=&r" (tmp), "=&r" (ret)
				: "r" (dest), "r" (comp), "r" (exch)
				: "memory", "cc");

	return ret;
#else
	gpointer a, b;

	__asm__ __volatile__ (    "0:\n\t"
				  NACL_ALIGN()
				  MASK_REGISTER("%2", "al")
				  "ldr %1, [%2]\n\t"
				  "cmp %1, %4\n\t"
				  "mov %0, %1\n\t"
				  "bne 1f\n\t"
				  NACL_ALIGN()
				  MASK_REGISTER("%2", "eq")
				  "swpeq %0, %3, [%2]\n\t"
				  "cmp %0, %1\n\t"
				  NACL_ALIGN()
				  MASK_REGISTER("%2", "ne")
				  "swpne %3, %0, [%2]\n\t"
				  "bne 0b\n\t"
				  "1:"
				  : "=&r" (a), "=&r" (b)
				  : "r" (dest), "r" (exch), "r" (comp)
				  : "cc", "memory");

	return a;
#endif
}

static inline gint32 InterlockedIncrement(volatile gint32 *dest)
{
#if defined(__ARM_ARCH_6__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7S__)
	gint32 ret, flag;
	__asm__ __volatile__ (
				"dmb\n"
				"1:\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "al")
				"ldrex %0, [%2]\n"
				"add %0, %0, %3\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "al")
				"strex %1, %0, [%2]\n"
				"teq %1, #0\n"
				"bne 1b\n"
				"dmb\n"
				: "=&r" (ret), "=&r" (flag)
				: "r" (dest), "r" (1)
				: "memory", "cc");

	return ret;
#else
	gint32 a, b, c;

	__asm__ __volatile__ (  "0:\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"ldr %0, [%3]\n\t"
				"add %1, %0, %4\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"swp %2, %1, [%3]\n\t"
				"cmp %0, %2\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "ne")
				"swpne %1, %2, [%3]\n\t"
				"bne 0b"
				: "=&r" (a), "=&r" (b), "=&r" (c)
				: "r" (dest), "r" (1)
				: "cc", "memory");

	return b;
#endif
}

static inline gint32 InterlockedDecrement(volatile gint32 *dest)
{
#if defined(__ARM_ARCH_6__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7S__)
	gint32 ret, flag;
	__asm__ __volatile__ (
				"dmb\n"
				"1:\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "al")
				"ldrex %0, [%2]\n"
				"sub %0, %0, %3\n"
				NACL_ALIGN()
				MASK_REGISTER("%2", "al")
				"strex %1, %0, [%2]\n"
				"teq %1, #0\n"
				"bne 1b\n"
				"dmb\n"
				: "=&r" (ret), "=&r" (flag)
				: "r" (dest), "r" (1)
				: "memory", "cc");

	return ret;
#else
	gint32 a, b, c;

	__asm__ __volatile__ (  "0:\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"ldr %0, [%3]\n\t"
				"add %1, %0, %4\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"swp %2, %1, [%3]\n\t"
				"cmp %0, %2\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "ne")
				"swpne %1, %2, [%3]\n\t"
				"bne 0b"
				: "=&r" (a), "=&r" (b), "=&r" (c)
				: "r" (dest), "r" (-1)
				: "cc", "memory");

	return b;
#endif
}

static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
#if defined(__ARM_ARCH_6__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7S__)
	gint32 ret, flag;
	__asm__ __volatile__ (
				  "dmb\n"
			      "1:\n"
			      NACL_ALIGN()
			      MASK_REGISTER("%3", "al")
			      "ldrex %0, [%3]\n"
			      NACL_ALIGN()
			      MASK_REGISTER("%3", "al")
			      "strex %1, %2, [%3]\n"
			      "teq %1, #0\n"
			      "bne 1b\n"
				  "dmb\n"
			      : "=&r" (ret), "=&r" (flag)
			      : "r" (exch), "r" (dest)
			      : "memory", "cc");
	return ret;
#else
	gint32 a;

	__asm__ __volatile__ (  NACL_ALIGN()
				MASK_REGISTER("%1", "al")
                                "swp %0, %2, [%1]"
				: "=&r" (a)
				: "r" (dest), "r" (exch));

	return a;
#endif
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch)
{
#if defined(__ARM_ARCH_6__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7S__)
	gpointer ret, flag;
	__asm__ __volatile__ (
				  "dmb\n"
			      "1:\n"
			      NACL_ALIGN()
			      MASK_REGISTER("%3", "al")
			      "ldrex %0, [%3]\n"
			      NACL_ALIGN()
			      MASK_REGISTER("%3", "al")
			      "strex %1, %2, [%3]\n"
			      "teq %1, #0\n"
			      "bne 1b\n"
				  "dmb\n"
			      : "=&r" (ret), "=&r" (flag)
			      : "r" (exch), "r" (dest)
			      : "memory", "cc");
	return ret;
#else
	gpointer a;

	__asm__ __volatile__ (	NACL_ALIGN()
				MASK_REGISTER("%1", "al")
                                "swp %0, %2, [%1]"
				: "=&r" (a)
				: "r" (dest), "r" (exch));

	return a;
#endif
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
#if defined(__ARM_ARCH_6__) || defined(__ARM_ARCH_7A__) || defined(__ARM_ARCH_7__) || defined(__ARM_ARCH_7S__)
	gint32 ret, tmp, flag;
	__asm__ __volatile__ (
				"dmb\n"
				"1:\n"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"ldrex %0, [%3]\n"
				"add %1, %0, %4\n"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"strex %2, %1, [%3]\n"
				"teq %2, #0\n"
				"bne 1b\n"
				"dmb\n"
				: "=&r" (ret), "=&r" (tmp), "=&r" (flag)
				: "r" (dest), "r" (add)
				: "memory", "cc");

	return ret;
#else
	int a, b, c;

	__asm__ __volatile__ (  "0:\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"ldr %0, [%3]\n\t"
				"add %1, %0, %4\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "al")
				"swp %2, %1, [%3]\n\t"
				"cmp %0, %2\n\t"
				NACL_ALIGN()
				MASK_REGISTER("%3", "ne")
				"swpne %1, %2, [%3]\n\t"
				"bne 0b"
				: "=&r" (a), "=&r" (b), "=&r" (c)
				: "r" (dest), "r" (add)
				: "cc", "memory");

	return a;
#endif
}

#undef NACL_ALIGN
#undef MASK_REGISTER

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

#elif defined(__mips__)

#if SIZEOF_REGISTER == 8
#error "Not implemented."
#endif

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	gint32 tmp, result = 0;

	__asm__ __volatile__ ("    .set    mips32\n"
			      "1:  ll      %0, %2\n"
			      "    addu    %1, %0, 1\n"
                              "    sc      %1, %2\n"
			      "    beqz    %1, 1b\n"
			      "    .set    mips0\n"
			      : "=&r" (result), "=&r" (tmp), "=m" (*val)
			      : "m" (*val));
	return result + 1;
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	gint32 tmp, result = 0;

	__asm__ __volatile__ ("    .set    mips32\n"
			      "1:  ll      %0, %2\n"
			      "    subu    %1, %0, 1\n"
                              "    sc      %1, %2\n"
			      "    beqz    %1, 1b\n"
			      "    .set    mips0\n"
			      : "=&r" (result), "=&r" (tmp), "=m" (*val)
			      : "m" (*val));
	return result - 1;
}

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp) {
	gint32 old, tmp;

	__asm__ __volatile__ ("    .set    mips32\n"
			      "1:  ll      %0, %2\n"
			      "    bne     %0, %5, 2f\n"
			      "    move    %1, %4\n"
                              "    sc      %1, %2\n"
			      "    beqz    %1, 1b\n"
			      "2:  .set    mips0\n"
			      : "=&r" (old), "=&r" (tmp), "=m" (*dest)
			      : "m" (*dest), "r" (exch), "r" (comp));
	return(old);
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
	return (gpointer)(InterlockedCompareExchange((volatile gint32 *)(dest), (gint32)(exch), (gint32)(comp)));
}

static inline gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch)
{
	gint32 result, tmp;

	__asm__ __volatile__ ("    .set    mips32\n"
			      "1:  ll      %0, %2\n"
			      "    move    %1, %4\n"
                              "    sc      %1, %2\n"
			      "    beqz    %1, 1b\n"
			      "    .set    mips0\n"
			      : "=&r" (result), "=&r" (tmp), "=m" (*dest)
			      : "m" (*dest), "r" (exch));
	return(result);
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch)
{
	return (gpointer)InterlockedExchange((volatile gint32 *)(dest), (gint32)(exch));
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add)
{
        gint32 result, tmp;

	__asm__ __volatile__ ("    .set    mips32\n"
			      "1:  ll      %0, %2\n"
			      "    addu    %1, %0, %4\n"
                              "    sc      %1, %2\n"
			      "    beqz    %1, 1b\n"
			      "    .set    mips0\n"
			      : "=&r" (result), "=&r" (tmp), "=m" (*dest)
			      : "m" (*dest), "r" (add));
        return result;
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

/* Not yet used */
#ifdef USE_GCC_ATOMIC_OPS

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
#endif

#endif /* _WAPI_ATOMIC_H_ */
