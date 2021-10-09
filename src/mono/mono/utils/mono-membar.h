/**
 * \file
 * Memory barrier inline functions
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

/*
 * Memory barrier which only affects the compiler.
 * mono_memory_barrier_process_wide () should be uses to synchronize with code which uses this.
 */
//#define mono_compiler_barrier() asm volatile("": : :"memory")

#ifdef HOST_WASM

static inline void mono_memory_barrier (void)
{
}

static inline void mono_memory_read_barrier (void)
{
}

static inline void mono_memory_write_barrier (void)
{
}

#define mono_compiler_barrier() asm volatile("": : :"memory")

#elif _MSC_VER
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

#define mono_compiler_barrier() _ReadWriteBarrier ()

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

#define mono_compiler_barrier() asm volatile("": : :"memory")

#else
#error "Don't know how to do memory barriers!"
#endif

void mono_memory_barrier_process_wide (void);

#endif	/* _MONO_UTILS_MONO_MEMBAR_H_ */
