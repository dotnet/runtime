/**
 * \file
 * Copyright 2004-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
#ifndef HOST_WIN32 // FIXME?
#	if defined(MONO_KEYWORD_THREAD) && !defined(__powerpc__)

        /* The local alloc stuff is in pthread_support.c, but solaris uses solaris_threads.c */
        /* It is also disabled on solaris/x86 by libgc/configure.ac */
        /*
		 * ARM has no definition for some atomic functions in gc_locks.h and
		 * support is also disabled in libgc/configure.ac.
		 */
#       if !defined(__sparc__) && !defined(__sun) && !defined(__arm__) && !defined(__mips__)
#		    define GC_REDIRECT_TO_LOCAL
#       endif
#	endif
#endif // HOST_WIN32

#	include <gc.h>
#	include <gc_typed.h>
#	include <gc_mark.h>
#	include <gc_gcj.h>

#if defined(HOST_WIN32)
#define CreateThread GC_CreateThread
#endif

#elif defined(HAVE_SGEN_GC)

#else /* not Boehm and not sgen GC */
#endif

#endif
