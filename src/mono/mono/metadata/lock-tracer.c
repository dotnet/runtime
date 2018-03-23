/**
 * \file
 * Runtime simple lock tracer
 *
 * Authors:
 *	Rodrigo Kumpera (rkumpera@novell.com)
 * 
 */

#include <config.h>
#include <stdio.h>
#include <string.h>

#include <sys/types.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-threads.h>

#include "lock-tracer.h"

/*
 * This is a very simple lock trace implementation. It can be used to verify that the runtime is
 * correctly following all locking rules.
 * 
 * To log more kind of locks just do the following:
 * 	- add an entry into the RuntimeLocks enum
 *  - change mono_os_mutex_lock(mutex) to mono_locks_os_acquire (mutex, LockName)
 *  - change mono_os_mutex_unlock(mutex) to mono_locks_os_release (mutex, LockName)
 *  - change mono_coop_mutex_lock(mutex) to mono_locks_coop_acquire (mutex, LockName)
 *  - change mono_coop_mutex_unlock(mutex) to mono_locks_coop_release (mutex, LockName)
 *  - change the decoder to understand the new lock kind.
 *
 * TODO:
 * 	- Use unbuffered IO without fsync
 *  - Switch to a binary log format
 *  - Enable tracing of more runtime locks
 *  - Add lock check assertions (must_not_hold_any_lock_but, must_hold_lock, etc)
 *   This should be used to verify methods that expect that a given lock is held at entrypoint, for example.
 * 
 * To use the trace, define LOCK_TRACER in lock-trace.h and when running mono define MONO_ENABLE_LOCK_TRACER.
 * This will produce a locks.ZZZ where ZZZ is the pid of the mono process.
 * Use the decoder to verify the result.
 */

#ifdef LOCK_TRACER

#ifdef TARGET_OSX
#include <dlfcn.h>
#endif

static FILE *trace_file;
static mono_mutex_t tracer_lock;
static size_t base_address;

typedef enum {
	RECORD_MUST_NOT_HOLD_ANY,
	RECORD_MUST_NOT_HOLD_ONE,
	RECORD_MUST_HOLD_ONE,
	RECORD_LOCK_ACQUIRED,
	RECORD_LOCK_RELEASED
} RecordType;

void
mono_locks_tracer_init (void)
{
	Dl_info info;
	int res;
	char *name;
	mono_os_mutex_init_recursive (&tracer_lock);

	if (!g_hasenv ("MONO_ENABLE_LOCK_TRACER"))
		return;

	name = g_strdup_printf ("locks.%d", getpid ());
	trace_file = fopen (name, "w+");
	g_free (name);

#ifdef TARGET_OSX
	res = dladdr ((void*)&mono_locks_tracer_init, &info);
	/* The 0x1000 offset was found by empirically trying it. */
	if (res)
		base_address = (size_t)info.dli_fbase - 0x1000;
#endif
}


#ifdef HAVE_EXECINFO_H

static int
mono_backtrace (gpointer array[], int traces)
{
	return backtrace (array, traces);
}

#else

static int
mono_backtrace (gpointer array[], int traces)
{
	return 0;
}

#endif

static void
add_record (RecordType record_kind, RuntimeLocks kind, gpointer lock)
{
	int i = 0;
	const int no_frames = 6;
	gpointer frames[no_frames];

	char *msg;
 	if (!trace_file)
		return;

	memset (frames, 0, sizeof (gpointer) * no_frames);
	mono_backtrace (frames, no_frames);
	for (i = 0; i < no_frames; ++i)
		frames [i] = (gpointer)((size_t)frames[i] - base_address);

	/*We only dump 5 frames, which should be more than enough to most analysis.*/
	msg = g_strdup_printf ("%x,%d,%d,%p,%p,%p,%p,%p,%p\n", (guint32)mono_native_thread_id_get (), record_kind, kind, lock, frames [1], frames [2], frames [3], frames [4], frames [5]);
	fwrite (msg, strlen (msg), 1, trace_file);
	fflush (trace_file);
	g_free (msg);
}

void
mono_locks_lock_acquired (RuntimeLocks kind, gpointer lock)
{
	add_record (RECORD_LOCK_ACQUIRED, kind, lock);
}

void
mono_locks_lock_released (RuntimeLocks kind, gpointer lock)
{
	add_record (RECORD_LOCK_RELEASED, kind, lock);
}
#else

MONO_EMPTY_SOURCE_FILE (lock_tracer);
#endif /* LOCK_TRACER */
