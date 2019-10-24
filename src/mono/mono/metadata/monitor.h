/**
 * \file
 * Monitor locking functions
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2003 Ximian, Inc
 */

#ifndef _MONO_METADATA_MONITOR_H_
#define _MONO_METADATA_MONITOR_H_

#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/metadata/icalls.h>

#define OWNER_MASK		0x0000ffff
#define ENTRY_COUNT_MASK	0xffff0000
#define ENTRY_COUNT_WAITERS	0x80000000
#define ENTRY_COUNT_ZERO	0x7fff0000
#define ENTRY_COUNT_SHIFT	16

struct _MonoThreadsSync
{
	/*
	 * The entry count field can be negative, which would mean that the entry_sem is
	 * signaled and nobody is waiting to acquire it. This can happen when the thread
	 * that was waiting is either interrupted or timeouts, and the owner releases
	 * the lock before the forementioned thread updates the entry count.
	 *
	 * The 0 entry_count value is encoded as ENTRY_COUNT_ZERO, positive numbers being
	 * greater than it and negative numbers smaller than it.
	 */
	guint32 status;			/* entry_count (16) | owner_id (16) */
	guint32 nest;
#ifdef HAVE_MOVING_COLLECTOR
	gint32 hash_code;
#endif
	GSList *wait_list;
	void *data;
	MonoCoopMutex *entry_mutex;
	MonoCoopCond *entry_cond;
};

/*
 * Lock word format:
 *
 * The least significant bit stores whether a hash for the object is computed
 * which is stored either in the lock word or in the MonoThreadsSync structure
 * that the lock word points to.
 *
 * The second bit stores whether the lock word is inflated, containing an
 * address to the MonoThreadsSync structure.
 *
 * If both bits are 0, either the lock word is free (entire lock word is 0)
 * or it is a thin/flat lock.
 *
 * 32-bit
 *            LOCK_WORD_FLAT:    [owner:22 | nest:8 | status:2]
 *       LOCK_WORD_THIN_HASH:    [hash:30 | status:2]
 *        LOCK_WORD_INFLATED:    [sync:30 | status:2]
 *        LOCK_WORD_FAT_HASH:    [sync:30 | status:2]
 *
 * 64-bit
 *            LOCK_WORD_FLAT:    [unused:22 | owner:32 | nest:8 | status:2]
 *       LOCK_WORD_THIN_HASH:    [hash:62 | status:2]
 *        LOCK_WORD_INFLATED:    [sync:62 | status:2]
 *        LOCK_WORD_FAT_HASH:    [sync:62 | status:2]
 *
 * In order to save processing time and to have one additional value, the nest
 * count starts from 0 for the lock word (just valid thread ID in the lock word
 * means that the thread holds the lock once, although nest is 0).
 * FIXME Have the same convention on inflated locks
 */

typedef union {
	gsize lock_word;
	MonoThreadsSync *sync;
} LockWord;

enum {
	LOCK_WORD_FLAT = 0,
	LOCK_WORD_HAS_HASH = 1,
	LOCK_WORD_INFLATED = 2,

	LOCK_WORD_STATUS_BITS = 2,
	LOCK_WORD_NEST_BITS = 8,

	LOCK_WORD_STATUS_MASK = (1 << LOCK_WORD_STATUS_BITS) - 1,
	LOCK_WORD_NEST_MASK = ((1 << LOCK_WORD_NEST_BITS) - 1) << LOCK_WORD_STATUS_BITS,

	LOCK_WORD_HASH_SHIFT = LOCK_WORD_STATUS_BITS,
	LOCK_WORD_NEST_SHIFT = LOCK_WORD_STATUS_BITS,
	LOCK_WORD_OWNER_SHIFT = LOCK_WORD_STATUS_BITS + LOCK_WORD_NEST_BITS
};

MONO_API void
mono_locks_dump (gboolean include_untaken);

void
mono_monitor_init (void);

void
mono_monitor_cleanup (void);

ICALL_EXTERN_C
MonoBoolean
mono_monitor_enter_internal (MonoObject *obj);

ICALL_EXTERN_C
void
mono_monitor_enter_v4_internal (MonoObject *obj, MonoBoolean *lock_taken);

ICALL_EXTERN_C
guint32
mono_monitor_enter_fast (MonoObject *obj);

ICALL_EXTERN_C
guint32
mono_monitor_enter_v4_fast (MonoObject *obj, MonoBoolean *lock_taken);

guint32
mono_monitor_get_object_monitor_gchandle (MonoObject *object);

void
mono_monitor_threads_sync_members_offset (int *status_offset, int *nest_offset);
#define MONO_THREADS_SYNC_MEMBER_OFFSET(o)	((o)>>8)
#define MONO_THREADS_SYNC_MEMBER_SIZE(o)	((o)&0xff)

#if ENABLE_NETCORE
ICALL_EXPORT
gint64
ves_icall_System_Threading_Monitor_Monitor_LockContentionCount (void);
#endif

#endif /* _MONO_METADATA_MONITOR_H_ */
