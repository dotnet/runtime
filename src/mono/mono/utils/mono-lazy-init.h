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

typedef gint32 mono_lazy_init_t;

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

	if (status >= MONO_LAZY_INIT_STATUS_INITIALIZED)
		return status == MONO_LAZY_INIT_STATUS_INITIALIZED;
	if (status == MONO_LAZY_INIT_STATUS_INITIALIZING
	     || mono_atomic_cas_i32 (lazy_init, MONO_LAZY_INIT_STATUS_INITIALIZING, MONO_LAZY_INIT_STATUS_NOT_INITIALIZED)
	         != MONO_LAZY_INIT_STATUS_NOT_INITIALIZED
	) {
		while (*lazy_init == MONO_LAZY_INIT_STATUS_INITIALIZING)
			mono_thread_info_yield ();
		g_assert (mono_atomic_load_i32 (lazy_init) >= MONO_LAZY_INIT_STATUS_INITIALIZED);
		return status == MONO_LAZY_INIT_STATUS_INITIALIZED;
	}

	initialize ();

	mono_atomic_store_release (lazy_init, MONO_LAZY_INIT_STATUS_INITIALIZED);
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
