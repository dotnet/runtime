/*
 * threads-pthread.h: System-specific thread support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_THREADS_PTHREAD_H_
#define _MONO_METADATA_THREADS_PTHREAD_H_

#include <pthread.h>

#include <mono/metadata/object.h>

extern pthread_t ves_icall_System_Threading_Thread_Thread_internal(MonoObject *this, MonoObject *start);
extern void ves_icall_System_Threading_Thread_Start_internal(MonoObject *this, pthread_t tid);
extern int ves_icall_System_Threading_Thread_Sleep_internal(int ms);
extern void ves_icall_System_Threading_Thread_Schedule_internal(void);
extern MonoObject *ves_icall_System_Threading_Thread_CurrentThread_internal(void);
extern gboolean ves_icall_System_Threading_Thread_Join_internal(MonoObject *this, int ms, pthread_t tid);
extern void ves_icall_System_Threading_Thread_DataSlot_register(MonoObject *slot);
extern void ves_icall_System_Threading_Thread_DataSlot_store(MonoObject *slot, MonoObject *data);
extern MonoObject *ves_icall_System_Threading_Thread_DataSlot_retrieve(MonoObject *slot);
extern void ves_icall_System_LocalDataStoreSlot_DataSlot_unregister(MonoObject *this);
extern void ves_icall_System_Threading_Monitor_Monitor_enter(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_exit(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_owner(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_test_synchronised(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse(MonoObject *obj);
extern void ves_icall_System_Threading_Monitor_Monitor_pulse_all(MonoObject *obj);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_try_enter(MonoObject *obj, int ms);
extern gboolean ves_icall_System_Threading_Monitor_Monitor_wait(MonoObject *obj, int ms);


#endif /* _MONO_METADATA_THREADS_PTHREAD_H_ */
