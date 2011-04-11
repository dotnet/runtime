/*
 * mono-threads.c: Low-level threading
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2011 Novell, Inc
 */

#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-semaphore.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/appdomain.h>

#include <errno.h>


#define THREADS_DEBUG(...)
//#define THREADS_DEBUG(...) g_message(__VA_ARGS__)

typedef struct {
	void *(*start_routine) (void *);
	void *arg;
	int flags;
	MonoSemType registered;
} ThreadStartInfo;

static int thread_info_size;
static MonoThreadInfoCallbacks threads_callbacks;
static MonoNativeTlsKey thread_info_key;
static MonoLinkedListSet thread_list;

static inline void
mono_hazard_pointer_clear_all (MonoThreadHazardPointers *hp, int retain)
{
	if (retain != 0)
		mono_hazard_pointer_clear (hp, 0);
	if (retain != 1)
		mono_hazard_pointer_clear (hp, 1);
	if (retain != 2)
		mono_hazard_pointer_clear (hp, 2);
}

static gboolean
mono_thread_info_insert (MonoThreadInfo *info)
{
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get ();

	if (!mono_lls_insert (&thread_list, hp, (MonoLinkedListSetNode*)info)) {
		mono_hazard_pointer_clear_all (hp, -1);
		return FALSE;
	} 

	mono_hazard_pointer_clear_all (hp, -1);
	return TRUE;
}

static gboolean
mono_thread_info_remove (MonoThreadInfo *info)
{
	/*TLS is gone by now, so we can't rely on it to retrieve hp*/
	MonoThreadHazardPointers *hp = mono_hazard_pointer_get_by_id (info->small_id);
	gboolean res;

	THREADS_DEBUG ("removing info %p\n", info);
	res = mono_lls_remove (&thread_list, hp, (MonoLinkedListSetNode*)info);
	mono_hazard_pointer_clear_all (hp, -1);
	return res;
}

static void
free_thread_info (gpointer mem)
{
	MonoThreadInfo *info = mem;

	g_free (info);
}

static void*
register_thread (MonoThreadInfo *info, gpointer baseptr)
{
	gboolean result;
	mono_thread_info_set_tid (info, mono_native_thread_id_get ());
	info->small_id = mono_thread_small_id_alloc ();

	THREADS_DEBUG ("registering info %p tid %p small id %x\n", info, mono_thread_info_get_tid (info), info->small_id);

	if (threads_callbacks.thread_register) {
		if (threads_callbacks.thread_register (info, baseptr) == NULL) {
			g_warning ("thread registation failed\n");
			g_free (info);
			return NULL;
		}
	}

	mono_native_tls_set_value (thread_info_key, info);

	/*If this fail it means a given thread has been registered twice, which doesn't make sense. */
	result = mono_thread_info_insert (info);
	g_assert (result);
	return info;
}

static void
unregister_thread (void *arg)
{
	gboolean result;
	MonoThreadInfo *info = arg;
	int small_id = info->small_id;
	g_assert (info);

	THREADS_DEBUG ("unregistering info %p\n", info);

	if (threads_callbacks.thread_unregister)
		threads_callbacks.thread_unregister (info);

	result = mono_thread_info_remove (info);
	g_assert (result);

	mono_thread_small_id_free (small_id);
}

static void*
inner_start_thread (void *arg)
{
	ThreadStartInfo *start_info = arg;
	MonoThreadInfo* info;
	void *t_arg = start_info->arg;
	void *(*start_func) (void*) = start_info->start_routine;
	void *result;
	int post_result;

	info = g_malloc0 (thread_info_size);

	register_thread (info, &post_result);

	post_result = MONO_SEM_POST (&(start_info->registered));
	g_assert (!post_result);

	result = start_func (t_arg);
	g_assert (!mono_domain_get ());

	return result;
}

MonoThreadInfo*
mono_thread_info_current (void)
{
	return mono_native_tls_get_value (thread_info_key);
}

MonoLinkedListSet*
mono_thread_info_list_head (void)
{
	return &thread_list;
}

MonoThreadInfo*
mono_thread_info_attach (void *baseptr)
{
	MonoThreadInfo *info = mono_native_tls_get_value (thread_info_key);
	if (!info) {
		info = g_malloc0 (thread_info_size);
		if (!register_thread (info, baseptr))
			return NULL;
	} else if (threads_callbacks.thread_attach) {
		threads_callbacks.thread_attach (info);
	}
	return info;
}

void
mono_threads_init (MonoThreadInfoCallbacks *callbacks, size_t info_size)
{
	gboolean res;
	threads_callbacks = *callbacks;
	thread_info_size = info_size;
	res = mono_native_tls_alloc (&thread_info_key, unregister_thread);
	mono_lls_init (&thread_list, free_thread_info);
	mono_thread_smr_init ();

	g_assert (res);
	g_assert (sizeof (MonoNativeThreadId) == sizeof (uintptr_t));
}

int
mono_threads_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg)
{
	ThreadStartInfo *start_info;
	int result;

	start_info = g_malloc0 (sizeof (ThreadStartInfo));
	if (!start_info)
		return ENOMEM;
	MONO_SEM_INIT (&(start_info->registered), 0);
	start_info->arg = arg;
	start_info->start_routine = start_routine;

	result = pthread_create (new_thread, attr, inner_start_thread, start_info);
	if (result == 0) {
		while (MONO_SEM_WAIT (&(start_info->registered)) != 0) {
			/*if (EINTR != errno) ABORT("sem_wait failed"); */
		}
	}
	MONO_SEM_DESTROY (&(start_info->registered));
	g_free (start_info);
	return result;
}
