#ifndef __MONO_OS_GC_WRAPPER_H__
#define __MONO_OS_GC_WRAPPER_H__

#include <config.h>

#ifdef HAVE_BOEHM_GC

	/* libgc specifies this on the command line,
	 * so we must define it ourselfs
	 */
#	define GC_GCJ_SUPPORT
	/*
	 * Local allocation is only beneficial if we have __thread
	 * We had to fix a bug with include order in libgc, so only do
	 * it if it is the included one.
	 */
	
#	if defined(HAVE_KW_THREAD) || defined(USE_INCLUDED_LIBGC)  
#		define GC_REDIRECT_TO_LOCAL
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
	/* for some strange resion, they want one extra byte on the end */
#	define MONO_GC_REGISTER_ROOT(x) \
		GC_add_roots ((char*)&(x), (char*)&(x) + sizeof (x) + 1)
	/* this needs to be called before ANY gc allocations. We can't use
	 * mono_gc_init here because we need to make allocations before that
	 * function is called 
	 */
#	define MONO_GC_PRE_INIT() GC_init ()

#else
#	define MONO_GC_REGISTER_ROOT(x) /* nop */
#	define MONO_GC_PRE_INIT()
#endif

#endif
