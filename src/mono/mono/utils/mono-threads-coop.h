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
	if (G_UNLIKELY (mono_polling_required)) mono_threads_state_poll ();	\
} while (0);

#define MONO_PREPARE_BLOCKING	\
{	\
	void *__dummy;	\
	void *__blocking_cookie = mono_threads_prepare_blocking (&__dummy);

#define MONO_FINISH_BLOCKING \
	mono_threads_finish_blocking (__blocking_cookie, &__dummy);	\
}

#define MONO_PREPARE_RESET_BLOCKING	\
{	\
	void *__dummy;	\
	void *__reset_cookie = mono_threads_reset_blocking_start (&__dummy);

#define MONO_FINISH_RESET_BLOCKING \
	mono_threads_reset_blocking_end (__reset_cookie, &__dummy);	\
}

#define MONO_TRY_BLOCKING	\
{	\
	void *__dummy;	\
	void *__try_block_cookie = mono_threads_try_prepare_blocking (&__dummy);

#define MONO_FINISH_TRY_BLOCKING \
	mono_threads_finish_try_blocking (__try_block_cookie, &__dummy);	\
}

/* Internal API */

void mono_threads_state_poll (void);
void mono_threads_state_poll_stack_data (void* stackdata);

void* mono_threads_prepare_blocking (void* stackdata);
void mono_threads_finish_blocking (void* cookie, void* stackdata);

void* mono_threads_reset_blocking_start (void* stackdata);
void mono_threads_reset_blocking_end (void* cookie, void* stackdata);

void* mono_threads_try_prepare_blocking (void* stackdata);
void mono_threads_finish_try_blocking (void* cookie, void* stackdata);

/* JIT specific interface */
extern volatile size_t mono_polling_required;

#else

#define MONO_SUSPEND_CHECK() do {	} while (0);
#define MONO_PREPARE_BLOCKING {
#define MONO_FINISH_BLOCKING }
#define MONO_PREPARE_RESET_BLOCKING {
#define MONO_FINISH_RESET_BLOCKING }
#define MONO_TRY_BLOCKING {
#define MONO_FINISH_TRY_BLOCKING }

#endif /* USE_COOP_GC */


#endif
