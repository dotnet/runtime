/**
 * \file
 */

#ifndef __UTILS_MONO_TIME_H__
#define __UTILS_MONO_TIME_H__

#include <mono/utils/mono-compiler.h>
#include <glib.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

/* Returns the number of milliseconds from boot time: this should be monotonic
 *
 * Prefer to use mono_msec_ticks for elapsed time calculation. */
gint64 mono_msec_boottime (void);

/* Returns the number of milliseconds ticks from unspecified time: this should be monotonic */
MONO_COMPONENT_API gint64 mono_msec_ticks (void);

/* Returns the number of 100ns ticks from unspecified time: this should be monotonic */
MONO_COMPONENT_API gint64 mono_100ns_ticks (void);

/* Returns the number of 100ns ticks since 1/1/1601, UTC timezone */
gint64 mono_100ns_datetime (void);

#ifndef HOST_WIN32
gint64 mono_100ns_datetime_from_timeval (struct timeval tv);
#endif

#if defined(HOST_DARWIN)
#include <mach/clock.h>
typedef clock_serv_t mono_clock_id_t;
#elif defined(HAVE_CLOCKID_T)
#include <sys/types.h>
typedef clockid_t mono_clock_id_t;
#else
typedef void *mono_clock_id_t;
#endif

void mono_clock_init (mono_clock_id_t *clk_id);
void mono_clock_cleanup (mono_clock_id_t clk_id);
guint64 mono_clock_get_time_ns (mono_clock_id_t clk_id);

/* Stopwatch class for internal runtime use */
typedef struct {
	gint64 start, stop;
} MonoStopwatch;

static inline void
mono_stopwatch_start (MonoStopwatch *w)
{
	w->start = mono_100ns_ticks ();
	w->stop = 0;
}

static inline void
mono_stopwatch_stop (MonoStopwatch *w)
{
	w->stop = mono_100ns_ticks ();
}

static inline guint64
mono_stopwatch_elapsed (MonoStopwatch *w)
{
	return (w->stop - w->start) / 10;
}

static inline guint64
mono_stopwatch_elapsed_ms (MonoStopwatch *w)
{
	return (mono_stopwatch_elapsed (w) + 500) / 1000;
}

// Expand non-portable strftime shorthands.
#define MONO_STRFTIME_F "%Y-%m-%d" // %F in some systems, but this works on all.
#define MONO_STRFTIME_T "%H:%M:%S" // %T in some systems, but this works on all.

#endif /* __UTILS_MONO_TIME_H__ */
