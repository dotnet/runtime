/*
 * mono-threads.h: Low-level threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#ifndef __MONO_THREADS_H__
#define __MONO_THREADS_H__

#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-stack-unwinding.h>
#include <mono/utils/mono-linked-list-set.h>
#include <mono/utils/mono-mutex.h>
#include <mono/utils/mono-tls.h>

#include <glib.h>

#ifdef HOST_WIN32

#include <windows.h>

typedef DWORD MonoNativeThreadId;
typedef HANDLE MonoNativeThreadHandle; /* unused */

typedef DWORD mono_native_thread_return_t;

#define MONO_NATIVE_THREAD_ID_TO_UINT(tid) (tid)
#define MONO_UINT_TO_NATIVE_THREAD_ID(tid) ((MonoNativeThreadId)(tid))

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

#define MONO_NATIVE_THREAD_ID_TO_UINT(tid) (gsize)(tid)
#define MONO_UINT_TO_NATIVE_THREAD_ID(tid) (MonoNativeThreadId)(gsize)(tid)

#endif /* #ifdef HOST_WIN32 */

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

enum {
	STATE_STARTING			= 0x00,
	STATE_RUNNING			= 0x01,
	STATE_SHUTTING_DOWN		= 0x02,
	STATE_DEAD				= 0x03,
	RUN_STATE_MASK			= 0x0F,

	STATE_SUSPENDED			= 0x10,
	STATE_SELF_SUSPENDED	= 0x20,
	SUSPEND_STATE_MASK		= 0xF0,
};

/*
 * This enum tells which async thread service corresponds to which bit.
 */
typedef enum {
	MONO_SERVICE_REQUEST_SAMPLE = 1,
} MonoAsyncJob;

#define mono_thread_info_run_state(info) (((MonoThreadInfo*)info)->thread_state & RUN_STATE_MASK)
#define mono_thread_info_suspend_state(info) (((MonoThreadInfo*)info)->thread_state & SUSPEND_STATE_MASK)

typedef struct {
	MonoLinkedListSetNode node;
	guint32 small_id; /*Used by hazard pointers */
	MonoNativeThreadHandle native_handle; /* Valid on mach and android */
	int thread_state;

	/*Tells if this thread was created by the runtime or not.*/
	gboolean runtime_thread;

	/* Tells if this thread should be ignored or not by runtime services such as GC and profiling */
	gboolean tools_thread;

	/* suspend machinery, fields protected by suspend_semaphore */
	MonoSemType suspend_semaphore;
	int suspend_count;

	MonoSemType finish_resume_semaphore;
	MonoSemType resume_semaphore; 

	/* only needed by the posix backend */ 
#if (defined(_POSIX_VERSION) || defined(__native_client__)) && !defined (__MACH__)
	MonoSemType begin_suspend_semaphore;
	gboolean syscall_break_signal;
	gboolean suspend_can_continue;
#endif

	/*In theory, only the posix backend needs this, but having it on mach/win32 simplifies things a lot.*/
	MonoThreadUnwindState suspend_state;

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

	gboolean create_suspended;

	/* Semaphore used to implement CREATE_SUSPENDED */
	MonoSemType create_suspended_sem;

	/*
	 * Values of TLS variables for this thread.
	 * This can be used to obtain the values of TLS variable for threads
	 * other than the current one.
	 */
	gpointer tls [TLS_KEY_NUM];

	/* IO layer handle for this thread */
	/* Set when the thread is started, or in _wapi_thread_duplicate () */
	HANDLE handle;

	/* Asynchronous service request. This flag is meant to be consumed by the multiplexing signal handlers to discover what sort of work they need to do.
	 * Use the mono_threads_add_async_job and mono_threads_consume_async_jobs APIs to modify this flag.
	 * In the future the signaling should be part of the API, but for now, it's only for massaging the bits.
	 */
	volatile gint32 service_requests;
} MonoThreadInfo;

typedef struct {
	void* (*thread_register)(THREAD_INFO_TYPE *info, void *baseaddr);
	/*
	This callback is called with @info still on the thread list.
	This call is made while holding the suspend lock, so don't do callbacks.
	SMR remains functional as its small_id has not been reclaimed.
	*/
	void (*thread_unregister)(THREAD_INFO_TYPE *info);
	/*
	This callback is called right before thread_unregister. This is called
	without any locks held so it's the place for complicated cleanup.

	The thread must remain operational between this call and thread_unregister.
	It must be possible to successfully suspend it after thread_unregister completes.
	*/
	void (*thread_detach)(THREAD_INFO_TYPE *info);
	void (*thread_attach)(THREAD_INFO_TYPE *info);
	gboolean (*mono_method_is_critical) (void *method);
	void (*thread_exit)(void *retval);
#ifndef HOST_WIN32
	int (*mono_gc_pthread_create) (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg);
#endif
} MonoThreadInfoCallbacks;

typedef struct {
	void (*setup_async_callback) (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data);
	gboolean (*thread_state_init_from_sigctx) (MonoThreadUnwindState *state, void *sigctx);
	gboolean (*thread_state_init_from_handle) (MonoThreadUnwindState *tctx, MonoThreadInfo *info);
} MonoThreadInfoRuntimeCallbacks;

static inline gboolean
mono_threads_filter_tools_threads (THREAD_INFO_TYPE *info)
{
	return !((MonoThreadInfo*)info)->tools_thread;
}

/*
Requires the world to be stoped
*/
#define FOREACH_THREAD(thread) MONO_LLS_FOREACH_FILTERED (mono_thread_info_list_head (), thread, mono_threads_filter_tools_threads, THREAD_INFO_TYPE*)
#define END_FOREACH_THREAD MONO_LLS_END_FOREACH

/*
Snapshot iteration.
*/
#define FOREACH_THREAD_SAFE(thread) MONO_LLS_FOREACH_FILTERED_SAFE (mono_thread_info_list_head (), thread, mono_threads_filter_tools_threads, THREAD_INFO_TYPE*)
#define END_FOREACH_THREAD_SAFE MONO_LLS_END_FOREACH_SAFE

#define mono_thread_info_get_tid(info) ((MonoNativeThreadId)((MonoThreadInfo*)info)->node.key)
#define mono_thread_info_set_tid(info, val) do { ((MonoThreadInfo*)(info))->node.key = (uintptr_t)(val); } while (0)

/*
 * @thread_info_size is sizeof (GcThreadInfo), a struct the GC defines to make it possible to have
 * a single block with info from both camps. 
 */
void
mono_threads_init (MonoThreadInfoCallbacks *callbacks, size_t thread_info_size) MONO_INTERNAL;

void
mono_threads_runtime_init (MonoThreadInfoRuntimeCallbacks *callbacks) MONO_INTERNAL;

MonoThreadInfoCallbacks *
mono_threads_get_callbacks (void) MONO_INTERNAL;

MonoThreadInfoRuntimeCallbacks *
mono_threads_get_runtime_callbacks (void) MONO_INTERNAL;

int
mono_thread_info_register_small_id (void) MONO_INTERNAL;

THREAD_INFO_TYPE *
mono_thread_info_attach (void *baseptr) MONO_INTERNAL;

void
mono_thread_info_detach (void) MONO_INTERNAL;

gboolean
mono_thread_info_is_exiting (void) MONO_INTERNAL;

THREAD_INFO_TYPE *
mono_thread_info_current (void) MONO_INTERNAL;

int
mono_thread_info_get_small_id (void) MONO_INTERNAL;

MonoLinkedListSet*
mono_thread_info_list_head (void) MONO_INTERNAL;

THREAD_INFO_TYPE*
mono_thread_info_lookup (MonoNativeThreadId id) MONO_INTERNAL;

THREAD_INFO_TYPE*
mono_thread_info_safe_suspend_sync (MonoNativeThreadId tid, gboolean interrupt_kernel) MONO_INTERNAL;

gboolean
mono_thread_info_resume (MonoNativeThreadId tid) MONO_INTERNAL;

void
mono_thread_info_set_name (MonoNativeThreadId tid, const char *name) MONO_INTERNAL;

void
mono_thread_info_finish_suspend (MonoThreadInfo *info) MONO_INTERNAL;

void
mono_thread_info_finish_suspend_and_resume (MonoThreadInfo *info) MONO_INTERNAL;

void
mono_thread_info_self_suspend (void) MONO_INTERNAL;

gboolean
mono_thread_info_new_interrupt_enabled (void) MONO_INTERNAL;

void
mono_thread_info_setup_async_call (THREAD_INFO_TYPE *info, void (*target_func)(void*), void *user_data) MONO_INTERNAL;

void
mono_thread_info_suspend_lock (void) MONO_INTERNAL;

void
mono_thread_info_suspend_unlock (void) MONO_INTERNAL;

void
mono_thread_info_disable_new_interrupt (gboolean disable) MONO_INTERNAL;

void
mono_thread_info_abort_socket_syscall_for_close (MonoNativeThreadId tid) MONO_INTERNAL;

void
mono_thread_info_set_is_async_context (gboolean async_context) MONO_INTERNAL;

gboolean
mono_thread_info_is_async_context (void) MONO_INTERNAL;

void
mono_thread_info_get_stack_bounds (guint8 **staddr, size_t *stsize);

gboolean
mono_thread_info_yield (void) MONO_INTERNAL;

gpointer
mono_thread_info_tls_get (THREAD_INFO_TYPE *info, MonoTlsKey key);

void
mono_thread_info_tls_set (THREAD_INFO_TYPE *info, MonoTlsKey key, gpointer value);

void
mono_thread_info_exit (void);

HANDLE
mono_thread_info_open_handle (void);

gpointer
mono_thread_info_prepare_interrupt (HANDLE thread_handle);

void
mono_thread_info_finish_interrupt (gpointer wait_handle);

void
mono_thread_info_interrupt (HANDLE thread_handle);

void
mono_thread_info_self_interrupt (void);

void
mono_thread_info_clear_interruption (void);

HANDLE
mono_threads_create_thread (LPTHREAD_START_ROUTINE start, gpointer arg, guint32 stack_size, guint32 creation_flags, MonoNativeThreadId *out_tid);

int
mono_threads_get_max_stack_size (void) MONO_INTERNAL;

HANDLE
mono_threads_open_thread_handle (HANDLE handle, MonoNativeThreadId tid) MONO_INTERNAL;

/*
This is the async job submission/consumption API.
XXX: This is a PROVISIONAL API only meant to be used by the statistical profiler.
If you want to use/extend it anywhere else, understand that you'll have to do some API design work to better fit this puppy.
*/
gboolean
mono_threads_add_async_job (THREAD_INFO_TYPE *info, MonoAsyncJob job) MONO_INTERNAL;

MonoAsyncJob
mono_threads_consume_async_jobs (void) MONO_INTERNAL;

void
mono_threads_attach_tools_thread (void);


#if !defined(HOST_WIN32)

/*Use this instead of pthread_kill */
int
mono_threads_pthread_kill (THREAD_INFO_TYPE *info, int signum) MONO_INTERNAL;

#endif /* !defined(HOST_WIN32) */

/* Plartform specific functions DON'T use them */
void mono_threads_init_platform (void) MONO_INTERNAL; //ok
gboolean mono_threads_core_suspend (THREAD_INFO_TYPE *info, gboolean interrupt_kernel) MONO_INTERNAL;
gboolean mono_threads_core_resume (THREAD_INFO_TYPE *info) MONO_INTERNAL;
void mono_threads_platform_register (THREAD_INFO_TYPE *info) MONO_INTERNAL; //ok
void mono_threads_platform_free (THREAD_INFO_TYPE *info) MONO_INTERNAL;
void mono_threads_core_interrupt (THREAD_INFO_TYPE *info) MONO_INTERNAL;
void mono_threads_core_abort_syscall (THREAD_INFO_TYPE *info) MONO_INTERNAL;
gboolean mono_threads_core_needs_abort_syscall (void) MONO_INTERNAL;
HANDLE mono_threads_core_create_thread (LPTHREAD_START_ROUTINE start, gpointer arg, guint32 stack_size, guint32 creation_flags, MonoNativeThreadId *out_tid) MONO_INTERNAL;
void mono_threads_core_resume_created (THREAD_INFO_TYPE *info, MonoNativeThreadId tid) MONO_INTERNAL;
void mono_threads_core_get_stack_bounds (guint8 **staddr, size_t *stsize) MONO_INTERNAL;
gboolean mono_threads_core_yield (void) MONO_INTERNAL;
void mono_threads_core_exit (int exit_code) MONO_INTERNAL;
void mono_threads_core_unregister (THREAD_INFO_TYPE *info) MONO_INTERNAL;
HANDLE mono_threads_core_open_handle (void) MONO_INTERNAL;
HANDLE mono_threads_core_open_thread_handle (HANDLE handle, MonoNativeThreadId tid) MONO_INTERNAL;
void mono_threads_core_set_name (MonoNativeThreadId tid, const char *name) MONO_INTERNAL;
gpointer mono_threads_core_prepare_interrupt (HANDLE thread_handle);
void mono_threads_core_finish_interrupt (gpointer wait_handle);
void mono_threads_core_self_interrupt (void);
void mono_threads_core_clear_interruption (void);

MonoNativeThreadId mono_native_thread_id_get (void) MONO_INTERNAL;

gboolean mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2) MONO_INTERNAL;

gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg) MONO_INTERNAL;

/*Mach specific internals */
void mono_threads_init_dead_letter (void) MONO_INTERNAL;
void mono_threads_install_dead_letter (void) MONO_INTERNAL;

#endif /* __MONO_THREADS_H__ */
