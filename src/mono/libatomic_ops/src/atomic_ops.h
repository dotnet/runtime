/*
 * Copyright (c) 2003 Hewlett-Packard Development Company, L.P.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE. 
 */ 

#ifndef ATOMIC_OPS_H

#define ATOMIC_OPS_H

#include <assert.h>

/* We define various atomic operations on memory in a 		*/
/* machine-specific way.  Unfortunately, this is complicated 	*/
/* by the fact that these may or may not be combined with 	*/
/* various memory barriers.  Thus the actual operations we	*/
/* define have the form AO_<atomic-op>_<barrier>, for all	*/
/* plausible combinations of <atomic-op> and <barrier>.		*/
/* This of course results in a mild combinatorial explosion.	*/
/* To deal with it, we try to generate derived			*/
/* definitions for as many of the combinations as we can, as	*/
/* automatically as possible.					*/
/*								*/
/* Our assumption throughout is that the programmer will 	*/
/* specify the least demanding operation and memory barrier	*/
/* that will guarantee correctness for the implementation.	*/
/* Our job is to find the least expensive way to implement it	*/
/* on the applicable hardware.  In many cases that will 	*/
/* involve, for example, a stronger memory barrier, or a	*/
/* combination of hardware primitives.				*/
/*								*/
/* Conventions:							*/
/* "plain" atomic operations are not guaranteed to include	*/
/* a barrier.  The suffix in the name specifies the barrier	*/
/* type.  Suffixes are:						*/
/* _release: Earlier operations may not be delayed past it.	*/
/* _acquire: Later operations may not move ahead of it.		*/
/* _read: Subsequent reads must follow this operation and	*/
/* 	  preceding reads.					*/
/* _write: Earlier writes precede both this operation and 	*/
/* 	  later writes.						*/
/* _full: Ordered with respect to both earlier and later memops.*/
/* _release_write: Ordered with respect to earlier writes.	*/
/* _acquire_read: Ordered with repsect to later reads.		*/
/*								*/
/* Currently we try to define the following atomic memory 	*/
/* operations, in combination with the above barriers:		*/
/* AO_nop							*/
/* AO_load							*/
/* AO_store							*/
/* AO_test_and_set (binary)					*/
/* AO_fetch_and_add						*/
/* AO_fetch_and_add1						*/
/* AO_fetch_and_sub1						*/
/* AO_or							*/
/* AO_compare_and_swap						*/
/* 								*/
/* Note that atomicity guarantees are valid only if both 	*/
/* readers and writers use AO_ operations to access the 	*/
/* shared value, while ordering constraints are intended to	*/
/* apply all memory operations.	 If a location can potentially	*/
/* be accessed simultaneously from multiple threads, and one of	*/
/* those accesses may be a write access, then all such		*/
/* accesses to that location should be through AO_ primitives.	*/
/* However if AO_ operations enforce sufficient ordering to 	*/
/* ensure that a location x cannot be accessed concurrently,	*/
/* or can only be read concurrently, then x can be accessed	*/
/* via ordinary references and assignments.			*/
/*								*/
/* Compare_and_exchange takes an address and an expected old	*/
/* value and a new value, and returns an int.  Nonzero 		*/
/* indicates that it succeeded.					*/
/* Test_and_set takes an address, atomically replaces it by	*/
/* AO_TS_SET, and returns the prior value.			*/
/* An AO_TS_t clear location can be reset with the		*/
/* AO_CLEAR macro, which normally uses AO_store_release.	*/
/* AO_fetch_and_add takes an address and an AO_t increment 	*/
/* value.  The AO_fetch_and_add1 and AO_fetch_and_sub1 variants	*/
/* are provided, since they allow faster implementations on	*/
/* some hardware. AO_or atomically ors an AO_t value into a	*/
/* memory location, but does not provide access to the original.*/
/*								*/
/* We expect this list to grow slowly over time.		*/
/*								*/
/* Note that AO_nop_full is a full memory barrier.		*/
/*								*/
/* Note that if some data is initialized with			*/
/*	data.x = ...; data.y = ...; ...				*/
/*	AO_store_release_write(&data_is_initialized, 1)		*/
/* then data is guaranteed to be initialized after the test	*/
/* 	if (AO_load_release_read(&data_is_initialized)) ...	*/
/* succeeds.  Furthermore, this should generate near-optimal	*/
/* code on all common platforms.				*/
/*								*/
/* All operations operate on unsigned AO_t, which		*/
/* is the natural word size, and usually unsigned long.		*/
/* It is possible to check whether a particular operation op	*/
/* is available on a particular platform by checking whether	*/
/* AO_HAVE_op is defined.  We make heavy use of these macros 	*/
/* internally.							*/

/* The rest of this file basically has three sections:		*/
/*								*/
/* Some utility and default definitions.			*/
/*								*/
/* The architecture dependent section:				*/
/* This defines atomic operations that have direct hardware	*/
/* support on a particular platform, mostly by uncluding the	*/
/* appropriate compiler- and hardware-dependent file.  		*/
/*								*/
/* The synthesis section:					*/
/* This tries to define other atomic operations in terms of 	*/
/* those that are explicitly available on the platform.		*/
/* This section is hardware independent.			*/
/* We make no attempt to synthesize operations in ways that	*/
/* effectively introduce locks, except for the debugging/demo	*/
/* pthread-based implementation at the beginning.  A more 	*/
/* relistic implementation that falls back to locks could be	*/
/* added as a higher layer.  But that would sacrifice		*/
/* usability from signal handlers.				*/
/* The synthesis section is implemented almost entirely in	*/
/* atomic_ops_generalize.h.					*/

/* Some common defaults.  Overridden for some architectures.	*/
#define AO_t unsigned long
	/* Could conceivably be redefined below if/when we add	*/
	/* win64 support.					*/

/* The test_and_set primitive returns an AO_TS_VAL_t value.	*/
/* AO_TS_t is the type of an in-memory test-and-set location.	*/

#define AO_TS_INITIALIZER (AO_t)AO_TS_CLEAR

/* Platform-dependent stuff:					*/
#if defined(__GNUC__) || defined(_MSC_VER) || defined(__INTEL_COMPILER)
# define AO_INLINE static __inline
#else
# define AO_INLINE static
#endif

#if defined(__GNUC__) && !defined(__INTEL_COMPILER)
# define AO_compiler_barrier() __asm__ __volatile__("" : : : "memory")
#elif defined(_MSC_VER)
# define AO_compiler_barrier() __asm { }
#elif defined(__INTEL_COMPILER)
# define AO_compiler_barrier() __memory_barrier() /* Too strong? IA64-only? */
#elif defined(_HPUX_SOURCE)
# if defined(__ia64)
#   include <machine/sys/inline.h>
#   define AO_compiler_barrier() _Asm_sched_fence()
# else
    /* FIXME - We dont know how to do this.  This is a guess.	*/
    /* And probably a bad one.					*/
    static volatile int AO_barrier_dummy;
#   define AO_compiler_barrier() AO_barrier_dummy = AO_barrier_dummy
# endif
#else
  /* We conjecture that the following usually gives us the right 	*/
  /* semantics or an error.						*/
# define AO_compiler_barrier() asm("")
#endif

#if defined(AO_USE_PTHREAD_DEFS)
# include "atomic_ops/sysdeps/generic_pthread.h"
#endif /* AO_USE_PTHREAD_DEFS */

#if defined(__GNUC__) && !defined(AO_USE_PTHREAD_DEFS) \
    && !defined(__INTEL_COMPILER)
# if defined(__i386__)
#   include "atomic_ops/sysdeps/gcc/x86.h"
# endif /* __i386__ */
# if defined(__x86_64__)
#   include "atomic_ops/sysdeps/gcc/x86_64.h"
# endif /* __i386__ */
# if defined(__ia64__)
#   include "atomic_ops/sysdeps/gcc/ia64.h"
#   define AO_GENERALIZE_TWICE
# endif /* __ia64__ */
# if defined(__hppa__)
#   include "atomic_ops/sysdeps/gcc/hppa.h"
#   define AO_CAN_EMUL_CAS
# endif /* __hppa__ */
# if defined(__alpha__)
#   include "atomic_ops/sysdeps/gcc/alpha.h"
#   define AO_GENERALIZE_TWICE
# endif /* __alpha__ */
# if defined(__s390__)
#   include "atomic_ops/sysdeps/gcc/s390.h"
# endif /* __s390__ */
# if defined(__sparc__)
#   include "atomic_ops/sysdeps/gcc/sparc.h"
#   define AO_CAN_EMUL_CAS
# endif /* __sparc__ */
# if defined(__m68k__)
#   include "atomic_ops/sysdeps/gcc/m68k.h"
# endif /* __m68k__ */
# if defined(__powerpc__) || defined(__ppc__)
#   include "atomic_ops/sysdeps/gcc/powerpc.h"
# endif /* __powerpc__ */
# if defined(__arm__) && !defined(AO_USE_PTHREAD_DEFS)
#   include "atomic_ops/sysdeps/gcc/arm.h"
# endif /* __arm__ */
# if defined(__cris__) || defined(CRIS)
#   include "atomic_ops/sysdeps/gcc/cris.h"
# endif
#endif /* __GNUC__ && !AO_USE_PTHREAD_DEFS */

#if defined(__INTEL_COMPILER) && !defined(AO_USE_PTHREAD_DEFS)
# if defined(__ia64__)
#   include "atomic_ops/sysdeps/icc/ia64.h"
#   define AO_GENERALIZE_TWICE
# endif
#endif

#if defined(_HPUX_SOURCE) && !defined(__GNUC__) && !defined(AO_USE_PTHREAD_DEFS)
# if defined(__ia64)
#   include "atomic_ops/sysdeps/hpc/ia64.h"
#   define AO_GENERALIZE_TWICE
# else
#   include "atomic_ops/sysdeps/hpc/hppa.h"
#   define AO_CAN_EMUL_CAS
# endif
#endif

#if !defined(__GNUC__) && (defined(sparc) || defined(__sparc)) \
    && !defined(AO_USE_PTHREAD_DEFS)
#   include "atomic_ops/sysdeps/sunc/sparc.h"
#   define AO_CAN_EMUL_CAS
#endif

#if defined(_MSC_VER)
# if _M_IX86 >= 400
#   include "atomic_ops/sysdeps/msftc/x86.h"
# endif
#endif

#if defined(AO_REQUIRE_CAS) && !defined(AO_HAVE_compare_and_swap) \
    && !defined(AO_HAVE_compare_and_swap_full) \
    && !defined(AO_HAVE_compare_and_swap_acquire)
# if defined(AO_CAN_EMUL_CAS)
#   include "atomic_ops/sysdeps/emul_cas.h"
# else
#  error Cannot implement AO_compare_and_swap_full on this architecture.
# endif
#endif 	/* AO_REQUIRE_CAS && !AO_HAVE_compare_and_swap ... */

/* The most common way to clear a test-and-set location		*/
/* at the end of a critical section.				*/
#if AO_AO_TS_T && !defined(AO_CLEAR)
# define AO_CLEAR(addr) AO_store_release((AO_TS_t *)addr, AO_TS_CLEAR)
#endif
#if AO_CHAR_TS_T && !defined(AO_CLEAR)
# define AO_CLEAR(addr) AO_char_store_release((AO_TS_t *)addr, AO_TS_CLEAR)
#endif

/*
 * The generalization section.
 * Theoretically this should repeatedly include atomic_ops_generalize.h.
 * In fact, we observe that this converges after a small fixed number
 * of iterations, usually one.
 */
#include "atomic_ops/generalize.h"
#ifdef AO_GENERALIZE_TWICE
# include "atomic_ops/generalize.h"
#endif

/* For compatibility with version 0.4 and earlier	*/
#define AO_TS_T AO_TS_t
#define AO_T AO_t
#define AO_TS_VAL AO_TS_VAL_t

#endif /* ATOMIC_OPS_H */
