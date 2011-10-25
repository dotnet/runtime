#ifndef __UTILS_MONO_TIME_H__
#define __UTILS_MONO_TIME_H__

#include <mono/utils/mono-compiler.h>
#include <glib.h>

/* Returns the number of milliseconds from boot time: this should be monotonic */
guint32 mono_msec_ticks      (void) MONO_INTERNAL;

/* Returns the number of 100ns ticks from unspecified time: this should be monotonic */
gint64  mono_100ns_ticks     (void) MONO_INTERNAL;

/* Returns the number of 100ns ticks since 1/1/1, UTC timezone */
gint64  mono_100ns_datetime  (void) MONO_INTERNAL;

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

#endif /* __UTILS_MONO_TIME_H__ */

