// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       mytime.c
/// \brief      Time handling functions
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "private.h"

#if defined(MYTHREAD_VISTA) || defined(_MSC_VER)
	// Nothing
#elif defined(HAVE_CLOCK_GETTIME) \
		&& (!defined(__MINGW32__) || defined(MYTHREAD_POSIX))
#	include <time.h>
#else
#	include <sys/time.h>
#endif

uint64_t opt_flush_timeout = 0;

// start_time holds the time when the (de)compression was started.
// It's from mytime_now() and thus only useful for calculating relative
// time differences (elapsed time). start_time is initialized by calling
// mytime_set_start_time() and modified by mytime_sigtstp_handler().
//
// When mytime_sigtstp_handler() is used, start_time is made volatile.
// I'm not sure if that is really required since access to it is guarded
// by signals_block()/signals_unblock() since accessing an uint64_t isn't
// atomic on all systems. But since the variable isn't accessed very
// frequently making it volatile doesn't hurt.
#ifdef USE_SIGTSTP_HANDLER
static volatile uint64_t start_time;
#else
static uint64_t start_time;
#endif

static uint64_t next_flush;


/// \brief      Get the current time as milliseconds
///
/// It's relative to some point but not necessarily to the UNIX Epoch.
static uint64_t
mytime_now(void)
{
#if defined(MYTHREAD_VISTA) || defined(_MSC_VER)
	// Since there is no SIGALRM on Windows, this function gets
	// called frequently when the progress indicator is in use.
	// Progress indicator doesn't need high-resolution time.
	// GetTickCount64() has very low overhead but needs at least WinVista.
	//
	// MinGW-w64 provides the POSIX functions clock_gettime() and
	// gettimeofday() in a manner that allow xz to run on older
	// than WinVista. If the threading method needs WinVista anyway,
	// there's no reason to avoid a WinVista API here either.
	return GetTickCount64();

#elif defined(HAVE_CLOCK_GETTIME) \
		&& (!defined(__MINGW32__) || defined(MYTHREAD_POSIX))
	// MinGW-w64: clock_gettime() is defined in winpthreads but we need
	// nothing else from winpthreads (unless, for some odd reason, POSIX
	// threading has been selected). By avoiding clock_gettime(), we
	// avoid the dependency on libwinpthread-1.dll or the need to link
	// against the static version. The downside is that the fallback
	// method, gettimeofday(), doesn't provide monotonic time.
	struct timespec tv;

#	ifdef HAVE_CLOCK_MONOTONIC
	// If CLOCK_MONOTONIC was available at compile time but for some
	// reason isn't at runtime, fallback to CLOCK_REALTIME which
	// according to POSIX is mandatory for all implementations.
	static clockid_t clk_id = CLOCK_MONOTONIC;
	while (clock_gettime(clk_id, &tv))
		clk_id = CLOCK_REALTIME;
#	else
	clock_gettime(CLOCK_REALTIME, &tv);
#	endif

	return (uint64_t)tv.tv_sec * 1000 + (uint64_t)(tv.tv_nsec / 1000000);

#else
	struct timeval tv;
	gettimeofday(&tv, NULL);
	return (uint64_t)tv.tv_sec * 1000 + (uint64_t)(tv.tv_usec / 1000);
#endif
}


#ifdef USE_SIGTSTP_HANDLER
extern void
mytime_sigtstp_handler(int sig lzma_attribute((__unused__)))
{
	// Measure how long the process stays in the stopped state and add
	// that amount to start_time. This way the progress indicator
	// won't count the stopped time as elapsed time and the estimated
	// remaining time won't be confused by the time spent in the
	// stopped state.
	//
	// FIXME? Is raising SIGSTOP the correct thing to do? POSIX.1-2017
	// says that orphan processes shouldn't stop on SIGTSTP. So perhaps
	// the most correct thing to do could be to revert to the default
	// handler for SIGTSTP, unblock SIGTSTP, and then raise(SIGTSTP).
	// It's quite a bit more complicated than just raising SIGSTOP though.
	//
	// The difference between raising SIGTSTP vs. SIGSTOP can be seen on
	// the shell command line too by running "echo $?" after stopping
	// a process but perhaps that doesn't matter.
	const uint64_t t = mytime_now();
	raise(SIGSTOP);
	start_time += mytime_now() - t;
	return;
}
#endif


extern void
mytime_set_start_time(void)
{
#ifdef USE_SIGTSTP_HANDLER
	// Block the signals when accessing start_time so that we cannot
	// end up with a garbage value. start_time is volatile but access
	// to it isn't atomic at least on 32-bit systems.
	signals_block();
#endif

	start_time = mytime_now();

#ifdef USE_SIGTSTP_HANDLER
	signals_unblock();
#endif

	return;
}


extern uint64_t
mytime_get_elapsed(void)
{
#ifdef USE_SIGTSTP_HANDLER
	signals_block();
#endif

	const uint64_t t = mytime_now() - start_time;

#ifdef USE_SIGTSTP_HANDLER
	signals_unblock();
#endif

	return t;
}


extern void
mytime_set_flush_time(void)
{
	next_flush = mytime_now() + opt_flush_timeout;
	return;
}


extern int
mytime_get_flush_timeout(void)
{
	if (opt_flush_timeout == 0 || opt_mode != MODE_COMPRESS)
		return -1;

	const uint64_t now = mytime_now();
	if (now >= next_flush)
		return 0;

	const uint64_t remaining = next_flush - now;
	return remaining > INT_MAX ? INT_MAX : (int)remaining;
}
