/*
 * mono-threads-coop.h: Cooperative suspend thread helpers
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#ifndef __MONO_THREADS_COOP_H__
#define __MONO_THREADS_COOP_H__

#include <config.h>
#include <glib.h>

#include "checked-build.h"
#include "mono-threads.h"
#include "mono-threads-api.h"

G_BEGIN_DECLS

/* JIT specific interface */
extern volatile size_t mono_polling_required;

/* Runtime consumable API */

gboolean
mono_threads_is_coop_enabled (void);

/* Internal API */

void
mono_threads_state_poll (void);

static inline void
mono_threads_safepoint (void)
{
	if (G_UNLIKELY (mono_polling_required))
		mono_threads_state_poll ();
}

/*
 * The following are used for wrappers and trampolines as their
 * calls might be unbalanced, due to exception unwinding.
 */

gpointer
mono_threads_enter_gc_safe_region_unbalanced (gpointer *stackdata);

void
mono_threads_exit_gc_safe_region_unbalanced (gpointer cookie, gpointer *stackdata);

gpointer
mono_threads_enter_gc_unsafe_region_unbalanced (gpointer *stackdata);

void
mono_threads_exit_gc_unsafe_region_unbalanced (gpointer cookie, gpointer *stackdata);

#define MONO_ENTER_GC_UNSAFE_UNBALANCED	\
	do {	\
		gpointer __dummy;	\
		gpointer __reset_cookie = mono_threads_enter_gc_unsafe_region_unbalanced (&__dummy)

#define MONO_EXIT_GC_UNSAFE_UNBALANCED	\
		mono_threads_exit_gc_unsafe_region_unbalanced (__reset_cookie, &__dummy);	\
	} while (0)

G_END_DECLS

#endif
