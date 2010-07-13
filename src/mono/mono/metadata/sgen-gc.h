/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#ifndef __MONO_SGENGC_H__
#define __MONO_SGENGC_H__

/* pthread impl */
#include "config.h"
#include <glib.h>
#include <pthread.h>
#include <signal.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/class-internals.h>

/* #define SGEN_PARALLEL_MARK */

#define THREAD_HASH_SIZE 11

#define ARCH_THREAD_TYPE pthread_t
#define ARCH_GET_THREAD pthread_self
#define ARCH_THREAD_EQUALS(a,b) pthread_equal (a, b)

#if SIZEOF_VOID_P == 4
typedef guint32 mword;
#else
typedef guint64 mword;
#endif

/* for use with write barriers */
typedef struct _RememberedSet RememberedSet;
struct _RememberedSet {
	mword *store_next;
	mword *end_set;
	RememberedSet *next;
	mword data [MONO_ZERO_LEN_ARRAY];
};

/* eventually share with MonoThread? */
typedef struct _SgenThreadInfo SgenThreadInfo;

struct _SgenThreadInfo {
	SgenThreadInfo *next;
	ARCH_THREAD_TYPE id;
	unsigned int stop_count; /* to catch duplicate signals */
	int signal;
	int skip;
	volatile int in_critical_region;
	void *stack_end;
	void *stack_start;
	void *stack_start_limit;
	char **tlab_next_addr;
	char **tlab_start_addr;
	char **tlab_temp_end_addr;
	char **tlab_real_end_addr;
	gpointer **store_remset_buffer_addr;
	long *store_remset_buffer_index_addr;
	RememberedSet *remset;
	gpointer runtime_data;
	gpointer stopped_ip;	/* only valid if the thread is stopped */
	MonoDomain *stopped_domain; /* ditto */
	gpointer *stopped_regs;	    /* ditto */
#ifndef HAVE_KW_THREAD
	char *tlab_start;
	char *tlab_next;
	char *tlab_temp_end;
	char *tlab_real_end;
	gpointer *store_remset_buffer;
	long store_remset_buffer_index;
#endif
};

#ifdef __APPLE__
static int suspend_signal_num = SIGXFSZ;
#else
static int suspend_signal_num = SIGPWR;
#endif
static int restart_signal_num = SIGXCPU;

/*
 * Recursion is not allowed for the thread lock.
 */
#define LOCK_DECLARE(name) pthread_mutex_t name = PTHREAD_MUTEX_INITIALIZER
#define LOCK_INIT(name)
#define LOCK_GC pthread_mutex_lock (&gc_mutex)
#define UNLOCK_GC pthread_mutex_unlock (&gc_mutex)
#define LOCK_INTERRUPTION pthread_mutex_lock (&interruption_mutex)
#define UNLOCK_INTERRUPTION pthread_mutex_unlock (&interruption_mutex)

#ifdef SGEN_PARALLEL_MARK
#define LOCK_INTERNAL_ALLOCATOR pthread_mutex_lock (&internal_allocator_mutex)
#define UNLOCK_INTERNAL_ALLOCATOR pthread_mutex_unlock (&internal_allocator_mutex)
#else
#define LOCK_INTERNAL_ALLOCATOR
#define UNLOCK_INTERNAL_ALLOCATOR
#endif

#ifdef SGEN_PARALLEL_MARK
#define SGEN_CAS_PTR	InterlockedCompareExchangePointer
#define SGEN_ATOMIC_ADD(x,i)	do {					\
		int __old_x;						\
		do {							\
			__old_x = (x);					\
		} while (InterlockedCompareExchange (&(x), __old_x, __old_x + (i)) != __old_x); \
	} while (0)
#else
#define SGEN_CAS_PTR(p,n,c)	((*(void**)(p) == (void*)(c)) ? (*(void**)(p) = (void*)(n), (void*)(c)) : (*(void**)(p)))
#define SGEN_ATOMIC_ADD(x,i)	((x) += (i))
#endif

/* non-pthread will need to provide their own version of start/stop */
#define USE_SIGNAL_BASED_START_STOP_WORLD 1
/* we intercept pthread_create calls to know which threads exist */
#define USE_PTHREAD_INTERCEPT 1

#define MAX_DEBUG_LEVEL 2
#define DEBUG(level,a) do {if (G_UNLIKELY ((level) <= MAX_DEBUG_LEVEL && (level) <= gc_debug_level)) a;} while (0)

int mono_sgen_thread_handshake (int signum) MONO_INTERNAL;
SgenThreadInfo* mono_sgen_thread_info_lookup (ARCH_THREAD_TYPE id) MONO_INTERNAL;
SgenThreadInfo** mono_sgen_get_thread_table (void) MONO_INTERNAL;
void mono_sgen_wait_for_suspend_ack (int count) MONO_INTERNAL;

gboolean mono_sgen_is_worker_thread (pthread_t thread) MONO_INTERNAL;

#endif /* __MONO_SGENGC_H__ */

