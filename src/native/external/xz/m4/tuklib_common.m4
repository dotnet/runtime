# SPDX-License-Identifier: 0BSD

#############################################################################
#
# SYNOPSIS
#
#   TUKLIB_COMMON
#
# DESCRIPTION
#
#   Common checks for tuklib.
#
#############################################################################
#
# Author: Lasse Collin
#
#############################################################################

AC_DEFUN_ONCE([TUKLIB_COMMON], [
AC_REQUIRE([AC_CANONICAL_HOST])
AC_REQUIRE([AC_PROG_CC_C99])
AC_REQUIRE([AC_USE_SYSTEM_EXTENSIONS])
])dnl
