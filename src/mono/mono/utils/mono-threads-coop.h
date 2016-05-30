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

G_END_DECLS

#endif
