/**
 * \file
 * Low-level threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#ifndef __MONO_THREADS_H__
#define __MONO_THREADS_H__

#include <mono/utils/mono-forward-internal.h>
#include <mono/utils/mono-os-semaphore.h>
#include <mono/utils/mono-stack-unwinding.h>
#include <mono/utils/mono-linked-list-set.h>
#include <mono/utils/lock-free-alloc.h>
#include <mono/utils/lock-free-queue.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/utils/os-event.h>
#include <mono/utils/refcount.h>

#include <glib.h>
#include <config.h>
#ifdef HOST_WIN32

#include <windows.h>

typedef DWORD MonoNativeThreadId;
typedef HANDLE MonoNativeThreadHandle; /* unused */

typedef DWORD mono_native_thread_return_t;
typedef DWORD mono_thread_start_return_t;

#define MONO_NATIVE_THREAD_ID_TO_UINT(tid) (tid)
#define MONO_UINT_TO_NATIVE_THREAD_ID(tid) ((MonoNativeThreadId)(tid))

typedef LPTHREAD_START_ROUTINE MonoThreadStart;

#else

#include <pthread.h>

#if defined(__MACH__)
#include <mono/utils/mach-support.h>

typedef thread_port_t MonoNativeThreadHandle;

#else

#include <unistd.h>

typedef pid_t MonoNativeThreadHandle;

#endif /* defined(__MACH__) */

typedef pthread_t MonoNativeThreadId;

typedef void* mono_native_thread_return_t;
typedef gsize mono_thread_start_return_t;

#define MONO_NATIVE_THREAD_ID_TO_UINT(tid) (gsize)(tid)
#define MONO_UINT_TO_NATIVE_THREAD_ID(tid) (MonoNativeThreadId)(gsize)(tid)

typedef gsize (*MonoThreadStart)(gpointer);

#if !defined(__HAIKU__)
#define MONO_THREADS_PLATFORM_HAS_ATTR_SETSCHED
#endif /* !defined(__HAIKU__) */

#endif /* #ifdef HOST_WIN32 */

#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)

typedef struct {
	MonoRefCount ref;
	MonoOSEvent event;
} MonoThreadHandle;

/*
THREAD_INFO_TYPE is a way to make the mono-threads module parametric - or sort of.
The GC using mono-threads might extend the MonoThreadInfo struct to add its own
data, this avoid a pointer indirection on what is on a lot of hot paths.

But extending MonoThreadInfo has de disavantage that all functions here return type
would require a cast, something like the following:

typedef struct {
	MonoThreadInfo info;
	int stuff;
}  MyThreadInfo;

...
((MyThreadInfo*)mono_thread_info_current ())->stuff = 1;

While porting sgen to use mono-threads, the number of casts required was too much and
code ended up looking horrible. So we use this cute little hack. The idea is that
whomever is including this header can set the expected type to be used by functions here
and reduce the number of casts drastically.

*/
#ifndef THREAD_INFO_TYPE
#define THREAD_INFO_TYPE MonoThreadInfo
#endif

/* Mono Threads internal configuration knows*/

/* If this is defined, use the signals backed on Mach. Debug only as signals can't be made usable on OSX. */
// #define USE_SIGNALS_ON_MACH

#ifdef HOST_WASM
#define USE_WASM_BACKEND
#elif defined (_POSIX_VERSION)
#if defined (__MACH__) && !defined (USE_SIGNALS_ON_MACH)
#define USE_MACH_BACKEND
#else
#define USE_POSIX_BACKEND
#endif
#elif HOST_WIN32
#define USE_WINDOWS_BACKEND
#else
#error "no backend support for current platform"
#endif /* defined (_POSIX_VERSION) */

enum {
	STATE_STARTING				= 0x00,
	STATE_DETACHED				= 0x01,

	STATE_RUNNING				= 0x02,
	STATE_ASYNC_SUSPENDED			= 0x03,
	STATE_SELF_SUSPENDED			= 0x04,
	STATE_ASYNC_SUSPEND_REQUESTED		= 0x05,

	STATE_BLOCKING				= 0x06,
	STATE_BLOCKING_ASYNC_SUSPENDED 		= 0x07,
	/* FIXME: All the transitions from STATE_SELF_SUSPENDED and
	 * STATE_BLOCKING_SELF_SUSPENDED are the same - they should be the same
	 * state. */
	STATE_BLOCKING_SELF_SUSPENDED		= 0x08,
	STATE_BLOCKING_SUSPEND_REQUESTED	= 0x09,

	STATE_MAX				= 0x09,

	THREAD_STATE_MASK			= 0x007F,
	THREAD_SUSPEND_COUNT_MASK	= 0xFF00,
	THREAD_SUSPEND_COUNT_SHIFT	= 8,
	THREAD_SUSPEND_COUNT_MAX	= 0xFF,

	THREAD_SUSPEND_NO_SAFEPOINTS_MASK          = 0x0080,
	THREAD_SUSPEND_NO_SAFEPOINTS_SHIFT = 7,

	SELF_SUSPEND_STATE_INDEX = 0,
	ASYNC_SUSPEND_STATE_INDEX = 1,
};

/*
 * These flags control how the rest of the runtime will see and interact with
 * a thread.
 */
typedef enum {
	/*
	 * No flags means it's a normal thread that takes part in all runtime
	 * functionality.
	 */
	MONO_THREAD_INFO_FLAGS_NONE = 0,
	/*
	 * The thread will not be suspended by the STW machinery. The thread is not
	 * allowed to allocate or access managed memory at all, nor execute managed
	 * code.
	 */
	MONO_THREAD_INFO_FLAGS_NO_GC = 1,
	/*
	 * The thread will not be subject to profiler sampling signals.
	 */
	MONO_THREAD_INFO_FLAGS_NO_SAMPLE = 2,
} MonoThreadInfoFlags;

G_ENUM_FUNCTIONS (MonoThreadInfoFlags)

typedef struct _MonoThreadInfoInterruptToken MonoThreadInfoInterruptToken;

typedef struct _MonoThreadInfo {
	MonoLinkedListSetNode node;
	guint32 small_id; /*Used by hazard pointers */
	MonoNativeThreadHandle native_handle; /* Valid on mach, android and Windows */
	int thread_state;

	/*
	 * Must not be changed directly, and especially not by other threads. Use
	 * mono_thread_info_get/set_flags () to manipulate this.
	 */
	volatile gint32 flags;

	/*Tells if this thread was created by the runtime or not.*/
	gboolean runtime_thread;

	/* Max stack bounds, all valid addresses must be between [stack_start_limit, stack_end[ */
	void *stack_start_limit, *stack_end;

	/* suspend machinery, fields protected by suspend_semaphore */
	MonoSemType suspend_semaphore;
	int suspend_count;

	MonoSemType resume_semaphore;

	/* only needed by the posix backend */
#if defined(USE_POSIX_BACKEND)
	MonoSemType finish_resume_semaphore;
	gboolean syscall_break_signal;
	int signal;
#endif

	gboolean suspend_can_continue;

	/* This memory pool is used by coop GC to save stack data roots between GC unsafe regions */
	GByteArray *stackdata;

	/*In theory, only the posix backend needs this, but having it on mach/win32 simplifies things a lot.*/
	MonoThreadUnwindState thread_saved_state [2]; //0 is self suspend, 1 is async suspend.

	/*async call machinery, thread MUST be suspended before accessing those fields*/
	void (*async_target)(void*);
	void *user_data;

	/*
	If true, this thread is running a critical region of code and cannot be suspended.
	A critical session is implicitly started when you call mono_thread_info_safe_suspend_sync
	and is ended when you call either mono_thread_info_resume or mono_thread_info_finish_suspend.
	*/
	gboolean inside_critical_region;

	/*
	 * If TRUE, the thread is in async context. Code can use this information to avoid async-unsafe
	 * operations like locking without having to pass an 'async' parameter around.
	 */
	gboolean is_async_context;

	/*
	 * Values of TLS variables for this thread.
	 * This can be used to obtain the values of TLS variable for threads
	 * other than the current one.
	 */
	gpointer tls [TLS_KEY_NUM];

	/* IO layer handle for this thread */
	/* Set when the thread is started, or in _wapi_thread_duplicate () */
	MonoThreadHandle *handle;

	MonoJitTlsData *jit_data;

	MonoThreadInfoInterruptToken *interrupt_token;

	/* HandleStack for coop handles */
	MonoHandleStack *handle_stack;

	/* Stack mark for targets that explicitly require one */
	gpointer stack_mark;

	/* GCHandle to MonoInternalThread */
	guint32 internal_thread_gchandle;

	/*
	 * Used by the sampling code in mini-posix.c to ensure that a thread has
	 * handled a sampling signal before sending another one.
	 */
	gint32 profiler_signal_ack;

#ifdef USE_WINDOWS_BACKEND
	gint32 win32_apc_info;
	gpointer win32_apc_info_io_handle;
#endif

	/*
	 * This is where we store tools tls data so it follows our lifecycle and doesn't depends on posix tls cleanup ordering
	 *
	 * TODO support multiple values by multiple tools
	 */
	void *tools_data;
} MonoThreadInfo;

typedef struct {
	void* (*thread_attach)(THREAD_INFO_TYPE *info);
	/*
	This callback is called right before thread_detach_with_lock. This is called
	without any locks held so it's the place for complicated cleanup.

	The thread must remain operational between this call and thread_detach_with_lock.
	It must be possible to successfully suspend it after thread_detach completes.
	*/
	void (*thread_detach)(THREAD_INFO_TYPE *info);
	/*
	This callback is called with @info still on the thread list.
	This call is made while holding the suspend lock, so don't do callbacks.
	SMR remains functional as its small_id has not been reclaimed.
	*/
	void (*thread_detach_with_lock)(THREAD_INFO_TYPE *info);
	gboolean (*ip_in_critical_region) (MonoDomain *domain, gpointer ip);
	gboolean (*thread_in_critical_region) (THREAD_INFO_TYPE *info);

	// Called on the affected thread.
	void (*thread_flags_changing) (MonoThreadInfoFlags old, MonoThreadInfoFlags new_);
	void (*thread_flags_changed) (MonoThreadInfoFlags old, MonoThreadInfoFlags new_);
} MonoThreadInfoCallbacks;

typedef struct {
	void (*setup_async_callback) (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data);
	gboolean (*thread_state_init_from_sigctx) (MonoThreadUnwindState *state, void *sigctx);
	gboolean (*thread_state_init_from_handle) (MonoThreadUnwindState *tctx, MonoThreadInfo *info, /*optional*/ void *sigctx);
	void (*thread_state_init) (MonoThreadUnwindState *tctx);
} MonoThreadInfoRuntimeCallbacks;

//Not using 0 and 1 to ensure callbacks are not returning bad data
typedef enum {
	MonoResumeThread = 0x1234,
	KeepSuspended = 0x4321,
} SuspendThreadResult;

typedef SuspendThreadResult (*MonoSuspendThreadCallback) (THREAD_INFO_TYPE *info, gpointer user_data);

MONO_API MonoThreadInfoFlags
mono_thread_info_get_flags (THREAD_INFO_TYPE *info);

/*
 * Sets the thread info flags for the current thread. This function may invoke
 * callbacks containing arbitrary code (e.g. locks) so it must be assumed to be
 * async unsafe.
 */
MONO_API void
mono_thread_info_set_flags (MonoThreadInfoFlags flags);

static inline gboolean
mono_threads_filter_exclude_flags (THREAD_INFO_TYPE *info, MonoThreadInfoFlags flags)
{
	return !(mono_thread_info_get_flags (info) & flags);
}

/* Normal iteration; requires the world to be stopped. */

#define FOREACH_THREAD_ALL(thread) \
	MONO_LLS_FOREACH_FILTERED (mono_thread_info_list_head (), THREAD_INFO_TYPE, thread, mono_lls_filter_accept_all, NULL)

#define FOREACH_THREAD_EXCLUDE(thread, not_flags) \
	MONO_LLS_FOREACH_FILTERED (mono_thread_info_list_head (), THREAD_INFO_TYPE, thread, mono_threads_filter_exclude_flags, not_flags)

#define FOREACH_THREAD_END \
	MONO_LLS_FOREACH_END

/* Snapshot iteration; can be done anytime but is slower. */

#define FOREACH_THREAD_SAFE_ALL(thread) \
	MONO_LLS_FOREACH_FILTERED_SAFE (mono_thread_info_list_head (), THREAD_INFO_TYPE, thread, mono_lls_filter_accept_all, NULL)

#define FOREACH_THREAD_SAFE_EXCLUDE(thread, not_flags) \
	MONO_LLS_FOREACH_FILTERED_SAFE (mono_thread_info_list_head (), THREAD_INFO_TYPE, thread, mono_threads_filter_exclude_flags, not_flags)

#define FOREACH_THREAD_SAFE_END \
	MONO_LLS_FOREACH_SAFE_END

static inline MonoNativeThreadId
mono_thread_info_get_tid (THREAD_INFO_TYPE *info)
{
	return MONO_UINT_TO_NATIVE_THREAD_ID (((MonoThreadInfo*) info)->node.key);
}

static inline void
mono_thread_info_set_tid (THREAD_INFO_TYPE *info, MonoNativeThreadId tid)
{
	((MonoThreadInfo*) info)->node.key = (uintptr_t) MONO_NATIVE_THREAD_ID_TO_UINT (tid);
}

void
mono_thread_info_cleanup (void);

/*
 * @thread_info_size is sizeof (GcThreadInfo), a struct the GC defines to make it possible to have
 * a single block with info from both camps. 
 */
void
mono_thread_info_init (size_t thread_info_size);

/*
 * Wait for the above mono_thread_info_init to be called
 */
void
mono_thread_info_wait_inited (void);

void
mono_thread_info_callbacks_init (MonoThreadInfoCallbacks *callbacks);

void
mono_thread_info_signals_init (void);

void
mono_thread_info_runtime_init (MonoThreadInfoRuntimeCallbacks *callbacks);

MonoThreadInfoRuntimeCallbacks *
mono_threads_get_runtime_callbacks (void);

MONO_API int
mono_thread_info_register_small_id (void);

MONO_API THREAD_INFO_TYPE *
mono_thread_info_attach (void);

MONO_API void
mono_thread_info_detach (void);

gboolean
mono_thread_info_try_get_internal_thread_gchandle (THREAD_INFO_TYPE *info, guint32 *gchandle);

void
mono_thread_info_set_internal_thread_gchandle (THREAD_INFO_TYPE *info, guint32 gchandle);

void
mono_thread_info_unset_internal_thread_gchandle (THREAD_INFO_TYPE *info);

gboolean
mono_thread_info_is_exiting (void);

#ifdef HOST_WIN32
G_EXTERN_C // due to THREAD_INFO_TYPE varying
#endif
THREAD_INFO_TYPE *
mono_thread_info_current (void);

MONO_API gboolean
mono_thread_info_set_tools_data (void *data);

MONO_API void*
mono_thread_info_get_tools_data (void);


THREAD_INFO_TYPE*
mono_thread_info_current_unchecked (void);

MONO_API int
mono_thread_info_get_small_id (void);

MonoLinkedListSet*
mono_thread_info_list_head (void);

THREAD_INFO_TYPE*
mono_thread_info_lookup (MonoNativeThreadId id);

gboolean
mono_thread_info_resume (MonoNativeThreadId tid);

void
mono_thread_info_safe_suspend_and_run (MonoNativeThreadId id, gboolean interrupt_kernel, MonoSuspendThreadCallback callback, gpointer user_data);

void
mono_thread_info_setup_async_call (THREAD_INFO_TYPE *info, void (*target_func)(void*), void *user_data);

void
mono_thread_info_suspend_lock (void);

void
mono_thread_info_suspend_unlock (void);

void
mono_thread_info_abort_socket_syscall_for_close (MonoNativeThreadId tid);

void
mono_thread_info_set_is_async_context (gboolean async_context);

gboolean
mono_thread_info_is_async_context (void);

void
mono_thread_info_get_stack_bounds (guint8 **staddr, size_t *stsize);

MONO_API gboolean
mono_thread_info_yield (void);

gint
mono_thread_info_sleep (guint32 ms, gboolean *alerted);

gint
mono_thread_info_usleep (guint64 us);

gpointer
mono_thread_info_tls_get (THREAD_INFO_TYPE *info, MonoTlsKey key);

void
mono_thread_info_tls_set (THREAD_INFO_TYPE *info, MonoTlsKey key, gpointer value);

void
mono_thread_info_exit (gsize exit_code);

MONO_PAL_API void
mono_thread_info_install_interrupt (void (*callback) (gpointer data), gpointer data, gboolean *interrupted);

MONO_PAL_API void
mono_thread_info_uninstall_interrupt (gboolean *interrupted);

MonoThreadInfoInterruptToken*
mono_thread_info_prepare_interrupt (THREAD_INFO_TYPE *info);

void
mono_thread_info_finish_interrupt (MonoThreadInfoInterruptToken *token);

void
mono_thread_info_self_interrupt (void);

void
mono_thread_info_clear_self_interrupt (void);

gboolean
mono_thread_info_is_interrupt_state (THREAD_INFO_TYPE *info);

void
mono_thread_info_describe_interrupt_token (THREAD_INFO_TYPE *info, GString *text);

G_EXTERN_C // due to THREAD_INFO_TYPE varying
gboolean
mono_thread_info_is_live (THREAD_INFO_TYPE *info);

int
mono_thread_info_get_system_max_stack_size (void);

MonoThreadHandle*
mono_threads_open_thread_handle (MonoThreadHandle *handle);

void
mono_threads_close_thread_handle (MonoThreadHandle *handle);

#if !defined(HOST_WIN32)

/*Use this instead of pthread_kill */
int
mono_threads_pthread_kill (THREAD_INFO_TYPE *info, int signum);

#endif /* !defined(HOST_WIN32) */

/* Internal API between mono-threads and its backends. */

/* Backend functions - a backend must implement all of the following */
/*
This is called very early in the runtime, it cannot access any runtime facilities.

*/
void mono_threads_suspend_init (void); //ok

void mono_threads_suspend_init_signals (void);

void mono_threads_coop_init (void);

/*
This begins async suspend. This function must do the following:

-Ensure the target will EINTR any syscalls if @interrupt_kernel is true
-Call mono_threads_transition_finish_async_suspend as part of its async suspend.
-Register the thread for pending suspend with mono_threads_add_to_pending_operation_set if needed.

If begin suspend fails the thread must be left uninterrupted and resumed.
*/
gboolean mono_threads_suspend_begin_async_suspend (THREAD_INFO_TYPE *info, gboolean interrupt_kernel);

/*
This verifies the outcome of an async suspend operation.

Some targets, such as posix, verify suspend results assynchronously. Suspend results must be
available (in a non blocking way) after mono_threads_wait_pending_operations completes.
*/
gboolean mono_threads_suspend_check_suspend_result (THREAD_INFO_TYPE *info);

/*
This begins async resume. This function must do the following:

- Install an async target if one was requested.
- Notify the target to resume.
- Register the thread for pending ack with mono_threads_add_to_pending_operation_set if needed.
*/
gboolean mono_threads_suspend_begin_async_resume (THREAD_INFO_TYPE *info);

void mono_threads_suspend_register (THREAD_INFO_TYPE *info); //ok
void mono_threads_suspend_free (THREAD_INFO_TYPE *info);
void mono_threads_suspend_abort_syscall (THREAD_INFO_TYPE *info);
gint mono_threads_suspend_search_alternative_signal (void);
gint mono_threads_suspend_get_suspend_signal (void);
gint mono_threads_suspend_get_restart_signal (void);
gint mono_threads_suspend_get_abort_signal (void);

gboolean
mono_thread_platform_create_thread (MonoThreadStart thread_fn, gpointer thread_data,
	gsize* const stack_size, MonoNativeThreadId *tid);

void mono_threads_platform_get_stack_bounds (guint8 **staddr, size_t *stsize);
gboolean mono_threads_platform_is_main_thread (void);
void mono_threads_platform_init (void);
gboolean mono_threads_platform_in_critical_region (THREAD_INFO_TYPE *info);
gboolean mono_threads_platform_yield (void);
void mono_threads_platform_exit (gsize exit_code);

void mono_threads_coop_begin_global_suspend (void);
void mono_threads_coop_end_global_suspend (void);

MONO_API MonoNativeThreadId
mono_native_thread_id_get (void);

MONO_API gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2);

//FIXMEcxx typedef mono_native_thread_return_t (MONO_STDCALL * MonoNativeThreadStart)(void*);
// mono_native_thread_return_t func
// and remove the template

MONO_API gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg);

#ifdef __cplusplus
template <typename T>
inline gboolean
mono_native_thread_create (MonoNativeThreadId *tid, T func, gpointer arg)
{
	return  mono_native_thread_create (tid, (gpointer)func, arg);
}
#endif

MONO_API void
mono_native_thread_set_name (MonoNativeThreadId tid, const char *name);

size_t
mono_native_thread_get_name (MonoNativeThreadId tid, char *name_out, size_t max_len);

MONO_API gboolean
mono_native_thread_join (MonoNativeThreadId tid);

/*Mach specific internals */
void mono_threads_init_dead_letter (void);
void mono_threads_install_dead_letter (void);

/* mono-threads internal API used by the backends. */
/*
This tells the suspend initiator that we completed suspend and will now be waiting for resume.
*/
void mono_threads_notify_initiator_of_suspend (THREAD_INFO_TYPE* info);
/*
This tells the resume initiator that we completed resume duties and will return to runnable state.
*/
void mono_threads_notify_initiator_of_resume (THREAD_INFO_TYPE* info);

/*
This tells the resume initiator that we completed abort duties and will return to previous state.
*/
void mono_threads_notify_initiator_of_abort (THREAD_INFO_TYPE* info);

/* Thread state machine functions */

typedef enum {
	ResumeError,
	ResumeOk,
	ResumeInitSelfResume,
	ResumeInitAsyncResume,
	ResumeInitBlockingResume,
} MonoResumeResult;

typedef enum {
	PulseInitAsyncPulse,
} MonoPulseResult;

typedef enum {
	SelfSuspendResumed,
	SelfSuspendNotifyAndWait,
} MonoSelfSupendResult;

typedef enum {
	ReqSuspendAlreadySuspended,
	ReqSuspendAlreadySuspendedBlocking,
	ReqSuspendInitSuspendRunning,
	ReqSuspendInitSuspendBlocking,
} MonoRequestSuspendResult;

typedef enum {
	DoBlockingContinue, //in blocking mode, continue
	DoBlockingPollAndRetry, //async suspend raced blocking and won, pool and retry
} MonoDoBlockingResult;

typedef enum {
	DoneBlockingOk, //exited blocking fine
	DoneBlockingWait, //thread should end suspended and wait for resume
} MonoDoneBlockingResult;


typedef enum {
	AbortBlockingIgnore, //Ignore
	AbortBlockingIgnoreAndPoll, //Ignore and poll
	AbortBlockingOk, //Abort worked
	AbortBlockingWait, //Abort worked, but should wait for resume
} MonoAbortBlockingResult;


void mono_threads_transition_attach (THREAD_INFO_TYPE* info);
gboolean mono_threads_transition_detach (THREAD_INFO_TYPE *info);
MonoRequestSuspendResult mono_threads_transition_request_suspension (THREAD_INFO_TYPE *info);
MonoSelfSupendResult mono_threads_transition_state_poll (THREAD_INFO_TYPE *info);
MonoResumeResult mono_threads_transition_request_resume (THREAD_INFO_TYPE* info);
MonoPulseResult mono_threads_transition_request_pulse (THREAD_INFO_TYPE* info);
gboolean mono_threads_transition_finish_async_suspend (THREAD_INFO_TYPE* info);
MonoDoBlockingResult mono_threads_transition_do_blocking (THREAD_INFO_TYPE* info, const char* func);
MonoDoneBlockingResult mono_threads_transition_done_blocking (THREAD_INFO_TYPE* info, const char* func);
MonoAbortBlockingResult mono_threads_transition_abort_blocking (THREAD_INFO_TYPE* info, const char* func);
gboolean mono_threads_transition_peek_blocking_suspend_requested (THREAD_INFO_TYPE* info);
void mono_threads_transition_begin_no_safepoints (THREAD_INFO_TYPE* info, const char *func);
void mono_threads_transition_end_no_safepoints (THREAD_INFO_TYPE* info, const char *func);

G_EXTERN_C // due to THREAD_INFO_TYPE varying
MonoThreadUnwindState* mono_thread_info_get_suspend_state (THREAD_INFO_TYPE *info);

gpointer
mono_threads_enter_gc_unsafe_region_cookie (void);


void mono_thread_info_wait_for_resume (THREAD_INFO_TYPE *info);
/* Advanced suspend API, used for suspending multiple threads as once. */
G_EXTERN_C // due to THREAD_INFO_TYPE varying
gboolean mono_thread_info_is_running (THREAD_INFO_TYPE *info);
gboolean mono_thread_info_is_live (THREAD_INFO_TYPE *info);
G_EXTERN_C // due to THREAD_INFO_TYPE varying
int mono_thread_info_suspend_count (THREAD_INFO_TYPE *info);
int mono_thread_info_current_state (THREAD_INFO_TYPE *info);
const char* mono_thread_state_name (int state);
gboolean mono_thread_is_gc_unsafe_mode (void);
G_EXTERN_C // due to THREAD_INFO_TYPE varying
gboolean mono_thread_info_will_not_safepoint (THREAD_INFO_TYPE *info);

/* Suspend phases:
 *
 * In a full coop or full preemptive suspend, there is only a single phase.  In
 * the initial phase, all threads are either cooperatively or preemptively
 * suspended, respectively.
 *
 * In hybrid suspend, there may be two phases.  In the initial phase, threads
 * are invited to cooperatively suspend.  Running threads are expected to
 * finish cooperatively suspending (the phase waits for them), but blocking
 * threads need not.
 *
 * If any blocking thread was encountered in the initial phase, a second
 * "mop-up" phase runs which checks whether the blocking threads self-suspended
 * (in which case nothing more needs to be done) or if they're still in the
 * BLOCKING_SUSPEND_REQUESTED state, in which case they are preemptively
 * suspended.
 */
#define MONO_THREAD_SUSPEND_PHASE_INITIAL (0)
#define MONO_THREAD_SUSPEND_PHASE_MOPUP (1)
// number of phases
#define MONO_THREAD_SUSPEND_PHASE_COUNT (2)
typedef int MonoThreadSuspendPhase;

typedef enum {
	MONO_THREAD_BEGIN_SUSPEND_SKIP = 0,
	MONO_THREAD_BEGIN_SUSPEND_SUSPENDED = 1,
	MONO_THREAD_BEGIN_SUSPEND_NEXT_PHASE = 2,
} MonoThreadBeginSuspendResult;

G_EXTERN_C // due to THREAD_INFO_TYPE varying
gboolean mono_thread_info_in_critical_location (THREAD_INFO_TYPE *info);
G_EXTERN_C // due to THREAD_INFO_TYPE varying
MonoThreadBeginSuspendResult mono_thread_info_begin_suspend (THREAD_INFO_TYPE *info, MonoThreadSuspendPhase phase);
G_EXTERN_C // due to THREAD_INFO_TYPE varying
gboolean mono_thread_info_begin_resume (THREAD_INFO_TYPE *info);
G_EXTERN_C // due to THREAD_INFO_TYPE varying
gboolean mono_thread_info_begin_pulse_resume_and_request_suspension (THREAD_INFO_TYPE *info);


void mono_threads_add_to_pending_operation_set (THREAD_INFO_TYPE* info); //XXX rename to something to reflect the fact that this is used for both suspend and resume
gboolean mono_threads_wait_pending_operations (void);
void mono_threads_begin_global_suspend (void);
void mono_threads_end_global_suspend (void);

gboolean
mono_thread_info_is_current (THREAD_INFO_TYPE *info);

typedef enum {
	MONO_THREAD_INFO_WAIT_RET_SUCCESS_0   =  0,
	MONO_THREAD_INFO_WAIT_RET_ALERTED     = -1,
	MONO_THREAD_INFO_WAIT_RET_TIMEOUT     = -2,
	MONO_THREAD_INFO_WAIT_RET_FAILED      = -3,
} MonoThreadInfoWaitRet;

MonoThreadInfoWaitRet
mono_thread_info_wait_one_handle (MonoThreadHandle *handle, guint32 timeout, gboolean alertable);

MonoThreadInfoWaitRet
mono_thread_info_wait_multiple_handle (MonoThreadHandle **thread_handles, gsize nhandles, MonoOSEvent *background_change_event, gboolean waitall, guint32 timeout, gboolean alertable);

void mono_threads_join_lock (void);
void mono_threads_join_unlock (void);


#ifdef HOST_WASM
typedef void (*background_job_cb)(void);
void mono_threads_schedule_background_job (background_job_cb cb);
#endif

#ifdef USE_WINDOWS_BACKEND

void
mono_win32_enter_alertable_wait (THREAD_INFO_TYPE *info);

void
mono_win32_leave_alertable_wait (THREAD_INFO_TYPE *info);

void
mono_win32_enter_blocking_io_call (THREAD_INFO_TYPE *info, HANDLE io_handle);

void
mono_win32_leave_blocking_io_call (THREAD_INFO_TYPE *info, HANDLE io_handle);

void
mono_win32_interrupt_wait (PVOID thread_info, HANDLE native_thread_handle, DWORD tid);

void
mono_win32_abort_blocking_io_call (THREAD_INFO_TYPE *info);

#define W32_DEFINE_LAST_ERROR_RESTORE_POINT \
	DWORD _last_error_restore_point = GetLastError ();

#define W32_RESTORE_LAST_ERROR_FROM_RESTORE_POINT \
		/* Only restore if changed to prevent unecessary writes. */ \
		if (GetLastError () != _last_error_restore_point) \
			SetLastError (_last_error_restore_point);

#else

#define W32_DEFINE_LAST_ERROR_RESTORE_POINT /* nothing */
#define W32_RESTORE_LAST_ERROR_FROM_RESTORE_POINT /* nothing */

#endif

#endif /* __MONO_THREADS_H__ */
