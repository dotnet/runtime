
#ifndef __MONO_COOP_SEMAPHORE_H__
#define __MONO_COOP_SEMAPHORE_H__

#include <config.h>
#include <glib.h>

#include "mono-os-semaphore.h"
#include "mono-threads.h"

G_BEGIN_DECLS

/* We put the OS sync primitives in struct, so the compiler will warn us if
 * we use mono_os_(mutex|cond|sem)_... on MonoCoop(Mutex|Cond|Sem) structures */

typedef struct _MonoCoopSem MonoCoopSem;
struct _MonoCoopSem {
	MonoSemType s;
};

static inline gint
mono_coop_sem_init (MonoCoopSem *sem, int value)
{
	return mono_os_sem_init (&sem->s, value);
}

static inline gint
mono_coop_sem_destroy (MonoCoopSem *sem)
{
	return mono_os_sem_destroy (&sem->s);
}

static inline gint
mono_coop_sem_wait (MonoCoopSem *sem, MonoSemFlags flags)
{
	gint res;

	MONO_PREPARE_BLOCKING;

	res = mono_os_sem_wait (&sem->s, flags);

	MONO_FINISH_BLOCKING;

	return res;
}

static inline gint
mono_coop_sem_timedwait (MonoCoopSem *sem, guint timeout_ms, MonoSemFlags flags)
{
	gint res;

	MONO_PREPARE_BLOCKING;

	res = mono_os_sem_timedwait (&sem->s, timeout_ms, flags);

	MONO_FINISH_BLOCKING;

	return res;
}

static inline gint
mono_coop_sem_post (MonoCoopSem *sem)
{
	return mono_os_sem_post (&sem->s);
}

G_END_DECLS

#endif /* __MONO_COOP_SEMAPHORE_H__ */
