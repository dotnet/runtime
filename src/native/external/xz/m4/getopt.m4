dnl SPDX-License-Identifier: FSFULLR

# getopt.m4 serial 49 (modified version)
dnl Copyright (C) 2002-2006, 2008-2023 Free Software Foundation, Inc.
dnl This file is free software; the Free Software Foundation
dnl gives unlimited permission to copy and/or distribute it,
dnl with or without modifications, as long as this notice is preserved.

# This version has been modified to reduce complexity since we only need
# GNU getopt_long and do not care about replacing getopt.
#
# Pass gl_replace_getopt=yes (or any non-empty value instead of "yes") as
# an argument to configure to force the use of the getopt_long replacement.

AC_DEFUN([gl_FUNC_GETOPT_GNU],
[
  AC_REQUIRE([gl_GETOPT_CHECK_HEADERS])

  if test -n "$gl_replace_getopt"; then
    gl_GETOPT_SUBSTITUTE
  fi
])

AC_DEFUN([gl_GETOPT_CHECK_HEADERS],
[
  if test -z "$gl_replace_getopt"; then
    AC_CHECK_HEADERS([getopt.h], [], [gl_replace_getopt=yes])
  fi

  if test -z "$gl_replace_getopt"; then
    AC_CHECK_FUNCS([getopt_long], [], [gl_replace_getopt=yes])
  fi

  dnl BSD getopt_long uses a way to reset option processing, that is different
  dnl from GNU and Solaris (which copied the GNU behavior). We support both
  dnl GNU and BSD style resetting of getopt_long(), so there's no need to use
  dnl GNU getopt_long() on BSD due to different resetting style.
  if test -z "$gl_replace_getopt"; then
    AC_CHECK_DECL([optreset],
      [AC_DEFINE([HAVE_OPTRESET], 1,
        [Define to 1 if getopt.h declares extern int optreset.])],
      [], [#include <getopt.h>])
  fi

  dnl POSIX 2008 does not specify leading '+' behavior, but see
  dnl http://austingroupbugs.net/view.php?id=191 for a recommendation on
  dnl the next version of POSIX.  We don't use that feature, so this
  dnl is not a problem for us. Thus, the respective test was removed here.

  dnl Checks for getopt handling '-' as a leading character in an option
  dnl string were removed, since we also don't use that feature.

])

AC_DEFUN([gl_GETOPT_SUBSTITUTE],
[
  AC_LIBOBJ([getopt])
  AC_LIBOBJ([getopt1])

  AC_CHECK_HEADERS_ONCE([sys/cdefs.h])

  AC_DEFINE([__GETOPT_PREFIX], [[rpl_]],
    [Define to rpl_ if the getopt replacement functions and variables
     should be used.])

  GETOPT_H=getopt.h
  AC_SUBST([GETOPT_H])
])

AC_DEFUN([gl_GETOPT], [gl_FUNC_GETOPT_GNU])
