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

/* FIXME used for CRITICAL_SECTION replace with mono-mutex  */
#include <mono/io-layer/io-layer.h>

#include <glib.h>

#ifdef HOST_WIN32

#include <windows.h>

typedef DWORD MonoNativeThreadId;
typedef HANDLE MonoNativeThreadHandle; /* unused */

typedef DWORD mono_native_thread_return_t;

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
	STATE_STARTING			= 0x01,
	STATE_RUNNING			= 0x02,
	STATE_SHUTTING_DOWN		= 0x03,
	STATE_DEAD				= 0x04,
	RUN_STATE_MASK			= 0x0F,

	STATE_SUSPENDED			= 0x10,
	STATE_SELF_SUSPENDED	= 0x20,
	SUSPEND_STATE_MASK		= 0xF0,
};

#define mono_thread_info_run_state(info) ((info)->thread_state & RUN_STATE_MASK)
#define mono_thread_info_suspend_state(info) ((info)->thread_state & SUSPEND_STATE_MASK)

typedef struct {
	MonoLinkedListSetNode node;
	guint32 small_id; /*Used by hazard pointers */
	MonoNativeThreadHandle native_handle; /* Valid on mach and android */
	int thread_state;

	/* suspend machinery, fields protected by the suspend_lock */
	CRITICAL_SECTION suspend_lock;
	int suspend_count;

	MonoSemType finish_resume_semaphore;
	MonoSemType resume_semaphore; 

	/* only needed by the posix backend */ 
#if (defined(_POSIX_VERSION) || defined(__native_client__)) && !defined (__MACH__)
	MonoSemType suspend_semaphore;
	gboolean syscall_break_signal;
	gboolean suspend_can_continue;
#endif

	/*In theory, only the posix backend needs this, but having it on mach/win32 simplifies things a lot.*/
	MonoThreadUnwindState suspend_state;

	/*async call machinery, thread MUST be suspended before accessing those fields*/
	void (*async_target)(void*);
	void *user_data;
} MonoThreadInfo;

typedef struct {
	void* (*thread_register)(THREAD_INFO_TYPE *info, void *baseaddr);
	/*
	This callback is called after @info is removed from the thread list.
	SMR remains functional as its small_id has not been reclaimed.
	*/
	void (*thread_unregister)(THREAD_INFO_TYPE *info);
	void (*thread_attach)(THREAD_INFO_TYPE *info);
	gboolean (*mono_method_is_critical) (void *method);
#ifndef HOST_WIN32
	int (*mono_gc_pthread_create) (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg);
#endif
} MonoThreadInfoCallbacks;

typedef struct {
	void (*setup_async_callback) (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data);
	gboolean (*thread_state_init_from_sigctx) (MonoThreadUnwindState *state, void *sigctx);
	gboolean (*thread_state_init_from_handle) (MonoThreadUnwindState *tctx, MonoNativeThreadId thread_id, MonoNativeThreadHandle thread_handle);
} MonoThreadInfoRuntimeCallbacks;

/*
Requires the world to be stoped
*/
#define FOREACH_THREAD(thread) MONO_LLS_FOREACH (mono_thread_info_list_head (), thread, SgenThreadInfo*)
#define END_FOREACH_THREAD MONO_LLS_END_FOREACH

/*
Snapshot iteration.
*/
#define FOREACH_THREAD_SAFE(thread) MONO_LLS_FOREACH_SAFE (mono_thread_info_list_head (), thread, SgenThreadInfo*)
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
mono_thread_info_dettach (void) MONO_INTERNAL;

THREAD_INFO_TYPE *
mono_thread_info_current (void) MONO_INTERNAL;

int
mono_thread_info_get_small_id (void) MONO_INTERNAL;

MonoLinkedListSet*
mono_thread_info_list_head (void) MONO_INTERNAL;

MonoThreadInfo*
mono_thread_info_lookup (MonoNativeThreadId id) MONO_INTERNAL;

MonoThreadInfo*
mono_thread_info_safe_suspend_sync (MonoNativeThreadId tid, gboolean interrupt_kernel) MONO_INTERNAL;

gboolean
mono_thread_info_resume (MonoNativeThreadId tid) MONO_INTERNAL;

void
mono_thread_info_self_suspend (void) MONO_INTERNAL;

gboolean
mono_thread_info_new_interrupt_enabled (void) MONO_INTERNAL;

void
mono_thread_info_setup_async_call (MonoThreadInfo *info, void (*target_func)(void*), void *user_data) MONO_INTERNAL;

void
mono_thread_info_suspend_lock (void) MONO_INTERNAL;

void
mono_thread_info_suspend_unlock (void) MONO_INTERNAL;

void
mono_threads_unregister_current_thread (THREAD_INFO_TYPE *info) MONO_INTERNAL;

void
mono_thread_info_disable_new_interrupt (gboolean disable) MONO_INTERNAL;

void
mono_thread_info_abort_socket_syscall_for_close (MonoNativeThreadId tid) MONO_INTERNAL;

#if !defined(HOST_WIN32)

int
mono_threads_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg) MONO_INTERNAL;

#if !defined(__MACH__)
/*Use this instead of pthread_kill */
int
mono_threads_pthread_kill (THREAD_INFO_TYPE *info, int signum) MONO_INTERNAL;
#endif

#else  /* !defined(HOST_WIN32) */

HANDLE
	mono_threads_CreateThread (LPSECURITY_ATTRIBUTES attributes, SIZE_T stack_size, LPTHREAD_START_ROUTINE start_routine, LPVOID arg, DWORD creation_flags, LPDWORD thread_id);


#endif /* !defined(HOST_WIN32) */

/* Plartform specific functions DON'T use them */
void mono_threads_init_platform (void) MONO_INTERNAL; //ok
gboolean mono_threads_core_suspend (MonoThreadInfo *info) MONO_INTERNAL;
gboolean mono_threads_core_resume (MonoThreadInfo *info) MONO_INTERNAL;
void mono_threads_platform_register (MonoThreadInfo *info) MONO_INTERNAL; //ok
void mono_threads_platform_free (MonoThreadInfo *info) MONO_INTERNAL;
void mono_threads_core_interrupt (MonoThreadInfo *info) MONO_INTERNAL;
void mono_threads_core_abort_syscall (MonoThreadInfo *info) MONO_INTERNAL;
gboolean mono_threads_core_needs_abort_syscall (void) MONO_INTERNAL;

MonoNativeThreadId mono_native_thread_id_get (void) MONO_INTERNAL;

gboolean mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2) MONO_INTERNAL;

gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg) MONO_INTERNAL;

#endif /* __MONO_THREADS_H__ */
