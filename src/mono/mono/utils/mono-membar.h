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
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#include <intrin.h>

static inline void mono_memory_barrier (void)
{
	/* NOTE: _ReadWriteBarrier and friends only prevent the
	   compiler from reordering loads and stores. To prevent
	   the CPU from doing the same, we have to use the
	   MemoryBarrier macro which expands to e.g. a serializing
	   XCHG instruction on x86. Also note that the MemoryBarrier
	   macro does *not* imply _ReadWriteBarrier, so that call
	   cannot be eliminated. */
	_ReadWriteBarrier ();
	MemoryBarrier ();
}

static inline void mono_memory_read_barrier (void)
{
	_ReadBarrier ();
	MemoryBarrier ();
}

static inline void mono_memory_write_barrier (void)
{
	_WriteBarrier ();
	MemoryBarrier ();
}
#elif defined(__WIN32__) || defined(_WIN32)
#include <windows.h>

/* Since we only support GCC 3.x in Cygwin for
   some arcane reason, we have to use inline
   assembly to get fences (__sync_synchronize
   is not available). */

static inline void mono_memory_barrier (void)
{
	__asm__ __volatile__ (
		"lock\n\t"
		"addl\t$0,0(%%esp)\n\t"
		:
		:
		: "memory"
	);
}

static inline void mono_memory_read_barrier (void)
{
	mono_memory_barrier ();
}

static inline void mono_memory_write_barrier (void)
{
	mono_memory_barrier ();
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
#else
#error "Don't know how to do memory barriers!"
#endif

#endif	/* _MONO_UTILS_MONO_MEMBAR_H_ */
