#include <config.h>

#ifdef HAVE_BOEHM_GC

#ifdef	HAVE_GC_GC_H
#include <gc/gc.h>
#else

#ifdef	HAVE_GC_H
#include <gc.h>
#else
#error have boehm GC without headers, you probably need to install them by hand
#endif

#endif

#endif
