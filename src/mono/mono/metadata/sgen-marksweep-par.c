#include "config.h"

#ifndef DISABLE_SGEN_MAJOR_MARKSWEEP_PAR

#define SGEN_PARALLEL_MARK

#include "sgen-marksweep.c"

#else

#include "metadata/sgen-gc.h"

void
sgen_marksweep_par_init (SgenMajorCollector *collector)
{
	fprintf (stderr, "Error: Mono was configured using --enable-minimal=sgen_marksweep_par.\n");
	exit (1);
}	

#endif /* DISABLE_SGEN_MAJOR_MARKSWEEP_PAR */
