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

#ifdef CHECKED_BUILD

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

/*
This can be called by embedders
*/
#define MONO_REQ_API_ENTRYPOINT

/*
The JIT will generate code that will land on this function
*/
#define MONO_REQ_RUNTIME_ENTRYPOINT

#define CHECKED_MONO_INIT() do { checked_build_init (); } while (0)

#define CHECKED_BUILD_THREAD_TRANSITION(transition, info, from_state, suspend_count, next_state, suspend_count_delta) do {	\
	checked_build_thread_transition (transition, info, from_state, suspend_count, next_state, suspend_count_delta);	\
} while (0)

void assert_gc_safe_mode (void);
void assert_gc_unsafe_mode (void);
void assert_gc_neutral_mode (void);

void checked_build_init (void);
void checked_build_thread_transition(const char *transition, void *info, int from_state, int suspend_count, int next_state, int suspend_count_delta);

#else

#define MONO_REQ_GC_SAFE_MODE
#define MONO_REQ_GC_UNSAFE_MODE
#define MONO_REQ_GC_NEUTRAL_MODE
#define MONO_REQ_API_ENTRYPOINT
#define MONO_REQ_RUNTIME_ENTRYPOINT

#define CHECKED_MONO_INIT()
#define CHECKED_BUILD_THREAD_TRANSITION(transition, info, from_state, suspend_count, next_state, suspend_count_delta)

#endif /* CHECKED_BUILD */

#endif
