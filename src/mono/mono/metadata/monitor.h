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

G_BEGIN_DECLS

void mono_monitor_init (void);
void mono_monitor_cleanup (void);

extern gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj, guint32 ms);
extern void ves_icall_System_Threading_Monitor_Monitor_exit(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj, guint32 ms);

G_END_DECLS

#endif /* _MONO_METADATA_MONITOR_H_ */
