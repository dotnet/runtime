#include <config.h>

#ifdef	HAVE_BOEHM_GC
#include <gc/gc.h>
#else	/* HAVE_BOEHM_GC */
#ifdef	GC_I_HIDE_POINTERS
#define	HIDE_POINTER(v)		(v)
#define	REVEAL_POINTER(v)	(v)
#endif
#endif
