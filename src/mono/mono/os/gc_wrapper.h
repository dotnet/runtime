#ifndef __MONO_OS_GC_WRAPPER_H__
#define __MONO_OS_GC_WRAPPER_H__

#include <config.h>

#ifdef HAVE_BOEHM_GC

#ifdef	HAVE_GC_GC_H
#include <gc/gc.h>
#include <gc/gc_typed.h>
#include <gc/gc_gcj.h>
#include <gc/gc_mark.h>
#else

#if defined(HAVE_GC_H) || defined(USE_INCLUDED_LIBGC)
#include <gc.h>
#include <gc_typed.h>
#include <gc_gcj.h>
#include <gc_mark.h>
#else
#error have boehm GC without headers, you probably need to install them by hand
#endif

#endif
/* for some strange resion, they want one extra byte on the end */
#define MONO_GC_REGISTER_ROOT(x) \
	GC_add_roots ((char*)&(x), (char*)&(x) + sizeof (x) + 1)

#else

#define MONO_GC_REGISTER_ROOT(x) /* nop */
#endif
#endif
