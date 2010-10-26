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
#include "mono/utils/mono-compiler.h"

G_BEGIN_DECLS

void mono_locks_dump (gboolean include_untaken);

void mono_monitor_init (void) MONO_INTERNAL;
void mono_monitor_cleanup (void) MONO_INTERNAL;

void** mono_monitor_get_object_monitor_weak_link (MonoObject *object) MONO_INTERNAL;

void mono_monitor_init_tls (void) MONO_INTERNAL;

MonoMethod* mono_monitor_get_fast_path (MonoMethod *enter_or_exit) MONO_INTERNAL;

void mono_monitor_threads_sync_members_offset (int *owner_offset, int *nest_offset, int *entry_count_offset) MONO_INTERNAL;
#define MONO_THREADS_SYNC_MEMBER_OFFSET(o)	((o)>>8)
#define MONO_THREADS_SYNC_MEMBER_SIZE(o)	((o)&0xff)

extern gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj, guint32 ms) MONO_INTERNAL;
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj) MONO_INTERNAL;
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj) MONO_INTERNAL;
extern void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj) MONO_INTERNAL;
extern void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj) MONO_INTERNAL;
extern gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj, guint32 ms) MONO_INTERNAL;
extern void ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var (MonoObject *obj, guint32 ms, char *lockTaken) MONO_INTERNAL;

G_END_DECLS

#endif /* _MONO_METADATA_MONITOR_H_ */
