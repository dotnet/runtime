/* 
 * Copyright 2004-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */
#ifndef __MONO_OS_GC_WRAPPER_H__
#define __MONO_OS_GC_WRAPPER_H__

#include <config.h>
#include <stdlib.h>

#ifdef HAVE_BOEHM_GC

#	ifdef _MSC_VER
#		include <winsock2.h>
#	else
		/* libgc specifies this on the command line,
		 * so we must define it ourselfs
		*/
#		define GC_GCJ_SUPPORT
#	endif

	/*
	 * Local allocation is only beneficial if we have __thread
	 * We had to fix a bug with include order in libgc, so only do
	 * it if it is the included one.
	 */
	
#	if defined(HAVE_KW_THREAD) && defined(USE_INCLUDED_LIBGC) && !defined(__powerpc__)
        /* The local alloc stuff is in pthread_support.c, but solaris uses solaris_threads.c */
        /* It is also disabled on solaris/x86 by libgc/configure.in */
        /* 
		 * ARM has no definition for some atomic functions in gc_locks.h and
		 * support is also disabled in libgc/configure.in.
		 */
#       if !defined(__sparc__) && !defined(__sun) && !defined(__arm__) && !defined(__mips__)
#		    define GC_REDIRECT_TO_LOCAL
#       endif
#	endif

#	ifdef HAVE_GC_GC_H
#		include <gc/gc.h>
#		include <gc/gc_typed.h>
#		include <gc/gc_mark.h>
#		include <gc/gc_gcj.h>
#	elif defined(HAVE_GC_H) || defined(USE_INCLUDED_LIBGC)
#		include <gc.h>
#		include <gc_typed.h>
#		include <gc_mark.h>
#		include <gc_gcj.h>
#	else
#		error have boehm GC without headers, you probably need to install them by hand
#	endif

#if defined(HOST_WIN32)
#define CreateThread GC_CreateThread
#endif

#elif defined(HAVE_SGEN_GC)

#else /* not Boehm and not sgen GC */
#endif

#if !defined(HOST_WIN32)

/*
 * Both Boehm and SGEN needs to intercept some thread operations. So instead of the
 * pthread_... calls, runtime code should call these wrappers.
 */

/* pthread function wrappers */
#include <pthread.h>
#include <signal.h>

int mono_gc_pthread_create (pthread_t *new_thread, const pthread_attr_t *attr, void *(*start_routine)(void *), void *arg);
int mono_gc_pthread_join (pthread_t thread, void **retval);
int mono_gc_pthread_detach (pthread_t thread);
void mono_gc_pthread_exit (void *retval) G_GNUC_NORETURN;

#endif

#endif
