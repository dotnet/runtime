/* SPDX-License-Identifier: LGPL-2.1-or-later */

/* Declarations for getopt.
   Copyright (C) 1989-2023 Free Software Foundation, Inc.
   This file is part of gnulib.
   Unlike most of the getopt implementation, it is NOT shared
   with the GNU C Library, which supplies a different version of
   this file.

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

#ifndef _GETOPT_H

#define _GETOPT_H 1

/* Standalone applications should #define __GETOPT_PREFIX to an
   identifier that prefixes the external functions and variables
   defined in this header.  When this happens, include the
   headers that might declare getopt so that they will not cause
   confusion if included after this file.  Then systematically rename
   identifiers so that they do not collide with the system functions
   and variables.  Renaming avoids problems with some compilers and
   linkers.  */
#if defined __GETOPT_PREFIX
# include <stdlib.h>
# include <stdio.h>

# ifndef _MSC_VER
#  include <unistd.h>
# endif
#endif

/* From Gnulib's lib/arg-nonnull.h: */
/* _GL_ARG_NONNULL((n,...,m)) tells the compiler and static analyzer tools
   that the values passed as arguments n, ..., m must be non-NULL pointers.
   n = 1 stands for the first argument, n = 2 for the second argument etc.  */
#ifndef _GL_ARG_NONNULL
# if __GNUC__ > 3 || (__GNUC__ == 3 && __GNUC_MINOR__ >= 3) || defined __clang__
#  define _GL_ARG_NONNULL(params) __attribute__ ((__nonnull__ params))
# else
#  define _GL_ARG_NONNULL(params)
# endif
#endif

#include <getopt-cdefs.h>
#include <getopt-pfx-core.h>
#include <getopt-pfx-ext.h>

#endif /* _GETOPT_H */
