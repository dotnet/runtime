/*
 * Portions Copyright (c) 1987, 1993, 1994
 * The Regents of the University of California.  All rights reserved.
 *
 * Portions Copyright (c) 2003-2007, PostgreSQL Global Development Group
 *
 * $PostgreSQL: pgsql/src/include/getopt_long.h,v 1.8 2007/01/05 22:19:50 momjian Exp $
 */
#ifndef GETOPT_LONG_H
#define GETOPT_LONG_H

#ifdef HAVE_GETOPT_H
#include <getopt.h>
#endif

/* These are picked up from the system's getopt() facility. */
extern int	opterr;
extern int	optind;
extern int	optopt;
extern char *optarg;

/* Some systems have this, otherwise you need to define it somewhere. */
extern int	optreset;

#ifndef HAVE_STRUCT_OPTION

struct option
{
	const char *name;
	int			has_arg;
	int		   *flag;
	int			val;
};

#define no_argument 0
#define required_argument 1
#endif

#ifndef HAVE_GETOPT_LONG

extern int getopt_long(int argc, char *const argv[],
			const char *optstring,
			const struct option * longopts, int *longindex);
#endif

#endif   /* GETOPT_LONG_H */
