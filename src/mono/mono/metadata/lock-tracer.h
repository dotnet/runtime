#ifndef __MONO_METADATA_LOCK_TRACER_H__
#define __MONO_METADATA_LOCK_TRACER_H__

/*This is a private header*/
#include <glib.h>

#include "mono/utils/mono-compiler.h"

G_BEGIN_DECLS

typedef enum {
	InvalidLock = 0,
	LoaderLock,
	ImageDataLock,
	DomainLock,
	DomainAssembliesLock,
	DomainJitCodeHashLock,
} RuntimeLocks;

#ifdef LOCK_TRACER

void mono_locks_tracer_init (void) MONO_INTERNAL;

void mono_locks_lock_acquired (RuntimeLocks kind, gpointer lock) MONO_INTERNAL;
void mono_locks_lock_released (RuntimeLocks kind, gpointer lock) MONO_INTERNAL;

#else

#define mono_locks_tracer_init() do {} while (0)

#define mono_locks_lock_acquired(__UNUSED0, __UNUSED1) do {} while (0)
#define mono_locks_lock_released(__UNUSED0, __UNUSED1) do {} while (0)

#endif

#define mono_locks_acquire(LOCK, NAME) do { \
	EnterCriticalSection (LOCK); \
	mono_locks_lock_acquired (NAME, LOCK); \
} while (0)

#define mono_locks_release(LOCK, NAME) do { \
	mono_locks_lock_released (NAME, LOCK); \
	LeaveCriticalSection (LOCK); \
} while (0)

G_END_DECLS

#endif /* __MONO_METADATA_LOCK_TRACER_H__ */
