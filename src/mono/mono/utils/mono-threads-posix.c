/*
 * mono-threads-posix.c: Low-level threading, posix version
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include <config.h>

/* For pthread_main_np, pthread_get_stackaddr_np and pthread_get_stacksize_np */
#if defined (__MACH__)
#define _DARWIN_C_SOURCE 1
#endif

#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-posix-signals.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/metadata/gc-internals.h>
#include <mono/utils/w32handle.h>

#include <errno.h>

#if defined(PLATFORM_ANDROID) && !defined(TARGET_ARM64) && !defined(TARGET_AMD64)
#define USE_TKILL_ON_ANDROID 1
#endif

#ifdef USE_TKILL_ON_ANDROID
extern int tkill (pid_t tid, int signal);
#endif

#if defined(_POSIX_VERSION) || defined(__native_client__)

#include <pthread.h>

#include <sys/resource.h>

#if defined(__native_client__)
void nacl_shutdown_gc_thread(void);
#endif

typedef struct {
	pthread_t id;
	GPtrArray *owned_mutexes;
	gint32 priority;
} MonoW32HandleThread;

static gpointer
thread_handle_create (void)
{
	MonoW32HandleThread thread_data;
	gpointer thread_handle;

	thread_data.id = pthread_self ();
	thread_data.owned_mutexes = g_ptr_array_new ();
	thread_data.priority = MONO_THREAD_PRIORITY_NORMAL;

	thread_handle = mono_w32handle_new (MONO_W32HANDLE_THREAD, (gpointer) &thread_data);
	if (thread_handle == INVALID_HANDLE_VALUE)
		return NULL;

	/* We need to keep the handle alive, as long as the corresponding managed
	 * thread object is alive. The handle is going to be unref when calling
	 * the finalizer on the MonoThreadInternal object */
	mono_w32handle_ref (thread_handle);

	return thread_handle;
}

static int
win32_priority_to_posix_priority (MonoThreadPriority priority, int policy)
{
	g_assert (priority >= MONO_THREAD_PRIORITY_LOWEST);
	g_assert (priority <= MONO_THREAD_PRIORITY_HIGHEST);

/* Necessary to get valid priority range */
#ifdef _POSIX_PRIORITY_SCHEDULING
	int max, min;

	min = sched_get_priority_min (policy);
	max = sched_get_priority_max (policy);

	/* Partition priority range linearly (cross-multiply) */
	if (max > 0 && min >= 0 && max > min)
		return (int)((double) priority * (max - min) / (MONO_THREAD_PRIORITY_HIGHEST - MONO_THREAD_PRIORITY_LOWEST));
#endif

	switch (policy) {
	case SCHED_FIFO:
	case SCHED_RR:
		return 50;
#ifdef SCHED_BATCH
	case SCHED_BATCH:
#endif
	case SCHED_OTHER:
		return 0;
	default:
		return -1;
	}
}

void
mono_threads_platform_register (MonoThreadInfo *info)
{
	g_assert (!info->handle);
	info->handle = thread_handle_create ();
}

typedef struct {
	void *(*start_routine)(void*);
	void *arg;
	int flags;
	gint32 priority;
	MonoCoopSem registered;
	HANDLE handle;
} StartInfo;

static void*
inner_start_thread (void *arg)
{
	StartInfo *start_info = (StartInfo *) arg;
	void *t_arg = start_info->arg;
	int res;
	void *(*start_func)(void*) = start_info->start_routine;
	guint32 flags = start_info->flags;
	void *result;
	MonoThreadInfo *info;

	info = mono_thread_info_attach (&result);
	info->runtime_thread = TRUE;

	start_info->handle = info->handle;

	mono_threads_platform_set_priority (info, start_info->priority);

	if (flags & CREATE_SUSPENDED) {
		info->create_suspended = TRUE;
		mono_coop_sem_init (&info->create_suspended_sem, 0);
	}

	/* start_info is not valid after this */
	mono_coop_sem_post (&(start_info->registered));
	start_info = NULL;

	if (flags & CREATE_SUSPENDED) {
		res = mono_coop_sem_wait (&info->create_suspended_sem, MONO_SEM_FLAGS_NONE);
		g_assert (res != -1);

		mono_coop_sem_destroy (&info->create_suspended_sem);
	}

	/* Run the actual main function of the thread */
	result = start_func (t_arg);

	mono_threads_platform_exit (GPOINTER_TO_UINT (result));
	g_assert_not_reached ();
}

HANDLE
mono_threads_platform_create_thread (MonoThreadStart start_routine, gpointer arg, MonoThreadParm *tp, MonoNativeThreadId *out_tid)
{
	pthread_attr_t attr;
	int res;
	pthread_t thread;
	StartInfo start_info;
	guint32 stack_size;
	int policy;
	struct sched_param sp;

	res = pthread_attr_init (&attr);
	g_assert (!res);

	if (tp->stack_size == 0) {
#if HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND)
			stack_size = 1 << 20;
		else
			stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#else
		stack_size = (SIZEOF_VOID_P / 4) * 1024 * 1024;
#endif
	} else
		stack_size = tp->stack_size;

#ifdef PTHREAD_STACK_MIN
	if (stack_size < PTHREAD_STACK_MIN)
		stack_size = PTHREAD_STACK_MIN;
#endif

#ifdef HAVE_PTHREAD_ATTR_SETSTACKSIZE
	res = pthread_attr_setstacksize (&attr, stack_size);
	g_assert (!res);
#endif

	/*
	 * For policies that respect priorities set the prirority for the new thread
	 */ 
	pthread_getschedparam(pthread_self(), &policy, &sp);
	if ((policy == SCHED_FIFO) || (policy == SCHED_RR)) {
		sp.sched_priority = win32_priority_to_posix_priority (tp->priority, policy);
		res = pthread_attr_setschedparam (&attr, &sp);
	}

	memset (&start_info, 0, sizeof (StartInfo));
	start_info.start_routine = (void *(*)(void *)) start_routine;
	start_info.arg = arg;
	start_info.flags = tp->creation_flags;
	start_info.priority = tp->priority;
	mono_coop_sem_init (&(start_info.registered), 0);

	/* Actually start the thread */
	res = mono_gc_pthread_create (&thread, &attr, inner_start_thread, &start_info);
	if (res) {
		mono_coop_sem_destroy (&(start_info.registered));
		return NULL;
	}

	/* Wait until the thread register itself in various places */
	res = mono_coop_sem_wait (&start_info.registered, MONO_SEM_FLAGS_NONE);
	g_assert (res != -1);

	mono_coop_sem_destroy (&(start_info.registered));

	if (out_tid)
		*out_tid = thread;

	return start_info.handle;
}

gboolean
mono_threads_platform_yield (void)
{
	return sched_yield () == 0;
}

void
mono_threads_platform_exit (int exit_code)
{
#if defined(__native_client__)
	nacl_shutdown_gc_thread();
#endif

	mono_thread_info_detach ();

	pthread_exit (NULL);
}

void
mono_threads_platform_unregister (MonoThreadInfo *info)
{
	mono_threads_platform_set_exited (info);
}

int
mono_threads_get_max_stack_size (void)
{
	struct rlimit lim;

	/* If getrlimit fails, we don't enforce any limits. */
	if (getrlimit (RLIMIT_STACK, &lim))
		return INT_MAX;
	/* rlim_t is an unsigned long long on 64bits OSX but we want an int response. */
	if (lim.rlim_max > (rlim_t)INT_MAX)
		return INT_MAX;
	return (int)lim.rlim_max;
}

HANDLE
mono_threads_platform_open_thread_handle (HANDLE handle, MonoNativeThreadId tid)
{
	mono_w32handle_ref (handle);

	return handle;
}

int
mono_threads_pthread_kill (MonoThreadInfo *info, int signum)
{
	THREADS_SUSPEND_DEBUG ("sending signal %d to %p[%p]\n", signum, info, mono_thread_info_get_tid (info));
#ifdef USE_TKILL_ON_ANDROID
	int result, old_errno = errno;
	result = tkill (info->native_handle, signum);
	if (result < 0) {
		result = errno;
		errno = old_errno;
	}
	return result;
#elif defined(__native_client__)
	/* Workaround pthread_kill abort() in NaCl glibc. */
	return 0;
#elif !defined(HAVE_PTHREAD_KILL)
	g_error ("pthread_kill() is not supported by this platform");
#else
	return pthread_kill (mono_thread_info_get_tid (info), signum);
#endif
}

MonoNativeThreadId
mono_native_thread_id_get (void)
{
	return pthread_self ();
}

gboolean
mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
	return pthread_equal (id1, id2);
}

/*
 * mono_native_thread_create:
 *
 *   Low level thread creation function without any GC wrappers.
 */
gboolean
mono_native_thread_create (MonoNativeThreadId *tid, gpointer func, gpointer arg)
{
	return pthread_create (tid, NULL, (void *(*)(void *)) func, arg) == 0;
}

void
mono_native_thread_set_name (MonoNativeThreadId tid, const char *name)
{
#ifdef __MACH__
	/*
	 * We can't set the thread name for other threads, but we can at least make
	 * it work for threads that try to change their own name.
	 */
	if (tid != mono_native_thread_id_get ())
		return;

	if (!name) {
		pthread_setname_np ("");
	} else {
		char n [63];

		strncpy (n, name, 63);
		n [62] = '\0';
		pthread_setname_np (n);
	}
#elif defined (__NetBSD__)
	if (!name) {
		pthread_setname_np (tid, "%s", (void*)"");
	} else {
		char n [PTHREAD_MAX_NAMELEN_NP];

		strncpy (n, name, PTHREAD_MAX_NAMELEN_NP);
		n [PTHREAD_MAX_NAMELEN_NP - 1] = '\0';
		pthread_setname_np (tid, "%s", (void*)n);
	}
#elif defined (HAVE_PTHREAD_SETNAME_NP)
	if (!name) {
		pthread_setname_np (tid, "");
	} else {
		char n [16];

		strncpy (n, name, 16);
		n [15] = '\0';
		pthread_setname_np (tid, n);
	}
#endif
}

void
mono_threads_platform_set_exited (MonoThreadInfo *info)
{
	MonoW32HandleThread *thread_data;
	gpointer mutex_handle;
	int i, thr_ret;
	pid_t pid;
	pthread_t tid;

	g_assert (info->handle);

	if (mono_w32handle_issignalled (info->handle) || mono_w32handle_get_type (info->handle) == MONO_W32HANDLE_UNUSED) {
		/* We must have already deliberately finished
		 * with this thread, so don't do any more now */
		return;
	}

	if (!mono_w32handle_lookup (info->handle, MONO_W32HANDLE_THREAD, (gpointer*) &thread_data))
		g_error ("unknown thread handle %p", info->handle);

	pid = wapi_getpid ();
	tid = pthread_self ();

	for (i = 0; i < thread_data->owned_mutexes->len; i++) {
		mutex_handle = g_ptr_array_index (thread_data->owned_mutexes, i);
		wapi_mutex_abandon (mutex_handle, pid, tid);
		mono_thread_info_disown_mutex (info, mutex_handle);
	}

	g_ptr_array_free (thread_data->owned_mutexes, TRUE);

	thr_ret = mono_w32handle_lock_handle (info->handle);
	g_assert (thr_ret == 0);

	mono_w32handle_set_signal_state (info->handle, TRUE, TRUE);

	thr_ret = mono_w32handle_unlock_handle (info->handle);
	g_assert (thr_ret == 0);

	/* The thread is no longer active, so unref it */
	mono_w32handle_unref (info->handle);

	info->handle = NULL;
}

void
mono_threads_platform_describe (MonoThreadInfo *info, GString *text)
{
	MonoW32HandleThread *thread_data;
	int i;

	g_assert (info->handle);

	if (!mono_w32handle_lookup (info->handle, MONO_W32HANDLE_THREAD, (gpointer*) &thread_data))
		g_error ("unknown thread handle %p", info->handle);

	g_string_append_printf (text, "thread handle %p state : ", info->handle);

	mono_thread_info_describe_interrupt_token (info, text);

	g_string_append_printf (text, ", owns (");
	for (i = 0; i < thread_data->owned_mutexes->len; i++)
		g_string_append_printf (text, i > 0 ? ", %p" : "%p", g_ptr_array_index (thread_data->owned_mutexes, i));
	g_string_append_printf (text, ")");
}

void
mono_threads_platform_own_mutex (MonoThreadInfo *info, gpointer mutex_handle)
{
	MonoW32HandleThread *thread_data;

	g_assert (info->handle);

	if (!mono_w32handle_lookup (info->handle, MONO_W32HANDLE_THREAD, (gpointer*) &thread_data))
		g_error ("unknown thread handle %p", info->handle);

	mono_w32handle_ref (mutex_handle);

	g_ptr_array_add (thread_data->owned_mutexes, mutex_handle);
}

void
mono_threads_platform_disown_mutex (MonoThreadInfo *info, gpointer mutex_handle)
{
	MonoW32HandleThread *thread_data;

	g_assert (info->handle);

	if (!mono_w32handle_lookup (info->handle, MONO_W32HANDLE_THREAD, (gpointer*) &thread_data))
		g_error ("unknown thread handle %p", info->handle);

	mono_w32handle_unref (mutex_handle);

	g_ptr_array_remove (thread_data->owned_mutexes, mutex_handle);
}

MonoThreadPriority
mono_threads_platform_get_priority (MonoThreadInfo *info)
{
	MonoW32HandleThread *thread_data;

	g_assert (info->handle);

	if (!mono_w32handle_lookup (info->handle, MONO_W32HANDLE_THREAD, (gpointer *)&thread_data))
		return MONO_THREAD_PRIORITY_NORMAL;

	return thread_data->priority;
}

gboolean
mono_threads_platform_set_priority (MonoThreadInfo *info, MonoThreadPriority priority)
{
	MonoW32HandleThread *thread_data;
	int policy, posix_priority;
	struct sched_param param;

	g_assert (info->handle);

	if (!mono_w32handle_lookup (info->handle, MONO_W32HANDLE_THREAD, (gpointer*) &thread_data))
		return FALSE;

	switch (pthread_getschedparam (thread_data->id, &policy, &param)) {
	case 0:
		break;
	case ESRCH:
		g_warning ("pthread_getschedparam: error looking up thread id %x", (gsize)thread_data->id);
		return FALSE;
	default:
		return FALSE;
	}

	posix_priority =  win32_priority_to_posix_priority (priority, policy);
	if (posix_priority < 0)
		return FALSE;

	param.sched_priority = posix_priority;
	switch (pthread_setschedparam (thread_data->id, policy, &param)) {
	case 0:
		break;
	case ESRCH:
		g_warning ("%s: pthread_setschedprio: error looking up thread id %x", __func__, (gsize)thread_data->id);
		return FALSE;
	case ENOTSUP:
		g_warning ("%s: priority %d not supported", __func__, priority);
		return FALSE;
	case EPERM:
		g_warning ("%s: permission denied", __func__);
		return FALSE;
	default:
		return FALSE;
	}

	thread_data->priority = priority;
	return TRUE;

}

static void thread_details (gpointer data)
{
	MonoW32HandleThread *thread = (MonoW32HandleThread*) data;
	g_print ("id: %p, owned_mutexes: %d, priority: %d",
		thread->id, thread->owned_mutexes->len, thread->priority);
}

static const gchar* thread_typename (void)
{
	return "Thread";
}

static gsize thread_typesize (void)
{
	return sizeof (MonoW32HandleThread);
}

static MonoW32HandleOps thread_ops = {
	NULL,				/* close */
	NULL,				/* signal */
	NULL,				/* own */
	NULL,				/* is_owned */
	NULL,				/* special_wait */
	NULL,				/* prewait */
	thread_details,		/* details */
	thread_typename,	/* typename */
	thread_typesize,	/* typesize */
};

void
mono_threads_platform_init (void)
{
	mono_w32handle_register_ops (MONO_W32HANDLE_THREAD, &thread_ops);

	mono_w32handle_register_capabilities (MONO_W32HANDLE_THREAD, MONO_W32HANDLE_CAP_WAIT);
}

#endif /* defined(_POSIX_VERSION) || defined(__native_client__) */

#if defined(USE_POSIX_BACKEND)

gboolean
mono_threads_suspend_begin_async_suspend (MonoThreadInfo *info, gboolean interrupt_kernel)
{
	int sig = interrupt_kernel ? mono_threads_posix_get_abort_signal () :  mono_threads_posix_get_suspend_signal ();

	if (!mono_threads_pthread_kill (info, sig)) {
		mono_threads_add_to_pending_operation_set (info);
		return TRUE;
	}
	return FALSE;
}

gboolean
mono_threads_suspend_check_suspend_result (MonoThreadInfo *info)
{
	return info->suspend_can_continue;
}

/*
This begins async resume. This function must do the following:

- Install an async target if one was requested.
- Notify the target to resume.
*/
gboolean
mono_threads_suspend_begin_async_resume (MonoThreadInfo *info)
{
	mono_threads_add_to_pending_operation_set (info);
	return mono_threads_pthread_kill (info, mono_threads_posix_get_restart_signal ()) == 0;
}

void
mono_threads_suspend_register (MonoThreadInfo *info)
{
#if defined (PLATFORM_ANDROID)
	info->native_handle = gettid ();
#endif
}

void
mono_threads_suspend_free (MonoThreadInfo *info)
{
}

void
mono_threads_suspend_init (void)
{
	mono_threads_posix_init_signals (MONO_THREADS_POSIX_INIT_SIGNALS_SUSPEND_RESTART);
}

#endif /* defined(USE_POSIX_BACKEND) */
