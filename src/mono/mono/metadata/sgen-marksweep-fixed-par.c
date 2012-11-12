#include "config.h"

#ifdef HAVE_SGEN_GC

#ifndef DISABLE_SGEN_MARKSWEEP_FIXED_PAR

#define SGEN_PARALLEL_MARK
#define FIXED_HEAP

#include "sgen-marksweep.c"

#else

#include "metadata/sgen-gc.h"

void
sgen_marksweep_fixed_par_init (SgenMajorCollector *collector)
{
	fprintf (stderr, "Error: Mono was configured using --enable-minimal=sgen_marksweep_fixed_par.\n");
	exit (1);
}	

#endif

#endif
