/*
 * lock-tracer.c: Runtime simple lock tracer
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

#include <mono/io-layer/io-layer.h>

#include "lock-tracer.h"

/*
 * This is a very simple lock trace implementation. It can be used to verify that the runtime is
 * correctly following all locking rules.
 * 
 * To log more kind of locks just do the following:
 * 	- add an entry into the RuntimeLocks enum
 *  - change EnterCriticalSection(mutex) to mono_locks_acquire (mutex, LockName)
 *  - change LeaveCriticalSection to mono_locks_release (mutex, LockName)
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

static FILE *trace_file;
static CRITICAL_SECTION tracer_lock;

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
	char *name;
	InitializeCriticalSection (&tracer_lock);
	if (!getenv ("MONO_ENABLE_LOCK_TRACER"))
		return;
	name = g_strdup_printf ("locks.%d", getpid ());
	trace_file = fopen (name, "w+");
	g_free (name);
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
	gpointer frames[10];
	char *msg;
 	if (!trace_file)
		return;

	memset (frames, 0, sizeof (gpointer));
	mono_backtrace (frames, 6);

	/*We only dump 5 frames, which should be more than enough to most analysis.*/
	msg = g_strdup_printf ("%x,%d,%d,%p,%p,%p,%p,%p,%p\n", (guint32)GetCurrentThreadId (), record_kind, kind, lock, frames [1], frames [2], frames [3], frames [4], frames [5]);
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

#endif
