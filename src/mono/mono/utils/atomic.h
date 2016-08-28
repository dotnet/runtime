/*
 * atomic.h:  Atomic operations
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2012 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _WAPI_ATOMIC_H_
#define _WAPI_ATOMIC_H_

#include "config.h"
#include <glib.h>
#include <mono/utils/mono-membar.h>

/*
The current Nexus 7 arm-v7a fails with:
F/MonoDroid( 1568): shared runtime initialization error: Cannot load library: reloc_library[1285]:    37 cannot locate '__sync_val_compare_and_swap_8'

Apple targets have historically being problematic, xcode 4.6 would miscompile the intrinsic.
*/

/* On Windows, we always use the functions provided by the Windows API. */
#if defined(__WIN32__) || defined(_WIN32)

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

/* mingw is missing InterlockedCompareExchange64 () from winbase.h */
#if HAVE_DECL_INTERLOCKEDCOMPAREEXCHANGE64==0
static inline gint64 InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return __sync_val_compare_and_swap (dest, comp, exch);
}
#endif

/* mingw is missing InterlockedExchange64 () from winbase.h */
#if HAVE_DECL_INTERLOCKEDEXCHANGE64==0
static inline gint64 InterlockedExchange64(volatile gint64 *val, gint64 new_val)
{
	gint64 old_val;
	do {
		old_val = *val;
	} while (InterlockedCompareExchange64 (val, new_val, old_val) != old_val);
	return old_val;
}
#endif

/* mingw is missing InterlockedIncrement64 () from winbase.h */
#if HAVE_DECL_INTERLOCKEDINCREMENT64==0
static inline gint64 InterlockedIncrement64(volatile gint64 *val)
{
	return __sync_add_and_fetch (val, 1);
}
#endif

/* mingw is missing InterlockedDecrement64 () from winbase.h */
#if HAVE_DECL_INTERLOCKEDDECREMENT64==0
static inline gint64 InterlockedDecrement64(volatile gint64 *val)
{
	return __sync_sub_and_fetch (val, 1);
}
#endif

/* mingw is missing InterlockedAdd () from winbase.h */
#if HAVE_DECL_INTERLOCKEDADD==0
static inline gint32 InterlockedAdd(volatile gint32 *dest, gint32 add)
{
	return __sync_add_and_fetch (dest, add);
}
#endif

/* mingw is missing InterlockedAdd64 () from winbase.h */
#if HAVE_DECL_INTERLOCKEDADD64==0
static inline gint64 InterlockedAdd64(volatile gint64 *dest, gint64 add)
{
	return __sync_add_and_fetch (dest, add);
}
#endif

#if defined(_MSC_VER) && !defined(InterlockedAdd)
/* MSVC before 2013 only defines InterlockedAdd* for the Itanium architecture */
static inline gint32 InterlockedAdd(volatile gint32 *dest, gint32 add)
{
	return InterlockedExchangeAdd (dest, add) + add;
}
#endif

#if defined(_MSC_VER) && !defined(InterlockedAdd64)
#if defined(InterlockedExchangeAdd64)
/* This may be defined only on amd64 */
static inline gint64 InterlockedAdd64(volatile gint64 *dest, gint64 add)
{
	return InterlockedExchangeAdd64 (dest, add) + add;
}
#else
static inline gint64 InterlockedAdd64(volatile gint64 *dest, gint64 add)
{
	gint64 prev_value;

	do {
		prev_value = *dest;
	} while (prev_value != InterlockedCompareExchange64(dest, prev_value + add, prev_value));

	return prev_value + add;
}
#endif
#endif

#ifdef HOST_WIN32
#define TO_INTERLOCKED_ARGP(ptr) ((volatile LONG*)(ptr))
#else
#define TO_INTERLOCKED_ARGP(ptr) (ptr)
#endif

/* And now for some dirty hacks... The Windows API doesn't
 * provide any useful primitives for this (other than getting
 * into architecture-specific madness), so use CAS. */

static inline gint32 InterlockedRead(volatile gint32 *src)
{
	return InterlockedCompareExchange (TO_INTERLOCKED_ARGP (src), 0, 0);
}

static inline gint64 InterlockedRead64(volatile gint64 *src)
{
	return InterlockedCompareExchange64 (src, 0, 0);
}

static inline gpointer InterlockedReadPointer(volatile gpointer *src)
{
	return InterlockedCompareExchangePointer (src, NULL, NULL);
}

static inline void InterlockedWrite(volatile gint32 *dst, gint32 val)
{
	InterlockedExchange (TO_INTERLOCKED_ARGP (dst), val);
}

static inline void InterlockedWrite64(volatile gint64 *dst, gint64 val)
{
	InterlockedExchange64 (dst, val);
}

static inline void InterlockedWritePointer(volatile gpointer *dst, gpointer val)
{
	InterlockedExchangePointer (dst, val);
}

/* We can't even use CAS for these, so write them out
 * explicitly according to x86(_64) semantics... */

static inline gint8 InterlockedRead8(volatile gint8 *src)
{
	return *src;
}

static inline gint16 InterlockedRead16(volatile gint16 *src)
{
	return *src;
}

static inline void InterlockedWrite8(volatile gint8 *dst, gint8 val)
{
	*dst = val;
	mono_memory_barrier ();
}

static inline void InterlockedWrite16(volatile gint16 *dst, gint16 val)
{
	*dst = val;
	mono_memory_barrier ();
}

/* Prefer GCC atomic ops if the target supports it (see configure.ac). */
#elif defined(USE_GCC_ATOMIC_OPS)

/*
 * As of this comment (August 2016), all current Clang versions get atomic
 * intrinsics on ARM64 wrong. All GCC versions prior to 5.3.0 do, too. The bug
 * is the same: The compiler developers thought that the acq + rel barriers
 * that ARM64 load/store instructions can impose are sufficient to provide
 * sequential consistency semantics. This is not the case:
 *
 *     http://lists.infradead.org/pipermail/linux-arm-kernel/2014-February/229588.html
 *
 * We work around this bug by inserting full barriers around each atomic
 * intrinsic if we detect that we're built with a buggy compiler.
 */

#if defined (HOST_ARM64) && (defined (__clang__) || MONO_GNUC_VERSION < 50300)
#define WRAP_ATOMIC_INTRINSIC(INTRIN) \
	({ \
		mono_memory_barrier (); \
		__typeof__ (INTRIN) atomic_ret__ = (INTRIN); \
		mono_memory_barrier (); \
		atomic_ret__; \
	})

#define gcc_sync_val_compare_and_swap(a, b, c) WRAP_ATOMIC_INTRINSIC (__sync_val_compare_and_swap (a, b, c))
#define gcc_sync_add_and_fetch(a, b) WRAP_ATOMIC_INTRINSIC (__sync_add_and_fetch (a, b))
#define gcc_sync_sub_and_fetch(a, b) WRAP_ATOMIC_INTRINSIC (__sync_sub_and_fetch (a, b))
#define gcc_sync_fetch_and_add(a, b) WRAP_ATOMIC_INTRINSIC (__sync_fetch_and_add (a, b))
#else
#define gcc_sync_val_compare_and_swap(a, b, c) __sync_val_compare_and_swap (a, b, c)
#define gcc_sync_add_and_fetch(a, b) __sync_add_and_fetch (a, b)
#define gcc_sync_sub_and_fetch(a, b) __sync_sub_and_fetch (a, b)
#define gcc_sync_fetch_and_add(a, b) __sync_fetch_and_add (a, b)
#endif

static inline gint32 InterlockedCompareExchange(volatile gint32 *dest,
						gint32 exch, gint32 comp)
{
	return gcc_sync_val_compare_and_swap (dest, comp, exch);
}

static inline gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp)
{
	return gcc_sync_val_compare_and_swap (dest, comp, exch);
}

static inline gint32 InterlockedAdd(volatile gint32 *dest, gint32 add)
{
	return gcc_sync_add_and_fetch (dest, add);
}

static inline gint32 InterlockedIncrement(volatile gint32 *val)
{
	return gcc_sync_add_and_fetch (val, 1);
}

static inline gint32 InterlockedDecrement(volatile gint32 *val)
{
	return gcc_sync_sub_and_fetch (val, 1);
}

static inline gint32 InterlockedExchange(volatile gint32 *val, gint32 new_val)
{
	gint32 old_val;
	do {
		old_val = *val;
	} while (gcc_sync_val_compare_and_swap (val, old_val, new_val) != old_val);
	return old_val;
}

static inline gpointer InterlockedExchangePointer(volatile gpointer *val,
						  gpointer new_val)
{
	gpointer old_val;
	do {
		old_val = *val;
	} while (gcc_sync_val_compare_and_swap (val, old_val, new_val) != old_val);
	return old_val;
}

static inline gint32 InterlockedExchangeAdd(volatile gint32 *val, gint32 add)
{
	return gcc_sync_fetch_and_add (val, add);
}

static inline gint8 InterlockedRead8(volatile gint8 *src)
{
	/* Kind of a hack, but GCC doesn't give us anything better, and it's
	 * certainly not as bad as using a CAS loop. */
	return gcc_sync_fetch_and_add (src, 0);
}

static inline gint16 InterlockedRead16(volatile gint16 *src)
{
	return gcc_sync_fetch_and_add (src, 0);
}

static inline gint32 InterlockedRead(volatile gint32 *src)
{
	return gcc_sync_fetch_and_add (src, 0);
}

static inline void InterlockedWrite8(volatile gint8 *dst, gint8 val)
{
	/* Nothing useful from GCC at all, so fall back to CAS. */
	gint8 old_val;
	do {
		old_val = *dst;
	} while (gcc_sync_val_compare_and_swap (dst, old_val, val) != old_val);
}

static inline void InterlockedWrite16(volatile gint16 *dst, gint16 val)
{
	gint16 old_val;
	do {
		old_val = *dst;
	} while (gcc_sync_val_compare_and_swap (dst, old_val, val) != old_val);
}

static inline void InterlockedWrite(volatile gint32 *dst, gint32 val)
{
	/* Nothing useful from GCC at all, so fall back to CAS. */
	gint32 old_val;
	do {
		old_val = *dst;
	} while (gcc_sync_val_compare_and_swap (dst, old_val, val) != old_val);
}

#if defined (TARGET_OSX) || defined (__arm__) || (defined (__mips__) && !defined (__mips64)) || (defined (__powerpc__) && !defined (__powerpc64__)) || (defined (__sparc__) && !defined (__arch64__))
#define BROKEN_64BIT_ATOMICS_INTRINSIC 1
#endif

#if !defined (BROKEN_64BIT_ATOMICS_INTRINSIC)

static inline gint64 InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return gcc_sync_val_compare_and_swap (dest, comp, exch);
}

static inline gint64 InterlockedAdd64(volatile gint64 *dest, gint64 add)
{
	return gcc_sync_add_and_fetch (dest, add);
}

static inline gint64 InterlockedIncrement64(volatile gint64 *val)
{
	return gcc_sync_add_and_fetch (val, 1);
}

static inline gint64 InterlockedDecrement64(volatile gint64 *val)
{
	return gcc_sync_sub_and_fetch (val, 1);
}

static inline gint64 InterlockedExchangeAdd64(volatile gint64 *val, gint64 add)
{
	return gcc_sync_fetch_and_add (val, add);
}

static inline gint64 InterlockedRead64(volatile gint64 *src)
{
	/* Kind of a hack, but GCC doesn't give us anything better. */
	return gcc_sync_fetch_and_add (src, 0);
}

#else

/* Implement 64-bit cmpxchg by hand or emulate it. */
extern gint64 InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp);

/* Implement all other 64-bit atomics in terms of a specialized CAS
 * in this case, since chances are that the other 64-bit atomic
 * intrinsics are broken too.
 */

static inline gint64 InterlockedExchangeAdd64(volatile gint64 *dest, gint64 add)
{
	gint64 old_val;
	do {
		old_val = *dest;
	} while (InterlockedCompareExchange64 (dest, old_val + add, old_val) != old_val);
	return old_val;
}

static inline gint64 InterlockedIncrement64(volatile gint64 *val)
{
	gint64 get, set;
	do {
		get = *val;
		set = get + 1;
	} while (InterlockedCompareExchange64 (val, set, get) != get);
	return set;
}

static inline gint64 InterlockedDecrement64(volatile gint64 *val)
{
	gint64 get, set;
	do {
		get = *val;
		set = get - 1;
	} while (InterlockedCompareExchange64 (val, set, get) != get);
	return set;
}

static inline gint64 InterlockedAdd64(volatile gint64 *dest, gint64 add)
{
	gint64 get, set;
	do {
		get = *dest;
		set = get + add;
	} while (InterlockedCompareExchange64 (dest, set, get) != get);
	return set;
}

static inline gint64 InterlockedRead64(volatile gint64 *src)
{
	return InterlockedCompareExchange64 (src, 0, 0);
}

#endif

static inline gpointer InterlockedReadPointer(volatile gpointer *src)
{
	return InterlockedCompareExchangePointer (src, NULL, NULL);
}

static inline void InterlockedWritePointer(volatile gpointer *dst, gpointer val)
{
	InterlockedExchangePointer (dst, val);
}

/* We always implement this in terms of a 64-bit cmpxchg since
 * GCC doesn't have an intrisic to model it anyway. */
static inline gint64 InterlockedExchange64(volatile gint64 *val, gint64 new_val)
{
	gint64 old_val;
	do {
		old_val = *val;
	} while (InterlockedCompareExchange64 (val, new_val, old_val) != old_val);
	return old_val;
}

static inline void InterlockedWrite64(volatile gint64 *dst, gint64 val)
{
	/* Nothing useful from GCC at all, so fall back to CAS. */
	InterlockedExchange64 (dst, val);
}

#else

#define WAPI_NO_ATOMIC_ASM

extern gint32 InterlockedCompareExchange(volatile gint32 *dest, gint32 exch, gint32 comp);
extern gint64 InterlockedCompareExchange64(volatile gint64 *dest, gint64 exch, gint64 comp);
extern gpointer InterlockedCompareExchangePointer(volatile gpointer *dest, gpointer exch, gpointer comp);
extern gint32 InterlockedAdd(volatile gint32 *dest, gint32 add);
extern gint64 InterlockedAdd64(volatile gint64 *dest, gint64 add);
extern gint32 InterlockedIncrement(volatile gint32 *dest);
extern gint64 InterlockedIncrement64(volatile gint64 *dest);
extern gint32 InterlockedDecrement(volatile gint32 *dest);
extern gint64 InterlockedDecrement64(volatile gint64 *dest);
extern gint32 InterlockedExchange(volatile gint32 *dest, gint32 exch);
extern gint64 InterlockedExchange64(volatile gint64 *dest, gint64 exch);
extern gpointer InterlockedExchangePointer(volatile gpointer *dest, gpointer exch);
extern gint32 InterlockedExchangeAdd(volatile gint32 *dest, gint32 add);
extern gint64 InterlockedExchangeAdd64(volatile gint64 *dest, gint64 add);
extern gint8 InterlockedRead8(volatile gint8 *src);
extern gint16 InterlockedRead16(volatile gint16 *src);
extern gint32 InterlockedRead(volatile gint32 *src);
extern gint64 InterlockedRead64(volatile gint64 *src);
extern gpointer InterlockedReadPointer(volatile gpointer *src);
extern void InterlockedWrite8(volatile gint8 *dst, gint8 val);
extern void InterlockedWrite16(volatile gint16 *dst, gint16 val);
extern void InterlockedWrite(volatile gint32 *dst, gint32 val);
extern void InterlockedWrite64(volatile gint64 *dst, gint64 val);
extern void InterlockedWritePointer(volatile gpointer *dst, gpointer val);

#endif

#endif /* _WAPI_ATOMIC_H_ */
