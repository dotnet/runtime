/*
 * threads.h: Thread support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc
 */

#ifndef _MONO_METADATA_THREADS_H_
#define _MONO_METADATA_THREADS_H_

#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>

/* This is a copy of System.Threading.ThreadState */
typedef enum {
	ThreadState_Running = 0x00000000,
	ThreadState_StopRequested = 0x00000001,
	ThreadState_SuspendRequested = 0x00000002,
	ThreadState_Background = 0x00000004,
	ThreadState_Unstarted = 0x00000008,
	ThreadState_Stopped = 0x00000010,
	ThreadState_WaitSleepJoin = 0x00000020,
	ThreadState_Suspended = 0x00000040,
	ThreadState_AbortRequested = 0x00000080,
	ThreadState_Aborted = 0x00000100,
} MonoThreadState;


extern int  mono_thread_get_abort_signal (void);

extern void mono_thread_init (MonoThreadStartCB start_cb,
			      MonoThreadAttachCB attach_cb);
extern void mono_thread_manage(void);
extern void mono_thread_abort_all_other_threads (void);

extern MonoThread *mono_thread_current (void);

typedef struct {
	gpointer (* thread_start_compile_func) (MonoMethod *delegate);
	void (* start_resume) (guint32 tid);
	void (* end_resume) (guint32 tid);
} MonoThreadCallbacks;

extern void mono_install_thread_callbacks (MonoThreadCallbacks *callbacks);

extern void mono_thread_new_init (guint32 tid, gpointer stack_start,
				  gpointer func);
extern void mono_thread_create (MonoDomain *domain, gpointer func,
				       gpointer arg);
extern MonoThread *mono_thread_attach (MonoDomain *domain);

extern HANDLE ves_icall_System_Threading_Thread_Thread_internal(MonoThread *this, MonoObject *start);
extern void ves_icall_System_Threading_Thread_Thread_free_internal(MonoThread *this, HANDLE thread);
extern void ves_icall_System_Threading_Thread_Start_internal(MonoThread *this, HANDLE thread);
extern void ves_icall_System_Threading_Thread_Sleep_internal(int ms);
extern gboolean ves_icall_System_Threading_Thread_Join_internal(MonoThread *this, int ms, HANDLE thread);
extern gint32 ves_icall_System_Threading_Thread_GetDomainID (void);
extern void ves_icall_System_Threading_Thread_SlotHash_store(MonoObject *data);
extern MonoObject *ves_icall_System_Threading_Thread_SlotHash_lookup(void);
extern HANDLE ves_icall_System_Threading_Mutex_CreateMutex_internal(MonoBoolean owned,char *name);
extern void ves_icall_System_Threading_Mutex_ReleaseMutex_internal(HANDLE handle);
extern void ves_icall_System_Threading_Mutex_ReleaseMutex_internal (HANDLE handle );
extern HANDLE ves_icall_System_Threading_Events_CreateEvent_internal (MonoBoolean manual,MonoBoolean initial,char *name);
extern gboolean ves_icall_System_Threading_Events_SetEvent_internal (HANDLE handle);
extern gboolean ves_icall_System_Threading_Events_ResetEvent_internal (HANDLE handle);

extern gboolean ves_icall_System_Threading_WaitHandle_WaitAll_internal(MonoArray *mono_handles, gint32 ms, gboolean exitContext);
extern gint32 ves_icall_System_Threading_WaitHandle_WaitAny_internal(MonoArray *mono_handles, gint32 ms, gboolean exitContext);
extern gboolean ves_icall_System_Threading_WaitHandle_WaitOne_internal(MonoObject *this, HANDLE handle, gint32 ms, gboolean exitContext);

extern gint32 ves_icall_System_Threading_Interlocked_Increment_Int(gint32 *location);
extern gint64 ves_icall_System_Threading_Interlocked_Increment_Long(gint64 *location);
extern gint32 ves_icall_System_Threading_Interlocked_Decrement_Int(gint32 *location);
extern gint64 ves_icall_System_Threading_Interlocked_Decrement_Long(gint64 * location);

extern gint32 ves_icall_System_Threading_Interlocked_Exchange_Int(gint32 *location1, gint32 value);
extern MonoObject *ves_icall_System_Threading_Interlocked_Exchange_Object(MonoObject **location1, MonoObject *value);
extern gfloat ves_icall_System_Threading_Interlocked_Exchange_Single(gfloat *location1, gfloat value);

extern gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location1, gint32 value, gint32 comparand);
extern MonoObject *ves_icall_System_Threading_Interlocked_CompareExchange_Object(MonoObject **location1, MonoObject *value, MonoObject *comparand);
extern gfloat ves_icall_System_Threading_Interlocked_CompareExchange_Single(gfloat *location1, gfloat value, gfloat comparand);
extern void ves_icall_System_Threading_Thread_Abort (MonoThread *thread, MonoObject *state);
extern void ves_icall_System_Threading_Thread_ResetAbort (void);


#endif /* _MONO_METADATA_THREADS_H_ */
