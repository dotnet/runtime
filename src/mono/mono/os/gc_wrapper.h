#include <config.h>

#ifdef HAVE_BOEHM_GC

#ifdef	HAVE_GC_GC_H
#include <gc/gc.h>
#include <gc/gc_typed.h>
#include <gc/gc_gcj.h>
#include <gc/gc_mark.h>
#else

#ifdef	HAVE_GC_H
#include <gc.h>
#include <gc_typed.h>
#include <gc_gcj.h>
#include <gc_mark.h>
#else
#error have boehm GC without headers, you probably need to install them by hand
#endif

#endif

#endif
