/*
 * mono-membar.h: Memory barrier inline functions
 *
 * Author:
 *	Mark Probst (mark.probst@gmail.com)
 *
 * (C) 2007 Novell, Inc
 */

#ifndef _MONO_UTILS_MONO_MEMBAR_H_
#define _MONO_UTILS_MONO_MEMBAR_H_

#include <config.h>

#include <glib.h>

#if defined(__x86_64__) || defined(TARGET_AMD64)
#ifndef _MSC_VER
static inline void mono_memory_barrier (void)
{
	__asm__ __volatile__ ("mfence" : : : "memory");
}

static inline void mono_memory_read_barrier (void)
{
	__asm__ __volatile__ ("lfence" : : : "memory");
}

static inline void mono_memory_write_barrier (void)
{
	__asm__ __volatile__ ("sfence" : : : "memory");
}
#else
#include <intrin.h>

static inline void mono_memory_barrier (void)
{
	_ReadWriteBarrier ();
}

static inline void mono_memory_read_barrier (void)
{
	_ReadBarrier ();
}

static inline void mono_memory_write_barrier (void)
{
	_WriteBarrier ();
}
#endif
#elif defined(__i386__) || defined(TARGET_X86)
#ifndef _MSC_VER
static inline void mono_memory_barrier (void)
{
	__asm__ __volatile__ ("lock; addl $0,0(%%esp)" : : : "memory");
}

static inline void mono_memory_read_barrier (void)
{
	mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
	mono_memory_barrier ();
}
#else
#include <intrin.h>

static inline void mono_memory_barrier (void)
{
	_ReadWriteBarrier ();
}

static inline void mono_memory_read_barrier (void)
{
	_ReadBarrier ();
}

static inline void mono_memory_write_barrier (void)
{
	_WriteBarrier ();
}
#endif
#elif defined(sparc) || defined(__sparc__)
static inline void mono_memory_barrier (void)
{
	__asm__ __volatile__ ("membar	#LoadLoad | #LoadStore | #StoreStore | #StoreLoad" : : : "memory");
}

static inline void mono_memory_read_barrier (void)
{
	__asm__ __volatile__ ("membar	#LoadLoad" : : : "memory");
}

static inline void mono_memory_write_barrier (void)
{
	__asm__ __volatile__ ("membar	#StoreStore" : : : "memory");
}
#elif defined(__s390__)
static inline void mono_memory_barrier (void)
{
	__asm__ __volatile__ ("bcr 15,0" : : : "memory");
}

static inline void mono_memory_read_barrier (void)
{
	mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
	mono_memory_barrier ();
}
#elif defined(__ppc__) || defined(__powerpc__) || defined(__ppc64__)
static inline void mono_memory_barrier (void)
{
	__asm__ __volatile__ ("sync" : : : "memory");
}

static inline void mono_memory_read_barrier (void)
{
	mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
	__asm__ __volatile__ ("eieio" : : : "memory");
}

#elif defined(__arm__)
static inline void mono_memory_barrier (void)
{
#ifdef HAVE_ARMV6
#ifdef __native_client__
	/* NaCl requires ARMv7 CPUs. */
	__asm__ __volatile__("dsb" : : : "memory");
#else
	__asm__ __volatile__ ("mcr p15, 0, %0, c7, c10, 5" : : "r" (0) : "memory");
#endif
#endif
}

static inline void mono_memory_read_barrier (void)
{
	mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
	mono_memory_barrier ();
}
#elif defined(__ia64__)
static inline void mono_memory_barrier (void)
{
	__asm__ __volatile__ ("mf" : : : "memory");
}

static inline void mono_memory_read_barrier (void)
{
	mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
	mono_memory_barrier ();
}
#elif defined(__mips__)
static inline void mono_memory_barrier (void)
{
        __asm__ __volatile__ ("" : : : "memory");
}

static inline void mono_memory_read_barrier (void)
{
        mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
        mono_memory_barrier ();
}
#elif defined(MONO_CROSS_COMPILE)
static inline void mono_memory_barrier (void)
{
}

static inline void mono_memory_read_barrier (void)
{
}

static inline void mono_memory_write_barrier (void)
{
}
#endif

#endif	/* _MONO_UTILS_MONO_MEMBAR_H_ */
