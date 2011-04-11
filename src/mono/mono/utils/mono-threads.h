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

#include <glib.h>

#ifdef HOST_WIN32

#include <windows.h>

typedef DWORD MonoNativeThreadId;

#define mono_native_thread_id_get GetCurrentThreadId
#define mono_native_thread_id_equals(a,b) ((a) == ((b))

#else

#include <pthread.h>

#if defined(__MACH__)
#include <mono/utils/mach-support.h>
#endif

typedef pthread_t MonoNativeThreadId;

#define mono_native_thread_id_get pthread_self
#define mono_native_thread_id_equals(a,b) pthread_equal((a),(b))

#endif /* #ifdef HOST_WIN32 */

#ifndef THREAD_INFO_TYPE
#define THREAD_INFO_TYPE MonoThreadInfo
#endif

typedef struct {
	MonoLinkedListSetNode node;
	guint32 small_id; /*Used by hazard pointers */
} MonoThreadInfo;

typedef struct {
	void* (*thread_register)(THREAD_INFO_TYPE *info, void *baseaddr);
	void (*thread_unregister)(THREAD_INFO_TYPE *info);
	void (*thread_attach)(THREAD_INFO_TYPE *info);
} MonoThreadInfoCallbacks;

/*
Requires the world to be stoped
*/
#define FOREACH_THREAD(thread) MONO_LLS_FOREACH (mono_thread_info_list_head (), thread)
#define END_FOREACH_THREAD MONO_LLS_END_FOREACH

/*
Snapshot iteration.
*/
#define FOREACH_THREAD_SAFE(thread) MONO_LLS_FOREACH_SAFE (mono_thread_info_list_head (), thread)
#define END_FOREACH_THREAD_SAFE MONO_LLS_END_FOREACH_SAFE

#define mono_thread_info_get_tid(info) ((MonoNativeThreadId)((MonoThreadInfo*)info)->node.key)
#define mono_thread_info_set_tid(info, val) do { ((MonoThreadInfo*)(info))->node.key = (uintptr_t)(val); } while (0)

/*
 * @thread_info_size is sizeof (GcThreadInfo), a struct the GC defines to make it possible to have
 * a single block with info from both camps. 
 */
void
mono_threads_init (MonoThreadInfoCallbacks *callbacks, size_t thread_info_size) MONO_INTERNAL;


THREAD_INFO_TYPE *
mono_thread_info_attach (void *baseptr) MONO_INTERNAL;

THREAD_INFO_TYPE *
mono_thread_info_current (void) MONO_INTERNAL;

MonoLinkedListSet*
mono_thread_info_list_head (void) MONO_INTERNAL;

MonoThreadInfo*
mono_thread_info_lookup (MonoNativeThreadId id) MONO_INTERNAL;

#if defined(HOST_WIN32)

gpointer
mono_threads_wthread_create (LPSECURITY_ATTRIBUTES security, guint32 stacksize, LPTHREAD_START_ROUTINE start, gpointer param, guint32 create, LPDWORD tid) MONO_INTERNAL;

#else
int
mono_threads_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg) MONO_INTERNAL;

#endif
#endif /* __MONO_THREADS_H__ */
