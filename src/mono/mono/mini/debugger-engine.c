/**
 * \file
 * Debugger Engine shared code.
 *
 * Author:
 *   Zoltan Varga (vargaz@gmail.com)
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include "mini-runtime.h"

#if !defined (DISABLE_SDB) || defined(TARGET_WASM)

#include <glib.h>

#include "debugger-engine.h"

/*
 * Logging support
 */
static int log_level;
static FILE *log_file;

#ifdef HOST_ANDROID
#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { g_print (__VA_ARGS__); } } while (0)
#else
#define DEBUG_PRINTF(level, ...) do { if (G_UNLIKELY ((level) <= log_level)) { fprintf (log_file, __VA_ARGS__); fflush (log_file); } } while (0)
#endif

/*
 * Locking
 */
#define dbg_lock() mono_coop_mutex_lock (&debug_mutex)
#define dbg_unlock() mono_coop_mutex_unlock (&debug_mutex)
static MonoCoopMutex debug_mutex;

void
mono_de_lock (void)
{
	dbg_lock ();
}

void
mono_de_unlock (void)
{
	dbg_unlock ();
}

/* Single stepping engine */
/* Number of single stepping operations in progress */
static int ss_count;


/*
 * mono_de_start_single_stepping:
 *
 *   Turn on single stepping. Can be called multiple times, for example,
 * by a single step event request + a suspend.
 */
void
mono_de_start_single_stepping (void)
{
	int val = mono_atomic_inc_i32 (&ss_count);

	if (val == 1) {
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
		mono_arch_start_single_stepping ();
#endif
		mini_get_interp_callbacks ()->start_single_stepping ();
	}
}

void
mono_de_stop_single_stepping (void)
{
	int val = mono_atomic_dec_i32 (&ss_count);

	if (val == 0) {
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
		mono_arch_stop_single_stepping ();
#endif
		mini_get_interp_callbacks ()->stop_single_stepping ();
	}
}


/*
 * mono_de_set_log_level:
 *
 * Configures logging level and output file. Must be called together with mono_de_init.
 */
void
mono_de_set_log_level (int level, FILE *file)
{
	log_level = level;
	log_file = file;
}

/*
 * mono_de_init:
 *
 * Inits the shared debugger engine. Not reentrant.
 */
void
mono_de_init (void)
{
	mono_coop_mutex_init_recursive (&debug_mutex);
}

void
mono_de_cleanup (void)
{
}

#endif
