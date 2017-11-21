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

#include <mono/utils/mono-os-semaphore.h>
#include <mono/utils/mono-stack-unwinding.h>
#include <mono/utils/mono-linked-list-set.h>
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

#ifndef MONO_INFINITE_WAIT
#define MONO_INFINITE_WAIT ((guint32) 0xFFFFFFFF)
#endif

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
	STATE_RUNNING				= 0x01,
	STATE_DETACHED				= 0x02,

	STATE_ASYNC_SUSPENDED			= 0x03,
	STATE_SELF_SUSPENDED			= 0x04,
	STATE_ASYNC_SUSPEND_REQUESTED	= 0x05,
	STATE_SELF_SUSPEND_REQUESTED 	= 0x06,
	STATE_BLOCKING					= 0x07,
	STATE_BLOCKING_AND_SUSPENDED	= 0x8,

	STATE_MAX						= 0x08,

	THREAD_STATE_MASK			= 0x00FF,
	THREAD_SUSPEND_COUNT_MASK	= 0xFF00,
	THREAD_SUSPEND_COUNT_SHIFT	= 8,
	THREAD_SUSPEND_COUNT_MAX	= 0xFF,

	SELF_SUSPEND_STATE_INDEX = 0,
	ASYNC_SUSPEND_STATE_INDEX = 1,
};

typedef struct _MonoThreadInfoInterruptToken MonoThreadInfoInterruptToken;

typedef struct {
	MonoLinkedListSetNode node;
	guint32 small_id; /*Used by hazard pointers */
	MonoNativeThreadHandle native_handle; /* Valid on mach and android */
	int thread_state;

	/*Tells if this thread was created by the runtime or not.*/
	gboolean runtime_thread;

	/* Tells if this thread should be ignored or not by runtime services such as GC and profiling */
	gboolean tools_thread;

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

	void *jit_data;

	MonoThreadInfoInterruptToken *interrupt_token;

	/* HandleStack for coop handles */
	gpointer handle_stack;

	/* Stack mark for targets that explicitly require one */
	gpointer stack_mark;

	/* GCHandle to MonoInternalThread */
	guint32 internal_thread_gchandle;

	/*
	 * Used by the sampling code in mini-posix.c to ensure that a thread has
	 * handled a sampling signal before sending another one.
	 */
	gint32 profiler_signal_ack;

	gint32 thread_pending_native_join;

#ifdef USE_WINDOWS_BACKEND
	gint32 thread_wait_info;
#endif

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

static inline gboolean
mono_threads_filter_tools_threads (THREAD_INFO_TYPE *info)
{
	return !((MonoThreadInfo*)info)->tools_thread;
}

/*
Requires the world to be stoped
*/
#define FOREACH_THREAD(thread) \
	MONO_LLS_FOREACH_FILTERED (mono_thread_info_list_head (), THREAD_INFO_TYPE, thread, mono_threads_filter_tools_threads)

#define FOREACH_THREAD_END \
	MONO_LLS_FOREACH_END

/*
Snapshot iteration.
*/
#define FOREACH_THREAD_SAFE(thread) \
	MONO_LLS_FOREACH_FILTERED_SAFE (mono_thread_info_list_head (), THREAD_INFO_TYPE, thread, mono_threads_filter_tools_threads)

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

/*
 * @thread_info_size is sizeof (GcThreadInfo), a struct the GC defines to make it possible to have
 * a single block with info from both camps. 
 */
void
mono_thread_info_init (size_t thread_info_size);

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

THREAD_INFO_TYPE *
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

THREAD_INFO_TYPE *
mono_thread_info_current (void);

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

void
mono_thread_info_install_interrupt (void (*callback) (gpointer data), gpointer data, gboolean *interrupted);

void
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

gboolean
mono_thread_info_is_live (THREAD_INFO_TYPE *info);

int
mono_threads_get_max_stack_size (void);

MonoThreadHandle*
mono_threads_open_thread_handle (MonoThreadHandle *handle);

void
mono_threads_close_thread_handle (MonoThreadHandle *handle);

MONO_API void
mono_threads_attach_tools_thread (void);


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
void mono_threads_platform_init (void);
gboolean mono_threads_platform_in_critical_region (MonoNativeThreadId tid);
gboolean mono_threads_platform_yield (void);
void mono_threads_platform_exit (gsize exit_code);

void mono_threads_coop_begin_global_suspend (void);
void mono_threads_coop_end_global_suspend (void);

MONO_API MonoNativeThreadId
mono_native_thread_id_get (void);

MONO_API gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2);

MONO_API gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg);

MONO_API void
mono_native_thread_set_name (MonoNativeThreadId tid, const char *name);

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
	SelfSuspendResumed,
	SelfSuspendWait,
	SelfSuspendNotifyAndWait,
} MonoSelfSupendResult;

typedef enum {
	AsyncSuspendAlreadySuspended,
	AsyncSuspendWait,
	AsyncSuspendInitSuspend,
	AsyncSuspendBlocking,
} MonoRequestAsyncSuspendResult;

typedef enum {
	DoBlockingContinue, //in blocking mode, continue
	DoBlockingPollAndRetry, //async suspend raced blocking and won, pool and retry
} MonoDoBlockingResult;

typedef enum {
	DoneBlockingOk, //exited blocking fine
	DoneBlockingWait, //thread should end suspended
} MonoDoneBlockingResult;


typedef enum {
	AbortBlockingIgnore, //Ignore
	AbortBlockingIgnoreAndPoll, //Ignore and poll
	AbortBlockingOk, //Abort worked
	AbortBlockingWait, //Abort worked, but should wait for resume
} MonoAbortBlockingResult;


void mono_threads_transition_attach (THREAD_INFO_TYPE* info);
gboolean mono_threads_transition_detach (THREAD_INFO_TYPE *info);
MonoRequestAsyncSuspendResult mono_threads_transition_request_async_suspension (THREAD_INFO_TYPE *info);
MonoSelfSupendResult mono_threads_transition_state_poll (THREAD_INFO_TYPE *info);
MonoResumeResult mono_threads_transition_request_resume (THREAD_INFO_TYPE* info);
gboolean mono_threads_transition_finish_async_suspend (THREAD_INFO_TYPE* info);
MonoDoBlockingResult mono_threads_transition_do_blocking (THREAD_INFO_TYPE* info);
MonoDoneBlockingResult mono_threads_transition_done_blocking (THREAD_INFO_TYPE* info);
MonoAbortBlockingResult mono_threads_transition_abort_blocking (THREAD_INFO_TYPE* info);

MonoThreadUnwindState* mono_thread_info_get_suspend_state (THREAD_INFO_TYPE *info);

gpointer
mono_threads_enter_gc_unsafe_region_cookie (void);


void mono_thread_info_wait_for_resume (THREAD_INFO_TYPE *info);
/* Advanced suspend API, used for suspending multiple threads as once. */
gboolean mono_thread_info_is_running (THREAD_INFO_TYPE *info);
gboolean mono_thread_info_is_live (THREAD_INFO_TYPE *info);
int mono_thread_info_suspend_count (THREAD_INFO_TYPE *info);
int mono_thread_info_current_state (THREAD_INFO_TYPE *info);
const char* mono_thread_state_name (int state);
gboolean mono_thread_is_gc_unsafe_mode (void);

gboolean mono_thread_info_in_critical_location (THREAD_INFO_TYPE *info);
gboolean mono_thread_info_begin_suspend (THREAD_INFO_TYPE *info);
gboolean mono_thread_info_begin_resume (THREAD_INFO_TYPE *info);

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

#endif /* __MONO_THREADS_H__ */
