/*
 * monitor.h: Monitor locking functions
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
#include <mono/io-layer/io-layer.h>
#include "mono/utils/mono-compiler.h"

G_BEGIN_DECLS

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
	HANDLE entry_sem;
	GSList *wait_list;
	void *data;
};


MONO_API void mono_locks_dump (gboolean include_untaken);

void mono_monitor_init (void);
void mono_monitor_cleanup (void);

void** mono_monitor_get_object_monitor_weak_link (MonoObject *object);

void mono_monitor_threads_sync_members_offset (int *status_offset, int *nest_offset);
#define MONO_THREADS_SYNC_MEMBER_OFFSET(o)	((o)>>8)
#define MONO_THREADS_SYNC_MEMBER_SIZE(o)	((o)&0xff)

extern gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj, guint32 ms);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj, guint32 ms);
extern void ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var (MonoObject *obj, guint32 ms, char *lockTaken);

G_END_DECLS

#endif /* _MONO_METADATA_MONITOR_H_ */
