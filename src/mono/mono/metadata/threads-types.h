/**
 * \file
 * Generic thread typedef support (includes system-specific files)
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

#include <mono/utils/mono-forward-internal.h>
#include <mono/metadata/object.h>
#include "mono/metadata/handle.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/mono-threads.h"
#include "mono/metadata/class-internals.h"
#include <mono/metadata/icalls.h>

/* This is a copy of System.Threading.ThreadState */
typedef enum {
	ThreadState_Running = 0x00000000,
	ThreadState_SuspendRequested = 0x00000002,
	ThreadState_Background = 0x00000004,
	ThreadState_Unstarted = 0x00000008,
	ThreadState_Stopped = 0x00000010,
	ThreadState_WaitSleepJoin = 0x00000020,
	ThreadState_Suspended = 0x00000040,
	ThreadState_AbortRequested = 0x00000080,
	ThreadState_Aborted = 0x00000100
} MonoThreadState;

G_ENUM_FUNCTIONS (MonoThreadState)

/* This is a copy of System.Threading.ApartmentState */
typedef enum {
	ThreadApartmentState_STA = 0x00000000,
	ThreadApartmentState_MTA = 0x00000001,
	ThreadApartmentState_Unknown = 0x00000002
} MonoThreadApartmentState;

typedef enum {
// These values match System.Threading.ThreadPriority.
// These values match System.Diagnostics.ThreadPriorityLevel and Windows, but are offset by 2.
	MONO_THREAD_PRIORITY_LOWEST       = 0,
	MONO_THREAD_PRIORITY_BELOW_NORMAL = 1,
	MONO_THREAD_PRIORITY_NORMAL       = 2,
	MONO_THREAD_PRIORITY_ABOVE_NORMAL = 3,
	MONO_THREAD_PRIORITY_HIGHEST      = 4,
} MonoThreadPriority;

#define SPECIAL_STATIC_NONE 0
#define SPECIAL_STATIC_THREAD 1

/* It's safe to access System.Threading.InternalThread from native code via a
 * raw pointer because all instances should be pinned.  But for uniformity of
 * icall wrapping, let's declare a MonoInternalThreadHandle anyway.
 */
TYPED_HANDLE_DECL (MonoInternalThread);

typedef void (*MonoThreadCleanupFunc) (MonoNativeThreadId tid);
/* INFO has type MonoThreadInfo* */
typedef void (*MonoThreadNotifyPendingExcFunc) (gpointer info);

void
mono_thread_callbacks_init (void);

typedef enum {
	MONO_THREAD_CREATE_FLAGS_NONE			= 0x00,
	MONO_THREAD_CREATE_FLAGS_THREADPOOL		= 0x01,
	MONO_THREAD_CREATE_FLAGS_DEBUGGER		= 0x02,
	MONO_THREAD_CREATE_FLAGS_FORCE_CREATE	= 0x04,
	MONO_THREAD_CREATE_FLAGS_SMALL_STACK	= 0x08,
	// "external eventloop" means the thread main function can return without killing the thread
	// and the thread will continue to be attached to the runtime and may invoke embedding APIs
	// and managed calls.  There is usually some platform-specific way to shut down the thread.
	MONO_THREAD_CREATE_FLAGS_EXTERNAL_EVENTLOOP = 0x10,
} MonoThreadCreateFlags;

MONO_COMPONENT_API MonoInternalThread*
mono_thread_create_internal (MonoThreadStart func, gpointer arg, MonoThreadCreateFlags flags, MonoError *error);

MonoInternalThreadHandle
mono_thread_create_internal_handle (MonoThreadStart func, gpointer arg, MonoThreadCreateFlags flags, MonoError *error);

void
mono_thread_manage_internal (void);

/* Data owned by a MonoInternalThread that must live until both the finalizer
 * for MonoInternalThread has run, and the underlying machine thread has
 * detached.
 *
 * Normally a thread is first detached and then the InternalThread object is
 * finalized and collected.  However during shutdown, when the root domain is
 * finalized, all the InternalThread objects are finalized first and the
 * machine threads are detached later.
 */
typedef struct {
  MonoRefCount ref;
  MonoCoopMutex *synch_cs;
} MonoLongLivedThreadData;

void mono_threads_install_cleanup (MonoThreadCleanupFunc func);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_SetName_internal (MonoInternalThread *this_obj, MonoString *name);

ICALL_EXPORT
MonoObject* ves_icall_System_Threading_Thread_GetCachedCurrentCulture (MonoInternalThread *this_obj);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_SetCachedCurrentCulture (MonoThread *this_obj, MonoObject *culture);

ICALL_EXPORT
MonoObject* ves_icall_System_Threading_Thread_GetCachedCurrentUICulture (MonoInternalThread *this_obj);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_SetCachedCurrentUICulture (MonoThread *this_obj, MonoObject *culture);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_Increment_Int(gint32 *location);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Increment_Long(gint64 *location);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_Decrement_Int(gint32 *location);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Decrement_Long(gint64 * location);

ICALL_EXPORT
guint8 ves_icall_System_Threading_Interlocked_Exchange_Byte(guint8 *location, guint8 value);

ICALL_EXPORT
gint16 ves_icall_System_Threading_Interlocked_Exchange_Short(gint16 *location, gint16 value);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_Exchange_Int(gint32 *location, gint32 value);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Exchange_Long(gint64 *location, gint64 value);

ICALL_EXPORT
void ves_icall_System_Threading_Interlocked_Exchange_Object (MonoObject *volatile*location, MonoObject *volatile*value, MonoObject *volatile*res);

ICALL_EXPORT
guint8 ves_icall_System_Threading_Interlocked_CompareExchange_Byte(guint8 *location, guint8 value, guint8 comparand);

ICALL_EXPORT
gint16 ves_icall_System_Threading_Interlocked_CompareExchange_Short(gint16 *location, gint16 value, gint16 comparand);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location, gint32 value, gint32 comparand);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int_Success(gint32 *location, gint32 value, gint32 comparand, MonoBoolean *success);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_CompareExchange_Long(gint64 *location, gint64 value, gint64 comparand);

ICALL_EXPORT
void ves_icall_System_Threading_Interlocked_CompareExchange_Object (MonoObject *volatile*location, MonoObject *volatile*value, MonoObject *volatile*comparand, MonoObject *volatile*res);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_Add_Int(gint32 *location, gint32 value);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Add_Long(gint64 *location, gint64 value);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Read_Long(gint64 *location);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_Increment_Int(gint32 *location);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Increment_Long(gint64 *location);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_Decrement_Int(gint32 *location);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Decrement_Long(gint64 * location);

ICALL_EXPORT
void ves_icall_System_Threading_Interlocked_MemoryBarrierProcessWide (void);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_MemoryBarrier (void);

MONO_PROFILER_API MonoInternalThread *mono_thread_internal_current (void);

MonoInternalThreadHandle
mono_thread_internal_current_handle (void);

MONO_COMPONENT_API gboolean
mono_thread_internal_abort (MonoInternalThread *thread);
void mono_thread_internal_suspend_for_shutdown (MonoInternalThread *thread);

MONO_COMPONENT_API void mono_thread_internal_reset_abort (MonoInternalThread *thread);

void mono_thread_internal_unhandled_exception (MonoObject* exc);

void mono_alloc_special_static_data_free (GHashTable *special_static_fields);

MONO_COMPONENT_API void mono_thread_set_state (MonoInternalThread *thread, MonoThreadState state);
void mono_thread_clr_state (MonoInternalThread *thread, MonoThreadState state);
gboolean mono_thread_test_state (MonoInternalThread *thread, MonoThreadState test);
gboolean mono_thread_test_and_set_state (MonoInternalThread *thread, MonoThreadState test, MonoThreadState set);
void mono_thread_clear_and_set_state (MonoInternalThread *thread, MonoThreadState clear, MonoThreadState set);

void mono_thread_init_apartment_state (void);
void mono_thread_cleanup_apartment_state (void);

/* There are some threads that need initialization that would normally
	occur in managed code. Some threads occur prior to the runtime being
	fully initialized so that must be done in native. For example, Main and Finalizer. */
void mono_thread_init_from_native (void);
void mono_thread_cleanup_from_native (void);

void mono_threads_set_shutting_down (void);

MONO_API MonoException* mono_thread_get_undeniable_exception (void);

ICALL_EXPORT
void ves_icall_thread_finish_async_abort (void);

#if HOST_WIN32

void
mono_thread_set_name_windows (HANDLE thread_handle, PCWSTR thread_name);

#define MONO_THREAD_NAME_WINDOWS_CONSTANT(x) L ## x

#else

#define mono_thread_set_name_windows(thread_handle, thread_name) /* nothing */

#define MONO_THREAD_NAME_WINDOWS_CONSTANT(x) NULL

#endif

typedef enum {
    MonoSetThreadNameFlag_None      = 0x0000,
    MonoSetThreadNameFlag_Permanent = 0x0001,
    MonoSetThreadNameFlag_Reset     = 0x0002,
    MonoSetThreadNameFlag_Constant  = 0x0004,
    MonoSetThreadNameFlag_RepeatedlyButOptimized = 0x0008,
} MonoSetThreadNameFlags;

G_ENUM_FUNCTIONS (MonoSetThreadNameFlags)

MONO_PROFILER_API
void
mono_thread_set_name (MonoInternalThread *thread,
		      const char* name8, size_t name8_length, const gunichar2* name16,
		      MonoSetThreadNameFlags flags, MonoError *error);

#define mono_thread_set_name_constant_ignore_error(thread, name, flags) \
	mono_thread_set_name ((thread), name, STRING_LENGTH (name),  \
		MONO_THREAD_NAME_WINDOWS_CONSTANT (name),               \
		(flags) | MonoSetThreadNameFlag_Constant, NULL)

gboolean mono_thread_interruption_requested (void);

ICALL_EXPORT
MonoException*
mono_thread_interruption_checkpoint (void);

gboolean
mono_thread_interruption_checkpoint_bool (void);

void
mono_thread_interruption_checkpoint_void (void);

MonoExceptionHandle
mono_thread_interruption_checkpoint_handle (void);

ICALL_EXPORT
MonoException* mono_thread_force_interruption_checkpoint_noraise (void);

/**
 * mono_thread_interruption_request_flag:
 *
 * A flag that will be non-zero if an interruption has
 * been requested for a thread. The thread to interrupt may not be the current
 * thread, so an additional call to mono_thread_interruption_requested () or
 * mono_thread_interruption_checkpoint () is always needed if the flag is not
 * zero.
 */
MONO_API_DATA gint32 mono_thread_interruption_request_flag;

uint32_t mono_alloc_special_static_data (uint32_t static_type, uint32_t size, uint32_t align, uintptr_t *bitmap, int numbits);

ICALL_EXPORT
void*    mono_get_special_static_data   (uint32_t offset);

MONO_COMPONENT_API gpointer mono_get_special_static_data_for_thread (MonoInternalThread *thread, guint32 offset);

void
mono_thread_resume_interruption (gboolean exec);
void mono_threads_perform_thread_dump (void);

gboolean
mono_thread_create_checked (MonoThreadStart func, gpointer arg, MonoError *error);

void mono_threads_add_joinable_runtime_thread (MonoThreadInfo *thread_info);
void mono_threads_add_joinable_thread (gpointer tid);
void mono_threads_join_threads (void);
void mono_thread_join (gpointer tid);

MONO_PROFILER_API MonoThread*
mono_thread_internal_attach (MonoDomain *domain);

MONO_PROFILER_API void
mono_thread_internal_detach (MonoThread *thread);

MonoThread *
mono_thread_attach_external_native_thread (MonoDomain *domain, gboolean background);


MONO_API gpointer
mono_threads_attach_coop (MonoDomain *domain, gpointer *dummy);

MONO_API void
mono_threads_detach_coop (gpointer cookie, gpointer *dummy);

void
mono_threads_attach_coop_internal (gpointer *cookie, MonoStackData *stackdata);

void
mono_threads_detach_coop_internal (gpointer cookie, MonoStackData *stackdata);

void mono_threads_begin_abort_protected_block (void);
gboolean mono_threads_end_abort_protected_block (void);

gboolean
mono_thread_internal_current_is_attached (void);

void
mono_thread_internal_describe (MonoInternalThread *internal, GString *str);

MONO_COMPONENT_API gboolean
mono_thread_internal_is_current (MonoInternalThread *internal);

gboolean
mono_threads_is_current_thread_in_protected_block (void);

gboolean
mono_threads_is_critical_method (MonoMethod *method);

gpointer
mono_threads_enter_gc_unsafe_region_unbalanced_internal (MonoStackData *stackdata);

void
mono_threads_exit_gc_unsafe_region_unbalanced_internal (gpointer cookie, MonoStackData *stackdata);

gpointer
mono_threads_enter_gc_safe_region_unbalanced_internal (MonoStackData *stackdata);

void
mono_threads_exit_gc_safe_region_unbalanced_internal (gpointer cookie, MonoStackData *stackdata);

// Set directory to store thread dumps captured by SIGQUIT
void
mono_set_thread_dump_dir(gchar* dir);

MONO_COLD void
mono_set_pending_exception_handle (MonoExceptionHandle exc);

void
mono_threads_exiting (void);

#if SIZEOF_VOID_P == 4
/* Spin lock for unaligned InterlockedXXX 64 bit functions on 32bit platforms. */
extern mono_mutex_t mono_interlocked_mutex;
static inline void
mono_interlocked_lock(void) {
	mono_os_mutex_lock (&mono_interlocked_mutex);
}
static inline void
mono_interlocked_unlock(void) {
	mono_os_mutex_unlock (&mono_interlocked_mutex);
}
#endif

#endif /* _MONO_METADATA_THREADS_TYPES_H_ */
