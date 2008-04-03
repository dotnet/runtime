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

#endif /* __UTILS_MONO_TIME_H__ */

