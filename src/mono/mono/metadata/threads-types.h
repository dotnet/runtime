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
#include "mono/utils/mono-membar.h"
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
#define SPECIAL_STATIC_CONTEXT 2

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
	MONO_THREAD_CREATE_FLAGS_NONE         = 0x0,
	MONO_THREAD_CREATE_FLAGS_THREADPOOL   = 0x1,
	MONO_THREAD_CREATE_FLAGS_DEBUGGER     = 0x2,
	MONO_THREAD_CREATE_FLAGS_FORCE_CREATE = 0x4,
	MONO_THREAD_CREATE_FLAGS_SMALL_STACK  = 0x8,
} MonoThreadCreateFlags;

// FIXME func should be MonoThreadStart and remove the template
MonoInternalThread*
mono_thread_create_internal (MonoDomain *domain, gpointer func, gpointer arg, MonoThreadCreateFlags flags, MonoError *error);

#ifdef __cplusplus
template <typename T>
inline MonoInternalThread*
mono_thread_create_internal (MonoDomain *domain, T func, gpointer arg, MonoThreadCreateFlags flags, MonoError *error)
{
	return mono_thread_create_internal(domain, (gpointer)func, arg, flags, error);
}
#endif

MonoInternalThreadHandle
mono_thread_create_internal_handle (MonoDomain *domain, gpointer func, gpointer arg, MonoThreadCreateFlags flags, MonoError *error);

#ifdef __cplusplus
template <typename T>
inline MonoInternalThreadHandle
mono_thread_create_internal_handle (MonoDomain *domain, T func, gpointer arg, MonoThreadCreateFlags flags, MonoError *error)
{
	return mono_thread_create_internal_handle(domain, (gpointer)func, arg, flags, error);
}
#endif

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
gint32 ves_icall_System_Threading_Interlocked_Exchange_Int(gint32 *location, gint32 value);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_Exchange_Long(gint64 *location, gint64 value);

ICALL_EXPORT
MonoObject *ves_icall_System_Threading_Interlocked_Exchange_Object(MonoObject **location, MonoObject *value);

ICALL_EXPORT
gpointer ves_icall_System_Threading_Interlocked_Exchange_IntPtr(gpointer *location, gpointer value);

ICALL_EXPORT
gfloat ves_icall_System_Threading_Interlocked_Exchange_Single(gfloat *location, gfloat value);

ICALL_EXPORT
gdouble ves_icall_System_Threading_Interlocked_Exchange_Double(gdouble *location, gdouble value);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int(gint32 *location, gint32 value, gint32 comparand);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Interlocked_CompareExchange_Int_Success(gint32 *location, gint32 value, gint32 comparand, MonoBoolean *success);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Interlocked_CompareExchange_Long(gint64 *location, gint64 value, gint64 comparand);

ICALL_EXPORT
MonoObject *ves_icall_System_Threading_Interlocked_CompareExchange_Object(MonoObject **location, MonoObject *value, MonoObject *comparand);

ICALL_EXPORT
gpointer ves_icall_System_Threading_Interlocked_CompareExchange_IntPtr(gpointer *location, gpointer value, gpointer comparand);

ICALL_EXPORT
gfloat ves_icall_System_Threading_Interlocked_CompareExchange_Single(gfloat *location, gfloat value, gfloat comparand);

ICALL_EXPORT
gdouble ves_icall_System_Threading_Interlocked_CompareExchange_Double(gdouble *location, gdouble value, gdouble comparand);

ICALL_EXPORT
MonoObject* ves_icall_System_Threading_Interlocked_CompareExchange_T(MonoObject **location, MonoObject *value, MonoObject *comparand);

ICALL_EXPORT
MonoObject* ves_icall_System_Threading_Interlocked_Exchange_T(MonoObject **location, MonoObject *value);

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
gint8 ves_icall_System_Threading_Thread_VolatileRead1 (void *ptr);

ICALL_EXPORT
gint16 ves_icall_System_Threading_Thread_VolatileRead2 (void *ptr);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Thread_VolatileRead4 (void *ptr);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Thread_VolatileRead8 (void *ptr);

ICALL_EXPORT
void * ves_icall_System_Threading_Thread_VolatileReadIntPtr (void *ptr);

ICALL_EXPORT
void * ves_icall_System_Threading_Thread_VolatileReadObject (void *ptr);

ICALL_EXPORT
double ves_icall_System_Threading_Thread_VolatileReadDouble (void *ptr);

ICALL_EXPORT
float ves_icall_System_Threading_Thread_VolatileReadFloat (void *ptr);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWrite1 (void *ptr, gint8);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWrite2 (void *ptr, gint16);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWrite4 (void *ptr, gint32);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWrite8 (void *ptr, gint64);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWriteIntPtr (void *ptr, void *);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWriteObject (void *ptr, MonoObject *);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWriteFloat (void *ptr, float);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_VolatileWriteDouble (void *ptr, double);

ICALL_EXPORT
gint8 ves_icall_System_Threading_Volatile_Read1 (void *ptr);

ICALL_EXPORT
gint16 ves_icall_System_Threading_Volatile_Read2 (void *ptr);

ICALL_EXPORT
gint32 ves_icall_System_Threading_Volatile_Read4 (void *ptr);

ICALL_EXPORT
gint64 ves_icall_System_Threading_Volatile_Read8 (void *ptr);

ICALL_EXPORT
guint64 ves_icall_System_Threading_Volatile_ReadU8 (void *ptr);

ICALL_EXPORT
void * ves_icall_System_Threading_Volatile_ReadIntPtr (void *ptr);

ICALL_EXPORT
double ves_icall_System_Threading_Volatile_ReadDouble (void *ptr);

ICALL_EXPORT
float ves_icall_System_Threading_Volatile_ReadFloat (void *ptr);

ICALL_EXPORT
MonoObject* ves_icall_System_Threading_Volatile_Read_T (void *ptr);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_Write1 (void *ptr, gint8);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_Write2 (void *ptr, gint16);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_Write4 (void *ptr, gint32);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_Write8 (void *ptr, gint64);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_WriteU8 (void *ptr, guint64);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_WriteIntPtr (void *ptr, void *);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_WriteFloat (void *ptr, float);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_WriteDouble (void *ptr, double);

ICALL_EXPORT
void ves_icall_System_Threading_Volatile_Write_T (void *ptr, MonoObject *value);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_MemoryBarrier (void);

void
mono_threads_register_app_context (MonoAppContextHandle ctx, MonoError *error);
void
mono_threads_release_app_context (MonoAppContext* ctx, MonoError *error);

MONO_PROFILER_API MonoInternalThread *mono_thread_internal_current (void);

MonoInternalThreadHandle
mono_thread_internal_current_handle (void);

void mono_thread_internal_abort (MonoInternalThread *thread, gboolean appdomain_unload);
void mono_thread_internal_suspend_for_shutdown (MonoInternalThread *thread);

gboolean mono_thread_internal_has_appdomain_ref (MonoInternalThread *thread, MonoDomain *domain);

void mono_thread_internal_reset_abort (MonoInternalThread *thread);

void mono_thread_internal_unhandled_exception (MonoObject* exc);

void mono_alloc_special_static_data_free (GHashTable *special_static_fields);
gboolean mono_thread_current_check_pending_interrupt (void);

void mono_thread_set_state (MonoInternalThread *thread, MonoThreadState state);
void mono_thread_clr_state (MonoInternalThread *thread, MonoThreadState state);
gboolean mono_thread_test_state (MonoInternalThread *thread, MonoThreadState test);
gboolean mono_thread_test_and_set_state (MonoInternalThread *thread, MonoThreadState test, MonoThreadState set);
void mono_thread_clear_and_set_state (MonoInternalThread *thread, MonoThreadState clear, MonoThreadState set);

void mono_thread_init_apartment_state (void);
void mono_thread_cleanup_apartment_state (void);

void mono_threads_set_shutting_down (void);

gunichar2* mono_thread_get_name (MonoInternalThread *this_obj, guint32 *name_len);

MONO_API MonoException* mono_thread_get_undeniable_exception (void);

ICALL_EXPORT
void ves_icall_thread_finish_async_abort (void);

MONO_PROFILER_API void mono_thread_set_name_internal (MonoInternalThread *this_obj, MonoString *name, gboolean permanent, gboolean reset, MonoError *error);

void mono_thread_suspend_all_other_threads (void);
gboolean mono_threads_abort_appdomain_threads (MonoDomain *domain, int timeout);

void mono_thread_push_appdomain_ref (MonoDomain *domain);
void mono_thread_pop_appdomain_ref (void);
gboolean mono_thread_has_appdomain_ref (MonoThread *thread, MonoDomain *domain);

gboolean mono_thread_interruption_requested (void);

MonoException*
mono_thread_interruption_checkpoint (void);

gboolean
mono_thread_interruption_checkpoint_bool (void);

void
mono_thread_interruption_checkpoint_void (void);

MonoExceptionHandle
mono_thread_interruption_checkpoint_handle (void);

MonoException* mono_thread_force_interruption_checkpoint_noraise (void);
gint32* mono_thread_interruption_request_flag (void);

uint32_t mono_alloc_special_static_data (uint32_t static_type, uint32_t size, uint32_t align, uintptr_t *bitmap, int numbits);
void*    mono_get_special_static_data   (uint32_t offset);
gpointer mono_get_special_static_data_for_thread (MonoInternalThread *thread, guint32 offset);

void
mono_thread_resume_interruption (gboolean exec);
void mono_threads_perform_thread_dump (void);

// FIXME Correct the type of func and remove the template.
gboolean
mono_thread_create_checked (MonoDomain *domain, gpointer func, gpointer arg, MonoError *error);

#ifdef __cplusplus
template <typename T>
inline gboolean
mono_thread_create_checked (MonoDomain *domain, T func, gpointer arg, MonoError *error)
{
	return mono_thread_create_checked (domain, (gpointer)func, arg, error);
}
#endif

void mono_threads_add_joinable_runtime_thread (MonoThreadInfo *thread_info);
void mono_threads_add_joinable_thread (gpointer tid);
void mono_threads_join_threads (void);
void mono_thread_join (gpointer tid);

ICALL_EXPORT
void ves_icall_System_Threading_Thread_GetStackTraces (MonoArray **out_threads, MonoArray **out_stack_traces);

MONO_API gpointer
mono_threads_attach_coop (MonoDomain *domain, gpointer *dummy);

MONO_API void
mono_threads_detach_coop (gpointer cookie, gpointer *dummy);

MonoDomain*
mono_threads_attach_coop_internal (MonoDomain *domain, gpointer *cookie, MonoStackData *stackdata);

void
mono_threads_detach_coop_internal (MonoDomain *orig_domain, gpointer cookie, MonoStackData *stackdata);

void mono_threads_begin_abort_protected_block (void);
gboolean mono_threads_end_abort_protected_block (void);

gboolean
mono_thread_internal_current_is_attached (void);

void
mono_thread_internal_describe (MonoInternalThread *internal, GString *str);

gboolean
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

#define MONO_MAX_SUMMARY_NAME_LEN 140
#define MONO_MAX_THREAD_NAME_LEN 140
#define MONO_MAX_SUMMARY_THREADS 32
#define MONO_MAX_SUMMARY_FRAMES 80
#define MONO_MAX_SUMMARY_EXCEPTIONS 15

typedef struct {
	gboolean is_managed;
	char str_descr [MONO_MAX_SUMMARY_NAME_LEN];
	struct {
		int token;
		int il_offset;
		int native_offset;
		const char *guid;

#ifndef MONO_PRIVATE_CRASHES
		// We use ifdef to make it a compile-time error to store this 
		// symbolicated string on release builds
		const char *name;
#endif
		const char *filename;
		guint32 image_size;
		guint32 time_date_stamp;
	} managed_data;
	struct {
		intptr_t ip;
		gint32 offset;
		const char *module;
		gboolean is_trampoline;
		gboolean has_name;
	} unmanaged_data;
} MonoFrameSummary;

typedef struct {
	MonoClass *managed_exc_type;

	int num_managed_frames;
	MonoFrameSummary managed_frames [MONO_MAX_SUMMARY_FRAMES];
} MonoExcSummary;

typedef struct {
	guint64 offset_free_hash;
	guint64 offset_rich_hash;
} MonoStackHash;

typedef struct {
	gboolean done; // Needed because cond wait can have spurious wakeups
	MonoSemType done_wait; // Readers are finished with this

	// For managed stack walking

	MonoDomain *domain;
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;

	// Emitted attributes

	gboolean is_managed;

	char name [MONO_MAX_THREAD_NAME_LEN];

	intptr_t info_addr;
	intptr_t native_thread_id;

	// Print reason we don't have a complete
	// managed trace
	const char *error_msg;

	int num_managed_frames;
	MonoFrameSummary managed_frames [MONO_MAX_SUMMARY_FRAMES];

	int num_unmanaged_frames;
	MonoFrameSummary unmanaged_frames [MONO_MAX_SUMMARY_FRAMES];

	int num_exceptions;
	MonoExcSummary exceptions [MONO_MAX_SUMMARY_EXCEPTIONS];

	MonoStackHash hashes;

	MonoContext *ctx;
	MonoContext ctx_mem;
} MonoThreadSummary;

gboolean
mono_threads_summarize (MonoContext *ctx, gchar **out, MonoStackHash *hashes, gboolean silent, gboolean signal_handler_controller, gchar *mem, size_t provided_size);

gboolean
mono_threads_summarize_execute (MonoContext *ctx, gchar **out, MonoStackHash *hashes, gboolean silent, gchar *mem, size_t provided_size);

gboolean
mono_threads_summarize_one (MonoThreadSummary *out, MonoContext *ctx);

#endif /* _MONO_METADATA_THREADS_TYPES_H_ */
