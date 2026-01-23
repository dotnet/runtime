/* SPDX-License-Identifier: LGPL-2.1-or-later */

/* getopt (GNU extensions) gnulib wrapper header.
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

#ifndef _GETOPT_PFX_EXT_H
#define _GETOPT_PFX_EXT_H 1

/* This header should not be used directly; include getopt.h instead.
   It does not have a protective #error, because the guard macro for
   getopt.h in gnulib is not fixed.  */

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
# undef getopt_long
# undef getopt_long_only
# undef option
# undef _getopt_internal
# define getopt_long __GETOPT_ID (getopt_long)
# define getopt_long_only __GETOPT_ID (getopt_long_only)
# define option __GETOPT_ID (option)
# define _getopt_internal __GETOPT_ID (getopt_internal)

/* The system's getopt.h may have already included getopt-ext.h to
   declare the unprefixed identifiers.  Undef _GETOPT_EXT_H so that
   getopt-ext.h declares them with prefixes.  */
# undef _GETOPT_EXT_H
#endif

/* Standalone applications get correct prototypes for getopt_long and
   getopt_long_only; they declare "char **argv".  For backward
   compatibility with old applications, if __GETOPT_PREFIX is not
   defined, we supply GNU-libc-compatible, but incorrect, prototypes
   using "char *const *argv".  (GNU libc is stuck with the incorrect
   prototypes, as they are baked into older versions of LSB.)  */
#ifndef __getopt_argv_const
# if defined __GETOPT_PREFIX
#  define __getopt_argv_const /* empty */
# else
#  define __getopt_argv_const const
# endif
#endif

#include <getopt-ext.h>

#endif /* _GETOPT_PFX_EXT_H */
