#include <config.h>

#ifdef	HAVE_BOEHM_GC
#ifdef	HAVE_GC_GC_H
#include <gc/gc.h>
#endif
#ifdef	HAVE_GC_H
#include <gc.h>
#endif
#else	/* HAVE_BOEHM_GC */
#ifdef	GC_I_HIDE_POINTERS
#define	HIDE_POINTER(v)		(v)
#define	REVEAL_POINTER(v)	(v)
#endif
#endif
