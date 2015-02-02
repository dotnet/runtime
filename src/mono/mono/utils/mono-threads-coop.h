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

#ifdef USE_COOP_GC

/* Runtime consumable API */

#define MONO_SUSPEND_CHECK() do {	\
	if (G_UNLIKELY (mono_threads_polling_required)) mono_threads_state_poll ();	\
} while (0);

#define MONO_PREPARE_BLOCKING	\
{	\
	void *__blocking_cookie = mono_threads_prepare_blocking ();

#define MONO_FINISH_BLOCKING \
	mono_threads_finish_blocking (__blocking_cookie);	\
}

#define MONO_PREPARE_RESET_BLOCKING	\
{	\
	void *__reset_cookie = mono_threads_reset_blocking_start ();

#define MONO_FINISH_RESET_BLOCKING \
	mono_threads_reset_blocking_end (__reset_cookie);	\
}
/* Internal API */

extern volatile size_t mono_threads_polling_required;

void mono_threads_state_poll (void);
void* mono_threads_prepare_blocking (void);
void mono_threads_finish_blocking (void* cookie);

void* mono_threads_reset_blocking_start (void);
void mono_threads_reset_blocking_end (void* cookie);

#else

#define MONO_SUSPEND_CHECK do {	} while (0);
#define MONO_PREPARE_BLOCKING {
#define MONO_FINISH_BLOCKING }
#define MONO_PREPARE_RESET_BLOCKING {
#define MONO_FINISH_RESET_BLOCKING }

#endif /* USE_COOP_GC */


#endif
