/**
 * \file
 */

#ifndef __MONO_COOP_MUTEX_H__
#define __MONO_COOP_MUTEX_H__

#include <config.h>
#include <glib.h>

#include "mono-os-mutex.h"
#include "mono-threads-api.h"

/* We put the OS sync primitives in struct, so the compiler will warn us if
 * we use mono_os_(mutex|cond|sem)_... on MonoCoop(Mutex|Cond|Sem) structures */

typedef struct _MonoCoopMutex MonoCoopMutex;
typedef struct _MonoCoopCond MonoCoopCond;

#ifndef DISABLE_THREADS

struct _MonoCoopMutex {
	mono_mutex_t m;
};

struct _MonoCoopCond {
	mono_cond_t c;
};

static inline void
mono_coop_mutex_init (MonoCoopMutex *mutex)
{
	mono_os_mutex_init (&mutex->m);
}

static inline void
mono_coop_mutex_init_recursive (MonoCoopMutex *mutex)
{
	mono_os_mutex_init_recursive (&mutex->m);
}

static inline void
mono_coop_mutex_destroy (MonoCoopMutex *mutex)
{
	mono_os_mutex_destroy (&mutex->m);
}

static inline void
mono_coop_mutex_lock (MonoCoopMutex *mutex)
{
	/* Avoid thread state switch if lock is not contended */
	if (mono_os_mutex_trylock (&mutex->m) == 0)
		return;

	MONO_ENTER_GC_SAFE;

	mono_os_mutex_lock (&mutex->m);

	MONO_EXIT_GC_SAFE;
}

static inline gint
mono_coop_mutex_trylock (MonoCoopMutex *mutex)
{
	return mono_os_mutex_trylock (&mutex->m);
}

static inline void
mono_coop_mutex_unlock (MonoCoopMutex *mutex)
{
	mono_os_mutex_unlock (&mutex->m);
}

static inline void
mono_coop_cond_init (MonoCoopCond *cond)
{
	mono_os_cond_init (&cond->c);
}

static inline void
mono_coop_cond_destroy (MonoCoopCond *cond)
{
	mono_os_cond_destroy (&cond->c);
}

static inline void
mono_coop_cond_wait (MonoCoopCond *cond, MonoCoopMutex *mutex)
{
	MONO_ENTER_GC_SAFE;

	mono_os_cond_wait (&cond->c, &mutex->m);

	MONO_EXIT_GC_SAFE;
}

static inline gint
mono_coop_cond_timedwait (MonoCoopCond *cond, MonoCoopMutex *mutex, guint32 timeout_ms)
{
	gint res;

	MONO_ENTER_GC_SAFE;

	res = mono_os_cond_timedwait (&cond->c, &mutex->m, timeout_ms);

	MONO_EXIT_GC_SAFE;

	return res;
}

static inline void
mono_coop_cond_signal (MonoCoopCond *cond)
{
	/*
	 * On glibc using NTPL (ie Linux with an underlying futex), signaling a
	 * condition variable can block in the __condvar_quiesce_and_switch_g1
	 * operation. So switch to GC Safe mode here.
	 */
	MONO_ENTER_GC_SAFE;
	mono_os_cond_signal (&cond->c);
	MONO_EXIT_GC_SAFE;
}

static inline void
mono_coop_cond_broadcast (MonoCoopCond *cond)
{
	/*
	 * On glibc using NTPL (ie Linux with an underlying futex), signaling a
	 * condition variable can block in the __condvar_quiesce_and_switch_g1
	 * operation. So switch to GC Safe mode here.
	 */
	MONO_ENTER_GC_SAFE;
	mono_os_cond_broadcast (&cond->c);
	MONO_EXIT_GC_SAFE;
}

#else /* DISABLE_THREADS */

struct _MonoCoopMutex {
	int dummy;
};

struct _MonoCoopCond {
	int dummy;
};

static inline void
mono_coop_mutex_init (MonoCoopMutex *mutex)
{
}

static inline void
mono_coop_mutex_init_recursive (MonoCoopMutex *mutex)
{
}

static inline void
mono_coop_mutex_destroy (MonoCoopMutex *mutex)
{
}

static inline void
mono_coop_mutex_lock (MonoCoopMutex *mutex)
{
}

static inline gint
mono_coop_mutex_trylock (MonoCoopMutex *mutex)
{
	return 0;
}

static inline void
mono_coop_mutex_unlock (MonoCoopMutex *mutex)
{
}

static inline void
mono_coop_cond_init (MonoCoopCond *cond)
{
}

static inline void
mono_coop_cond_destroy (MonoCoopCond *cond)
{
}

static inline void
mono_coop_cond_wait (MonoCoopCond *cond, MonoCoopMutex *mutex)
{
}

static inline gint
mono_coop_cond_timedwait (MonoCoopCond *cond, MonoCoopMutex *mutex, guint32 timeout_ms)
{
	return 0;
}

static inline void
mono_coop_cond_signal (MonoCoopCond *cond)
{
}

static inline void
mono_coop_cond_broadcast (MonoCoopCond *cond)
{
}

#endif /* DISABLE_THREADS */

#endif /* __MONO_COOP_MUTEX_H__ */
