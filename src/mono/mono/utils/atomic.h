/**
 * \file
 * Atomic operations
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
#include <mono/utils/mono-compiler.h>

/*
The current Nexus 7 arm-v7a fails with:
F/MonoDroid( 1568): shared runtime initialization error: Cannot load library: reloc_library[1285]:    37 cannot locate '__sync_val_compare_and_swap_8'

Apple targets have historically being problematic, xcode 4.6 would miscompile the intrinsic.
*/

#if defined(HOST_WIN32)

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>

static inline gint32
mono_atomic_cas_i32 (volatile gint32 *dest, gint32 exch, gint32 comp)
{
	return InterlockedCompareExchange ((LONG volatile *)dest, (LONG)exch, (LONG)comp);
}

static inline gint64
mono_atomic_cas_i64 (volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return InterlockedCompareExchange64 ((LONG64 volatile *)dest, (LONG64)exch, (LONG64)comp);
}

static inline gpointer
mono_atomic_cas_ptr (volatile gpointer *dest, gpointer exch, gpointer comp)
{
	return InterlockedCompareExchangePointer ((PVOID volatile *)dest, (PVOID)exch, (PVOID)comp);
}

static inline gint32
mono_atomic_add_i32 (volatile gint32 *dest, gint32 add)
{
	return InterlockedAdd ((LONG volatile *)dest, (LONG)add);
}

static inline gint64
mono_atomic_add_i64 (volatile gint64 *dest, gint64 add)
{
	return InterlockedAdd64 ((LONG64 volatile *)dest, (LONG64)add);
}

static inline gint32
mono_atomic_inc_i32 (volatile gint32 *dest)
{
	return InterlockedIncrement ((LONG volatile *)dest);
}

static inline gint64
mono_atomic_inc_i64 (volatile gint64 *dest)
{
	return InterlockedIncrement64 ((LONG64 volatile *)dest);
}

static inline gint32
mono_atomic_dec_i32 (volatile gint32 *dest)
{
	return InterlockedDecrement ((LONG volatile *)dest);
}

static inline gint64
mono_atomic_dec_i64 (volatile gint64 *dest)
{
	return InterlockedDecrement64 ((LONG64 volatile *)dest);
}

static inline gint32
mono_atomic_xchg_i32 (volatile gint32 *dest, gint32 exch)
{
	return InterlockedExchange ((LONG volatile *)dest, (LONG)exch);
}

static inline gint64
mono_atomic_xchg_i64 (volatile gint64 *dest, gint64 exch)
{
	return InterlockedExchange64 ((LONG64 volatile *)dest, (LONG64)exch);
}

static inline gpointer
mono_atomic_xchg_ptr (volatile gpointer *dest, gpointer exch)
{
	return InterlockedExchangePointer ((PVOID volatile *)dest, (PVOID)exch);
}

static inline gint32
mono_atomic_fetch_add_i32 (volatile gint32 *dest, gint32 add)
{
	return InterlockedExchangeAdd ((LONG volatile *)dest, (LONG)add);
}

static inline gint64
mono_atomic_fetch_add_i64 (volatile gint64 *dest, gint64 add)
{
	return InterlockedExchangeAdd64 ((LONG64 volatile *)dest, (LONG)add);
}

static inline gint8
mono_atomic_load_i8 (volatile gint8 *src)
{
	gint8 loaded_value = *src;
	_ReadWriteBarrier ();

	return loaded_value;
}

static inline gint16
mono_atomic_load_i16 (volatile gint16 *src)
{
	gint16 loaded_value = *src;
	_ReadWriteBarrier ();

	return loaded_value;
}

static inline gint32 mono_atomic_load_i32 (volatile gint32 *src)
{
	gint32 loaded_value = *src;
	_ReadWriteBarrier ();

	return loaded_value;
}

static inline gint64
mono_atomic_load_i64 (volatile gint64 *src)
{
#if defined(TARGET_AMD64)
	gint64 loaded_value = *src;
	_ReadWriteBarrier ();

	return loaded_value;
#else
	return InterlockedCompareExchange64 ((LONG64 volatile *)src, 0, 0);
#endif
}

static inline gpointer
mono_atomic_load_ptr (volatile gpointer *src)
{
	gpointer loaded_value = *src;
	_ReadWriteBarrier ();

	return loaded_value;
}

static inline void
mono_atomic_store_i8 (volatile gint8 *dst, gint8 val)
{
#if (_MSC_VER >= 1600)
	_InterlockedExchange8 ((CHAR volatile *)dst, (CHAR)val);
#else
	*dst = val;
	mono_memory_barrier ();
#endif
}

static inline void
mono_atomic_store_i16 (volatile gint16 *dst, gint16 val)
{
#if (_MSC_VER >= 1600)
	InterlockedExchange16 ((SHORT volatile *)dst, (SHORT)val);
#else
	*dst = val;
	mono_memory_barrier ();
#endif
}

static inline void
mono_atomic_store_i32 (volatile gint32 *dst, gint32 val)
{
	InterlockedExchange ((LONG volatile *)dst, (LONG)val);
}

static inline void
mono_atomic_store_i64 (volatile gint64 *dst, gint64 val)
{
	InterlockedExchange64 ((LONG64 volatile *)dst, (LONG64)val);
}

static inline void
mono_atomic_store_ptr (volatile gpointer *dst, gpointer val)
{
	InterlockedExchangePointer ((PVOID volatile *)dst, (PVOID)val);
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

static inline gint32 mono_atomic_cas_i32(volatile gint32 *dest,
						gint32 exch, gint32 comp)
{
	return gcc_sync_val_compare_and_swap (dest, comp, exch);
}

static inline gpointer mono_atomic_cas_ptr(volatile gpointer *dest, gpointer exch, gpointer comp)
{
	return gcc_sync_val_compare_and_swap (dest, comp, exch);
}

static inline gint32 mono_atomic_add_i32(volatile gint32 *dest, gint32 add)
{
	return gcc_sync_add_and_fetch (dest, add);
}

static inline gint32 mono_atomic_inc_i32(volatile gint32 *val)
{
	return gcc_sync_add_and_fetch (val, 1);
}

static inline gint32 mono_atomic_dec_i32(volatile gint32 *val)
{
	return gcc_sync_sub_and_fetch (val, 1);
}

static inline gint32 mono_atomic_xchg_i32(volatile gint32 *val, gint32 new_val)
{
	gint32 old_val;
	do {
		old_val = *val;
	} while (gcc_sync_val_compare_and_swap (val, old_val, new_val) != old_val);
	return old_val;
}

static inline gpointer mono_atomic_xchg_ptr(volatile gpointer *val,
						  gpointer new_val)
{
	gpointer old_val;
	do {
		old_val = *val;
	} while (gcc_sync_val_compare_and_swap (val, old_val, new_val) != old_val);
	return old_val;
}

static inline gint32 mono_atomic_fetch_add_i32(volatile gint32 *val, gint32 add)
{
	return gcc_sync_fetch_and_add (val, add);
}

static inline gint8 mono_atomic_load_i8(volatile gint8 *src)
{
	/* Kind of a hack, but GCC doesn't give us anything better, and it's
	 * certainly not as bad as using a CAS loop. */
	return gcc_sync_fetch_and_add (src, 0);
}

static inline gint16 mono_atomic_load_i16(volatile gint16 *src)
{
	return gcc_sync_fetch_and_add (src, 0);
}

static inline gint32 mono_atomic_load_i32(volatile gint32 *src)
{
	return gcc_sync_fetch_and_add (src, 0);
}

static inline void mono_atomic_store_i8(volatile gint8 *dst, gint8 val)
{
	/* Nothing useful from GCC at all, so fall back to CAS. */
	gint8 old_val;
	do {
		old_val = *dst;
	} while (gcc_sync_val_compare_and_swap (dst, old_val, val) != old_val);
}

static inline void mono_atomic_store_i16(volatile gint16 *dst, gint16 val)
{
	gint16 old_val;
	do {
		old_val = *dst;
	} while (gcc_sync_val_compare_and_swap (dst, old_val, val) != old_val);
}

static inline void mono_atomic_store_i32(volatile gint32 *dst, gint32 val)
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

static inline gint64 mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp)
{
	return gcc_sync_val_compare_and_swap (dest, comp, exch);
}

static inline gint64 mono_atomic_add_i64(volatile gint64 *dest, gint64 add)
{
	return gcc_sync_add_and_fetch (dest, add);
}

static inline gint64 mono_atomic_inc_i64(volatile gint64 *val)
{
	return gcc_sync_add_and_fetch (val, 1);
}

static inline gint64 mono_atomic_dec_i64(volatile gint64 *val)
{
	return gcc_sync_sub_and_fetch (val, 1);
}

static inline gint64 mono_atomic_fetch_add_i64(volatile gint64 *val, gint64 add)
{
	return gcc_sync_fetch_and_add (val, add);
}

static inline gint64 mono_atomic_load_i64(volatile gint64 *src)
{
	/* Kind of a hack, but GCC doesn't give us anything better. */
	return gcc_sync_fetch_and_add (src, 0);
}

#else

/* Implement 64-bit cas by hand or emulate it. */
extern gint64 mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp);

/* Implement all other 64-bit atomics in terms of a specialized CAS
 * in this case, since chances are that the other 64-bit atomic
 * intrinsics are broken too.
 */

static inline gint64 mono_atomic_fetch_add_i64(volatile gint64 *dest, gint64 add)
{
	gint64 old_val;
	do {
		old_val = *dest;
	} while (mono_atomic_cas_i64 (dest, old_val + add, old_val) != old_val);
	return old_val;
}

static inline gint64 mono_atomic_inc_i64(volatile gint64 *val)
{
	gint64 get, set;
	do {
		get = *val;
		set = get + 1;
	} while (mono_atomic_cas_i64 (val, set, get) != get);
	return set;
}

static inline gint64 mono_atomic_dec_i64(volatile gint64 *val)
{
	gint64 get, set;
	do {
		get = *val;
		set = get - 1;
	} while (mono_atomic_cas_i64 (val, set, get) != get);
	return set;
}

static inline gint64 mono_atomic_add_i64(volatile gint64 *dest, gint64 add)
{
	gint64 get, set;
	do {
		get = *dest;
		set = get + add;
	} while (mono_atomic_cas_i64 (dest, set, get) != get);
	return set;
}

static inline gint64 mono_atomic_load_i64(volatile gint64 *src)
{
	return mono_atomic_cas_i64 (src, 0, 0);
}

#endif

static inline gpointer mono_atomic_load_ptr(volatile gpointer *src)
{
	return mono_atomic_cas_ptr (src, NULL, NULL);
}

static inline void mono_atomic_store_ptr(volatile gpointer *dst, gpointer val)
{
	mono_atomic_xchg_ptr (dst, val);
}

/* We always implement this in terms of a 64-bit cas since
 * GCC doesn't have an intrisic to model it anyway. */
static inline gint64 mono_atomic_xchg_i64(volatile gint64 *val, gint64 new_val)
{
	gint64 old_val;
	do {
		old_val = *val;
	} while (mono_atomic_cas_i64 (val, new_val, old_val) != old_val);
	return old_val;
}

static inline void mono_atomic_store_i64(volatile gint64 *dst, gint64 val)
{
	/* Nothing useful from GCC at all, so fall back to CAS. */
	mono_atomic_xchg_i64 (dst, val);
}

#else

#define WAPI_NO_ATOMIC_ASM

extern gint32 mono_atomic_cas_i32(volatile gint32 *dest, gint32 exch, gint32 comp);
extern gint64 mono_atomic_cas_i64(volatile gint64 *dest, gint64 exch, gint64 comp);
extern gpointer mono_atomic_cas_ptr(volatile gpointer *dest, gpointer exch, gpointer comp);
extern gint32 mono_atomic_add_i32(volatile gint32 *dest, gint32 add);
extern gint64 mono_atomic_add_i64(volatile gint64 *dest, gint64 add);
extern gint32 mono_atomic_inc_i32(volatile gint32 *dest);
extern gint64 mono_atomic_inc_i64(volatile gint64 *dest);
extern gint32 mono_atomic_dec_i32(volatile gint32 *dest);
extern gint64 mono_atomic_dec_i64(volatile gint64 *dest);
extern gint32 mono_atomic_xchg_i32(volatile gint32 *dest, gint32 exch);
extern gint64 mono_atomic_xchg_i64(volatile gint64 *dest, gint64 exch);
extern gpointer mono_atomic_xchg_ptr(volatile gpointer *dest, gpointer exch);
extern gint32 mono_atomic_fetch_add_i32(volatile gint32 *dest, gint32 add);
extern gint64 mono_atomic_fetch_add_i64(volatile gint64 *dest, gint64 add);
extern gint8 mono_atomic_load_i8(volatile gint8 *src);
extern gint16 mono_atomic_load_i16(volatile gint16 *src);
extern gint32 mono_atomic_load_i32(volatile gint32 *src);
extern gint64 mono_atomic_load_i64(volatile gint64 *src);
extern gpointer mono_atomic_load_ptr(volatile gpointer *src);
extern void mono_atomic_store_i8(volatile gint8 *dst, gint8 val);
extern void mono_atomic_store_i16(volatile gint16 *dst, gint16 val);
extern void mono_atomic_store_i32(volatile gint32 *dst, gint32 val);
extern void mono_atomic_store_i64(volatile gint64 *dst, gint64 val);
extern void mono_atomic_store_ptr(volatile gpointer *dst, gpointer val);

#endif

#if SIZEOF_VOID_P == 4
#define mono_atomic_fetch_add_word(p,add) mono_atomic_fetch_add_i32 ((volatile gint32*)p, (gint32)add)
#else
#define mono_atomic_fetch_add_word(p,add) mono_atomic_fetch_add_i64 ((volatile gint64*)p, (gint64)add)
#endif

/* The following functions cannot be found on any platform, and thus they can be declared without further existence checks */

static inline void
mono_atomic_store_bool (volatile gboolean *dest, gboolean val)
{
	/* both, gboolean and gint32, are int32_t; the purpose of these casts is to make things explicit */
	mono_atomic_store_i32 ((volatile gint32 *)dest, (gint32)val);
}

#if defined (WAPI_NO_ATOMIC_ASM)
#define MONO_ATOMIC_USES_LOCK
#elif defined(BROKEN_64BIT_ATOMICS_INTRINSIC)
#if !defined(TARGET_OSX) && !(defined (__arm__) && defined (HAVE_ARMV7) && (defined(TARGET_IOS) || defined(TARGET_WATCHOS) || defined(TARGET_ANDROID)))
#define MONO_ATOMIC_USES_LOCK
#endif
#endif

#endif /* _WAPI_ATOMIC_H_ */
