/*
 * threads.h: Thread support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc
 */

#ifndef _MONO_METADATA_THREADS_H_
#define _MONO_METADATA_THREADS_H_

#include <config.h>

#include <mono/metadata/object.h>

extern void mono_thread_init(void);
extern void mono_thread_cleanup(void);

extern HANDLE ves_icall_System_Threading_Thread_Thread_internal(MonoObject *this, MonoObject *start);
extern void ves_icall_System_Threading_Thread_Start_internal(MonoObject *this, HANDLE thread);
extern void ves_icall_System_Threading_Thread_Sleep_internal(int ms);
extern MonoObject *ves_icall_System_Threading_Thread_CurrentThread_internal(void);
extern gboolean ves_icall_System_Threading_Thread_Join_internal(MonoObject *this, int ms, HANDLE thread);
extern void ves_icall_System_Threading_Thread_SlotHash_store(MonoObject *data);
extern MonoObject *ves_icall_System_Threading_Thread_SlotHash_lookup(void);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj, int ms);
extern void ves_icall_System_Threading_Monitor_Monitor_exit(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj, int ms);

extern gboolean ves_icall_System_Threading_WaitHandle_WaitAll_internal(MonoArray *handles, gint32 ms, gboolean exitContext);
extern gint32 ves_icall_System_Threading_WaitHandle_WaitAny_internal(MonoArray *handles, gint32 ms, gboolean exitContext);
extern gboolean ves_icall_System_Threading_WaitHandle_WaitOne_internal(MonoObject *this, WapiHandle *handle, gint32 ms, gboolean exitContext);


#endif /* _MONO_METADATA_THREADS_H_ */
