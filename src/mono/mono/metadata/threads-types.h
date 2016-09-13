/*
 * threads-types.h: Generic thread typedef support (includes
 * system-specific files)
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc
 * (C) Copyright 2002-2006 Novell, Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_THREADS_TYPES_H_
#define _MONO_METADATA_THREADS_TYPES_H_

#include <glib.h>

#include <mono/io-layer/io-layer.h>
#include <mono/metadata/object.h>
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-membar.h"
#include "mono/utils/mono-threads.h"

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
	ThreadState_Aborted = 0x00000100
} MonoThreadState; 

/* This is a copy of System.Threading.ApartmentState */
typedef enum {
	ThreadApartmentState_STA = 0x00000000,
	ThreadApartmentState_MTA = 0x00000001,
	ThreadApartmentState_Unknown = 0x00000002
} MonoThreadApartmentState;

typedef enum {
	MONO_THREAD_PRIORITY_LOWEST       = 0,
	MONO_THREAD_PRIORITY_BELOW_NORMAL = 1,
	MONO_THREAD_PRIORITY_NORMAL       = 2,
	MONO_THREAD_PRIORITY_ABOVE_NORMAL = 3,
	MONO_THREAD_PRIORITY_HIGHEST      = 4,
} MonoThreadPriority;

#define SPECIAL_STATIC_NONE 0
#define SPECIAL_STATIC_THREAD 1
#define SPECIAL_STATIC_CONTEXT 2

#ifdef HOST_WIN32
typedef SECURITY_ATTRIBUTES WapiSecurityAttributes;
#endif

typedef struct _MonoInternalThread MonoInternalThread;

typedef void (*MonoThreadCleanupFunc) (MonoNativeThreadId tid);
/* INFO has type MonoThreadInfo* */
typedef void (*MonoThreadNotifyPendingExcFunc) (gpointer info);

MonoInternalThread* mono_thread_create_internal (MonoDomain *domain, gpointer func, gpointer arg, gboolean threadpool_thread, guint32 stack_size, MonoError *error);

void mono_threads_install_cleanup (MonoThreadCleanupFunc func);

void ves_icall_System_Threading_Thread_ConstructInternalThread (MonoThread *this_obj);
HANDLE ves_icall_System_Threading_Thread_Thread_internal(MonoThread *this_obj, MonoObject *start);
void ves_icall_System_Threading_InternalThread_Thread_free_internal(MonoInternalThread *this_obj, HANDLE thread);
void ves_icall_System_Threading_Thread_Sleep_internal(gint32 ms);
gboolean ves_icall_System_Threading_Thread_Join_internal(MonoThread *this_obj, int ms);
gint32 ves_icall_System_Threading_Thread_GetDomainID (void);
gboolean ves_icall_System_Threading_Thread_Yield (void);
MonoString* ves_icall_System_Threading_Thread_GetName_internal (MonoInternalThread *this_obj);
void ves_icall_System_Threading_Thread_SetName_internal (MonoInternalThread *this_obj, MonoString *name);
int ves_icall_System_Threading_Thread_GetPriority (MonoThread *this_obj);
void ves_icall_System_Threading_Thread_SetPriority (MonoThread *this_obj, int priority);
MonoObject* ves_icall_System_Threading_Thread_GetCachedCurrentCulture (MonoInternalThread *this_obj);
void ves_icall_System_Threading_Thread_SetCachedCurrentCulture (MonoThread *this_obj, MonoObject *culture);
MonoObject* ves_icall_System_Threading_Thread_GetCachedCurrentUICulture (MonoInternalThread *this_obj);
void ves_icall_System_Threading_Thread_SetCachedCurrentUICulture (MonoThread *this_obj, MonoObject *culture);

gint32 ves_icall_System_Threading_WaitHandle_WaitAll_internal(MonoArray *mono_handles, gint32 ms);
gint32 ves_icall_System_Threading_WaitHandle_WaitAny_internal(MonoArray *mono_handles, gint32 ms);
gint32 ves_icall_System_Threading_WaitHandle_WaitOne_internal(HANDLE handle, gint32 ms);
gint32 ves_icall_System_Threading_WaitHandle_SignalAndWait_Internal (HANDLE toSignal, HANDLE toWait, gint32 ms);

MonoArray* ves_icall_System_Threading_Thread_ByteArrayToRootDomain (MonoArray *arr);
MonoArray* ves_icall_System_Threading_Thread_ByteArrayToCurrentDomain (MonoArray *arr);

gint32 ves_icall_System_Threading_Interlocked_Increment_Int(gint32 *location);
gint64 ves_icall_System_Threading_Interlocked_Increment_Long(gint64 *location);
gint32 ves_icall_System_Threading_Interlocked_Decrement_Int(gint32 *location);
gint64 ves_icall_System_Threading_Interlocked_Decrement_Long(gint64 * location);

gint32 ves_icall_System_Threading_Interlocked_Exchange_Int(gint32 *location, gint32 value);
gint64 ves_icall_System_Threading_Interlocked_Exchange_Long(gint64 *location, gint64 value);
MonoObject *ves_icall_System_Threading_Interlocked_Exchange_Object(MonoObject **location, MonoObject *value);
gpointer ves_icall_System_Threading_Interlocked_Exchange_IntPtr(gpointer *location, gpointer value);
gfloat ves_icall_System_Threading_Interlocked_Exchange_Single(gfloat *location, gfloat value);
gdouble ves_icall_System_Threading_Interlocked_Exchange_Double(gdouble *location, gdouble value);

gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location, gint32 value, gint32 comparand);
gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int_Success(gint32 *location, gint32 value, gint32 comparand, MonoBoolean *success);
gint64 ves_icall_System_Threading_Interlocked_CompareExchange_Long(gint64 *location, gint64 value, gint64 comparand);
MonoObject *ves_icall_System_Threading_Interlocked_CompareExchange_Object(MonoObject **location, MonoObject *value, MonoObject *comparand);
gpointer ves_icall_System_Threading_Interlocked_CompareExchange_IntPtr(gpointer *location, gpointer value, gpointer comparand);
gfloat ves_icall_System_Threading_Interlocked_CompareExchange_Single(gfloat *location, gfloat value, gfloat comparand);
gdouble ves_icall_System_Threading_Interlocked_CompareExchange_Double(gdouble *location, gdouble value, gdouble comparand);
MonoObject* ves_icall_System_Threading_Interlocked_CompareExchange_T(MonoObject **location, MonoObject *value, MonoObject *comparand);
MonoObject* ves_icall_System_Threading_Interlocked_Exchange_T(MonoObject **location, MonoObject *value);

gint32 ves_icall_System_Threading_Interlocked_Add_Int(gint32 *location, gint32 value);
gint64 ves_icall_System_Threading_Interlocked_Add_Long(gint64 *location, gint64 value);
gint64 ves_icall_System_Threading_Interlocked_Read_Long(gint64 *location);

gint32 ves_icall_System_Threading_Interlocked_Increment_Int(gint32 *location);
gint64 ves_icall_System_Threading_Interlocked_Increment_Long(gint64 *location);

gint32 ves_icall_System_Threading_Interlocked_Decrement_Int(gint32 *location);
gint64 ves_icall_System_Threading_Interlocked_Decrement_Long(gint64 * location);

void ves_icall_System_Threading_Thread_Abort (MonoInternalThread *thread, MonoObject *state);
void ves_icall_System_Threading_Thread_ResetAbort (MonoThread *this_obj);
MonoObject* ves_icall_System_Threading_Thread_GetAbortExceptionState (MonoThread *thread);
void ves_icall_System_Threading_Thread_Suspend (MonoThread *this_obj);
void ves_icall_System_Threading_Thread_Resume (MonoThread *thread);
void ves_icall_System_Threading_Thread_ClrState (MonoInternalThread *thread, guint32 state);
void ves_icall_System_Threading_Thread_SetState (MonoInternalThread *thread, guint32 state);
guint32 ves_icall_System_Threading_Thread_GetState (MonoInternalThread *thread);

gint8 ves_icall_System_Threading_Thread_VolatileRead1 (void *ptr);
gint16 ves_icall_System_Threading_Thread_VolatileRead2 (void *ptr);
gint32 ves_icall_System_Threading_Thread_VolatileRead4 (void *ptr);
gint64 ves_icall_System_Threading_Thread_VolatileRead8 (void *ptr);
void * ves_icall_System_Threading_Thread_VolatileReadIntPtr (void *ptr);
void * ves_icall_System_Threading_Thread_VolatileReadObject (void *ptr);
double ves_icall_System_Threading_Thread_VolatileReadDouble (void *ptr);
float ves_icall_System_Threading_Thread_VolatileReadFloat (void *ptr);

void ves_icall_System_Threading_Thread_VolatileWrite1 (void *ptr, gint8);
void ves_icall_System_Threading_Thread_VolatileWrite2 (void *ptr, gint16);
void ves_icall_System_Threading_Thread_VolatileWrite4 (void *ptr, gint32);
void ves_icall_System_Threading_Thread_VolatileWrite8 (void *ptr, gint64);
void ves_icall_System_Threading_Thread_VolatileWriteIntPtr (void *ptr, void *);
void ves_icall_System_Threading_Thread_VolatileWriteObject (void *ptr, MonoObject *);
void ves_icall_System_Threading_Thread_VolatileWriteFloat (void *ptr, float);
void ves_icall_System_Threading_Thread_VolatileWriteDouble (void *ptr, double);

gint8 ves_icall_System_Threading_Volatile_Read1 (void *ptr);
gint16 ves_icall_System_Threading_Volatile_Read2 (void *ptr);
gint32 ves_icall_System_Threading_Volatile_Read4 (void *ptr);
gint64 ves_icall_System_Threading_Volatile_Read8 (void *ptr);
void * ves_icall_System_Threading_Volatile_ReadIntPtr (void *ptr);
double ves_icall_System_Threading_Volatile_ReadDouble (void *ptr);
float ves_icall_System_Threading_Volatile_ReadFloat (void *ptr);
MonoObject* ves_icall_System_Threading_Volatile_Read_T (void *ptr);

void ves_icall_System_Threading_Volatile_Write1 (void *ptr, gint8);
void ves_icall_System_Threading_Volatile_Write2 (void *ptr, gint16);
void ves_icall_System_Threading_Volatile_Write4 (void *ptr, gint32);
void ves_icall_System_Threading_Volatile_Write8 (void *ptr, gint64);
void ves_icall_System_Threading_Volatile_WriteIntPtr (void *ptr, void *);
void ves_icall_System_Threading_Volatile_WriteFloat (void *ptr, float);
void ves_icall_System_Threading_Volatile_WriteDouble (void *ptr, double);
void ves_icall_System_Threading_Volatile_Write_T (void *ptr, MonoObject *value);

void ves_icall_System_Threading_Thread_MemoryBarrier (void);
void ves_icall_System_Threading_Thread_Interrupt_internal (MonoThread *this_obj);
void ves_icall_System_Threading_Thread_SpinWait_nop (void);

void ves_icall_System_Runtime_Remoting_Contexts_Context_RegisterContext (MonoAppContext *ctx);
void ves_icall_System_Runtime_Remoting_Contexts_Context_ReleaseContext (MonoAppContext *ctx);

MonoInternalThread *mono_thread_internal_current (void);

void mono_thread_internal_check_for_interruption_critical (MonoInternalThread *thread);

void mono_thread_internal_stop (MonoInternalThread *thread);
void mono_thread_internal_abort (MonoInternalThread *thread);

gboolean mono_thread_internal_has_appdomain_ref (MonoInternalThread *thread, MonoDomain *domain);

void mono_thread_internal_reset_abort (MonoInternalThread *thread);

void mono_thread_internal_unhandled_exception (MonoObject* exc);

void mono_alloc_special_static_data_free (GHashTable *special_static_fields);
gboolean mono_thread_current_check_pending_interrupt (void);

void mono_thread_set_state (MonoInternalThread *thread, MonoThreadState state);
void mono_thread_clr_state (MonoInternalThread *thread, MonoThreadState state);
gboolean mono_thread_test_state (MonoInternalThread *thread, MonoThreadState test);
gboolean mono_thread_test_and_set_state (MonoInternalThread *thread, MonoThreadState test, MonoThreadState set);

void mono_thread_init_apartment_state (void);
void mono_thread_cleanup_apartment_state (void);

void mono_threads_set_shutting_down (void);

gunichar2* mono_thread_get_name (MonoInternalThread *this_obj, guint32 *name_len);

MONO_API MonoException* mono_thread_get_undeniable_exception (void);

void mono_thread_set_name_internal (MonoInternalThread *this_obj, MonoString *name, gboolean permanent, MonoError *error);

void mono_runtime_set_has_tls_get (gboolean val);
gboolean mono_runtime_has_tls_get (void);

void mono_thread_suspend_all_other_threads (void);
gboolean mono_threads_abort_appdomain_threads (MonoDomain *domain, int timeout);

void mono_thread_push_appdomain_ref (MonoDomain *domain);
void mono_thread_pop_appdomain_ref (void);
gboolean mono_thread_has_appdomain_ref (MonoThread *thread, MonoDomain *domain);

void mono_threads_clear_cached_culture (MonoDomain *domain);

MonoException* mono_thread_request_interruption (mono_bool running_managed);
gboolean mono_thread_interruption_requested (void);
MonoException* mono_thread_interruption_checkpoint (void);
MonoException* mono_thread_force_interruption_checkpoint_noraise (void);
gint32* mono_thread_interruption_request_flag (void);

uint32_t mono_alloc_special_static_data (uint32_t static_type, uint32_t size, uint32_t align, uintptr_t *bitmap, int numbits);
void*    mono_get_special_static_data   (uint32_t offset);
gpointer mono_get_special_static_data_for_thread (MonoInternalThread *thread, guint32 offset);

MonoException* mono_thread_resume_interruption (void);
void mono_threads_perform_thread_dump (void);

gboolean
mono_thread_create_checked (MonoDomain *domain, gpointer func, gpointer arg, MonoError *error);

MonoThread *
mono_thread_attach_full (MonoDomain *domain, gboolean force_attach);

void mono_thread_init_tls (void);

/* Can't include utils/mono-threads.h because of the THREAD_INFO_TYPE wizardry */
void mono_threads_add_joinable_thread (gpointer tid);
void mono_threads_join_threads (void);
void mono_thread_join (gpointer tid);

void mono_thread_detach_internal (MonoInternalThread *thread);

void ves_icall_System_Threading_Thread_GetStackTraces (MonoArray **out_threads, MonoArray **out_stack_traces);

MONO_API gpointer
mono_threads_attach_coop (MonoDomain *domain, gpointer *dummy);

MONO_API void
mono_threads_detach_coop (gpointer cookie, gpointer *dummy);

void mono_threads_begin_abort_protected_block (void);
void mono_threads_end_abort_protected_block (void);
MonoException* mono_thread_try_resume_interruption (void);

#endif /* _MONO_METADATA_THREADS_TYPES_H_ */
