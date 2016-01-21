/*
 * checked-build.h: Expensive asserts used when mono is built with --with-checked-build=yes
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#ifndef __CHECKED_BUILD_H__
#define __CHECKED_BUILD_H__

#include <config.h>
#include <mono/utils/atomic.h>

// This is for metadata writes which we have chosen not to check at the current time.
// Because in principle this should never happen, we still use a macro so that the exemptions will be easier to find, and remove, later.
// The current reason why this is needed is for pointers to constant strings, which the checker cannot verify yet.
#define CHECKED_METADATA_WRITE_PTR_EXEMPT(ptr, val) do { (ptr) = (val); } while (0)

#if defined(CHECKED_BUILD)

#define g_assert_checked g_assert

/*
This can be called by embedders
*/
#define MONO_REQ_API_ENTRYPOINT

/*
The JIT will generate code that will land on this function
*/
#define MONO_REQ_RUNTIME_ENTRYPOINT

#define CHECKED_MONO_INIT() do { checked_build_init (); } while (0)

void checked_build_init (void);

#else

#define g_assert_checked(...)

#define MONO_REQ_API_ENTRYPOINT
#define MONO_REQ_RUNTIME_ENTRYPOINT

#define CHECKED_MONO_INIT()

#endif /* CHECKED_BUILD */

#if defined(CHECKED_BUILD) && !defined(DISABLE_CHECKED_BUILD_GC)

/*
GC runtime modes rules:

- GC Safe
Can:
Call into foreigh functions.
Call GC Safe or Neutral modes functions.
Read from pinned managed memory.

Cannot:
Touch managed memory (read/write).
Be dettached.

What's good for?
Doing blocking calls.

- GC Unsafe
Can:
Touch managed memory (read/write).
Call GC Unsafe or Neutral modes functions.

Cannot:
Call foreign native code (embedder callbacks, pinvokes, etc)
Call into any Blocking functions/syscalls (mutexes, IO, etc)
Be dettached.

What's good for?
Poking into managed memory.

-- GC Neutral
Can:
Call other GC Neutral mode functions.

Cannot:
Touch managed memory.
Call foreign native code (embedder callbacks, pinvokes, etc)
Call into any Blocking functions/syscalls (mutexes, IO, etc)
Be dettached.

What's good for?
Functions that can be called from both coop or preept modes.

*/

#define MONO_REQ_GC_SAFE_MODE do {	\
	assert_gc_safe_mode ();	\
} while (0);

#define MONO_REQ_GC_UNSAFE_MODE do {	\
	assert_gc_unsafe_mode ();	\
} while (0);

#define MONO_REQ_GC_NEUTRAL_MODE do {	\
	assert_gc_neutral_mode ();	\
} while (0);

/* In a GC critical region, the thread is not allowed to switch to GC safe mode.
 * For example if the thread is about to call a method that will manipulate managed objects.
 * The GC critical region must only occur in unsafe mode.
 */
#define MONO_PREPARE_GC_CRITICAL_REGION					\
	MONO_REQ_GC_UNSAFE_MODE						\
	do {								\
		void* __critical_gc_region_cookie = critical_gc_region_begin()

#define MONO_FINISH_GC_CRITICAL_REGION			\
		critical_gc_region_end(__critical_gc_region_cookie);	\
	} while(0)

/* Verify that the thread is not currently in a GC critical region. */
#define MONO_REQ_GC_NOT_CRITICAL do {			\
		assert_not_in_gc_critical_region();	\
	} while(0)

/* Verify that the thread is currently in a GC critical region. */
#define MONO_REQ_GC_CRITICAL do {			\
		assert_in_gc_critical_region();	\
	} while(0)

void assert_gc_safe_mode (void);
void assert_gc_unsafe_mode (void);
void assert_gc_neutral_mode (void);

void* critical_gc_region_begin(void);
void critical_gc_region_end(void* token);
void assert_not_in_gc_critical_region(void);
void assert_in_gc_critical_region (void);

#else

#define MONO_REQ_GC_SAFE_MODE
#define MONO_REQ_GC_UNSAFE_MODE
#define MONO_REQ_GC_NEUTRAL_MODE

#define MONO_PREPARE_GC_CRITICAL_REGION
#define MONO_FINISH_GC_CRITICAL_REGION

#define MONO_REQ_GC_NOT_CRITICAL
#define MONO_REQ_GC_CRITICAL

#endif /* defined(CHECKED_BUILD) && !defined(DISABLE_CHECKED_BUILD_GC) */

#if defined(CHECKED_BUILD) && !defined(DISABLE_CHECKED_BUILD_METADATA)

// Use when writing a pointer from one image or imageset to another.
#define CHECKED_METADATA_WRITE_PTR(ptr, val) do {    \
    check_metadata_store (&(ptr), (val));    \
    (ptr) = (val);    \
} while (0);

// Use when writing a pointer from an image or imageset to itself.
#define CHECKED_METADATA_WRITE_PTR_LOCAL(ptr, val) do {    \
    check_metadata_store_local (&(ptr), (val));    \
    (ptr) = (val);    \
} while (0);

// Use when writing a pointer from one image or imageset to another (atomic version).
#define CHECKED_METADATA_WRITE_PTR_ATOMIC(ptr, val) do {    \
    check_metadata_store (&(ptr), (val));    \
    mono_atomic_store_release (&(ptr), (val));    \
} while (0);

void check_metadata_store(void *from, void *to);
void check_metadata_store_local(void *from, void *to);

#else

#define CHECKED_METADATA_WRITE_PTR(ptr, val) do { (ptr) = (val); } while (0)
#define CHECKED_METADATA_WRITE_PTR_LOCAL(ptr, val) do { (ptr) = (val); } while (0)
#define CHECKED_METADATA_WRITE_PTR_ATOMIC(ptr, val) do { mono_atomic_store_release (&(ptr), (val)); } while (0)

#endif /* defined(CHECKED_BUILD) && !defined(DISABLE_CHECKED_BUILD_METADATA) */

#if defined(CHECKED_BUILD) && !defined(DISABLE_CHECKED_BUILD_THREAD)

#define CHECKED_BUILD_THREAD_TRANSITION(transition, info, from_state, suspend_count, next_state, suspend_count_delta) do {	\
	checked_build_thread_transition (transition, info, from_state, suspend_count, next_state, suspend_count_delta);	\
} while (0)

void checked_build_thread_transition(const char *transition, void *info, int from_state, int suspend_count, int next_state, int suspend_count_delta);

#else

#define CHECKED_BUILD_THREAD_TRANSITION(transition, info, from_state, suspend_count, next_state, suspend_count_delta)

#endif /* defined(CHECKED_BUILD) && !defined(DISABLE_CHECKED_BUILD_THREAD) */

#endif /* __CHECKED_BUILD_H__ */
