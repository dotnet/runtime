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

#ifdef _MSC_VER
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
#elif defined(USE_GCC_ATOMIC_OPS)
static inline void mono_memory_barrier (void)
{
	__sync_synchronize ();
}

static inline void mono_memory_read_barrier (void)
{
	mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
	mono_memory_barrier ();
}
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
