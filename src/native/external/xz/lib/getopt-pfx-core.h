/* SPDX-License-Identifier: LGPL-2.1-or-later */

/* getopt (basic, portable features) gnulib wrapper header.
   Copyright (C) 1989-2023 Free Software Foundation, Inc.
   This file is part of gnulib.
   Unlike most of the getopt implementation, it is NOT shared
   with the GNU C Library.

   This file is free software: you can redistribute it and/or modify
   it under the terms of the GNU Lesser General Public License as
   published by the Free Software Foundation; either version 2.1 of the
   License, or (at your option) any later version.

   This file is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU Lesser General Public License for more details.

   You should have received a copy of the GNU Lesser General Public License
   along with this program.  If not, see <https://www.gnu.org/licenses/>.  */

#ifndef _GETOPT_PFX_CORE_H
#define _GETOPT_PFX_CORE_H 1

/* This header should not be used directly; include getopt.h or
   unistd.h instead.  It does not have a protective #error, because
   the guard macro for getopt.h in gnulib is not fixed.  */

/* Standalone applications should #define __GETOPT_PREFIX to an
   identifier that prefixes the external functions and variables
   defined in getopt-core.h and getopt-ext.h.  Systematically
   rename identifiers so that they do not collide with the system
   functions and variables.  Renaming avoids problems with some
   compilers and linkers.  */
#ifdef __GETOPT_PREFIX
# ifndef __GETOPT_ID
#  define __GETOPT_CONCAT(x, y) x ## y
#  define __GETOPT_XCONCAT(x, y) __GETOPT_CONCAT (x, y)
#  define __GETOPT_ID(y) __GETOPT_XCONCAT (__GETOPT_PREFIX, y)
# endif
# undef getopt
# undef optarg
# undef opterr
# undef optind
# undef optopt
# define getopt __GETOPT_ID (getopt)
# define optarg __GETOPT_ID (optarg)
# define opterr __GETOPT_ID (opterr)
# define optind __GETOPT_ID (optind)
# define optopt __GETOPT_ID (optopt)

/* Work around a problem on macOS, which declares getopt with a
   trailing __DARWIN_ALIAS(getopt) that would expand to something like
   __asm("_" "rpl_getopt" "$UNIX2003") were it not for the following
   hack to suppress the macOS declaration <https://bugs.gnu.org/40205>.  */
# ifdef __APPLE__
#  define _GETOPT
# endif

/* The system's getopt.h may have already included getopt-core.h to
   declare the unprefixed identifiers.  Undef _GETOPT_CORE_H so that
   getopt-core.h declares them with prefixes.  */
# undef _GETOPT_CORE_H
#endif

#include <getopt-core.h>

#endif /* _GETOPT_PFX_CORE_H */
