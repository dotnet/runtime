/**
 * \file
 * Lazy initialization and cleanup utilities
 *
 * Authors: Ludovic Henry <ludovic@xamarin.com>
 *
 * Copyright 2015 Xamarin, Inc. (www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_LAZY_INIT_H__
#define __MONO_LAZY_INIT_H__

#include <glib.h>

#include <config.h>

#include "atomic.h"
#include "mono-threads.h"
#include "mono-memory-model.h"

/*
 * These functions should be used if you want some form of lazy initialization. You can have a look at the
 * threadpool for a more detailed example.
 *
 * The idea is that a module can be in 5 different states:
 *  - not initialized: it is the first state it starts in
 *  - initializing/initialized: whenever we need this module for the first time, we need to initialize it: allocate
 *     memory, launch background thread, etc. To achieve this, we have a module specific function (let's call it
 *     initialize)
 *  - cleaning/cleaned: when we want to clean this module specific data up, then we need to clean it up: deallocate
 *     memory, wait for background threads to finish, etc. As for the initialization process, we need a module specific
 *     function (let's call it cleanup)
 *
 * The switch from one state to the other can only happen in the following ways:
 *  - not initialized
 *  - not initialized -> initializing -> initialized
 *  - not initialized -> cleaned
 *  - not initialized -> initializing -> initialized -> cleaning -> cleaned
 *
 * The initialize and cleanup functions are guaranteed to:
 *  - be each called once and only once
 *  - not be called concurrently (either 2+ initialize or 2+ cleanup, either initialize and cleanup)
 */

typedef volatile gint32 mono_lazy_init_t;

enum {
	MONO_LAZY_INIT_STATUS_NOT_INITIALIZED,
	MONO_LAZY_INIT_STATUS_INITIALIZING,
	MONO_LAZY_INIT_STATUS_INITIALIZED,
	MONO_LAZY_INIT_STATUS_CLEANING,
	MONO_LAZY_INIT_STATUS_CLEANED,
};

static inline gboolean
mono_lazy_initialize (mono_lazy_init_t *lazy_init, void (*initialize) (void))
{
	gint32 status;

	g_assert (lazy_init);

	status = *lazy_init;

	// This barrier might be redundant with volatile.
	//
	// Without either, code in our caller can
	// read state ahead of the call to mono_lazy_initialize,
	// and ahead of the call to initialize.
	//
	// Recall that barriers come in pairs.
	// One barrier is in mono_atomic_cas_i32 below.
	// This is the other.
	//
	// A common case of initializing a pointer, that
	// the reader dereferences, is ok,
	// on most architectures (not Alpha), due to "data dependency".
	//
	// But if the caller is merely reading globals, that initialize writes,
	// then those reads can run ahead of initialize and be incorrect.
	//
	// On-demand initialization is much tricker than generally understood.
	//
	// Strongly consider adapting:
	//   http://www.open-std.org/jtc1/sc22/wg21/docs/papers/2008/n2660.htm
	//
	// At the very bottom. After making it coop-friendly.
	//
	// In particular, it eliminates the barriers from the fast path.
	// At the cost of a thread local access.
	//
	// The thread local access should be "gamed" (forced to initialize
	// early on platforms that do on-demand initialization), by inserting
	// an extra use early in runtime initialization. i.e. so it does not
	// take any locks, and become coop-unfriendly.
	//
	mono_memory_read_barrier ();

	if (status >= MONO_LAZY_INIT_STATUS_INITIALIZED)
		return status == MONO_LAZY_INIT_STATUS_INITIALIZED;

	if (status == MONO_LAZY_INIT_STATUS_INITIALIZING
	     || mono_atomic_cas_i32 (lazy_init, MONO_LAZY_INIT_STATUS_INITIALIZING, MONO_LAZY_INIT_STATUS_NOT_INITIALIZED)
	         != MONO_LAZY_INIT_STATUS_NOT_INITIALIZED
	) {
		// FIXME: This is not coop-friendly.
		while (*lazy_init == MONO_LAZY_INIT_STATUS_INITIALIZING)
			mono_thread_info_yield ();

		g_assert (mono_atomic_load_i32 (lazy_init) >= MONO_LAZY_INIT_STATUS_INITIALIZED);

		// This result is transient. Another thread can proceed to cleanup.
		// Perhaps cleanup should not be attempted, just on-demand initialization.
		return *lazy_init == MONO_LAZY_INIT_STATUS_INITIALIZED;
	}

	initialize ();

	mono_atomic_store_release (lazy_init, MONO_LAZY_INIT_STATUS_INITIALIZED);

	// This result is transient. Another thread can proceed to cleanup.
	// Perhaps cleanup should not be attempted, just on-demand initialization.
	return TRUE;
}

static inline void
mono_lazy_cleanup (mono_lazy_init_t *lazy_init, void (*cleanup) (void))
{
	gint32 status;

	g_assert (lazy_init);

	status = *lazy_init;

	if (status == MONO_LAZY_INIT_STATUS_NOT_INITIALIZED
	     && mono_atomic_cas_i32 (lazy_init, MONO_LAZY_INIT_STATUS_CLEANED, MONO_LAZY_INIT_STATUS_NOT_INITIALIZED)
	         == MONO_LAZY_INIT_STATUS_NOT_INITIALIZED
	) {
		return;
	}
	if (status == MONO_LAZY_INIT_STATUS_INITIALIZING) {
		while ((status = *lazy_init) == MONO_LAZY_INIT_STATUS_INITIALIZING)
			mono_thread_info_yield ();
	}

	if (status == MONO_LAZY_INIT_STATUS_CLEANED)
		return;
	if (status == MONO_LAZY_INIT_STATUS_CLEANING
	     || mono_atomic_cas_i32 (lazy_init, MONO_LAZY_INIT_STATUS_CLEANING, MONO_LAZY_INIT_STATUS_INITIALIZED)
	         != MONO_LAZY_INIT_STATUS_INITIALIZED
	) {
		while (*lazy_init == MONO_LAZY_INIT_STATUS_CLEANING)
			mono_thread_info_yield ();
		g_assert (mono_atomic_load_i32 (lazy_init) == MONO_LAZY_INIT_STATUS_CLEANED);
		return;
	}

	cleanup ();

	mono_atomic_store_release (lazy_init, MONO_LAZY_INIT_STATUS_CLEANED);
}

static inline gboolean
mono_lazy_is_initialized (mono_lazy_init_t *lazy_init)
{
	g_assert (lazy_init);
	return mono_atomic_load_i32 (lazy_init) == MONO_LAZY_INIT_STATUS_INITIALIZED;
}

#endif
