# ===========================================================================
#   https://www.gnu.org/software/autoconf-archive/ax_append_link_flags.html
# ===========================================================================
#
# SYNOPSIS
#
#   AX_APPEND_LINK_FLAGS([FLAG1 FLAG2 ...], [FLAGS-VARIABLE], [EXTRA-FLAGS], [INPUT])
#
# DESCRIPTION
#
#   For every FLAG1, FLAG2 it is checked whether the linker works with the
#   flag.  If it does, the flag is added FLAGS-VARIABLE
#
#   If FLAGS-VARIABLE is not specified, the linker's flags (LDFLAGS) is
#   used. During the check the flag is always added to the linker's flags.
#
#   If EXTRA-FLAGS is defined, it is added to the linker's default flags
#   when the check is done.  The check is thus made with the flags: "LDFLAGS
#   EXTRA-FLAGS FLAG".  This can for example be used to force the linker to
#   issue an error when a bad flag is given.
#
#   INPUT gives an alternative input source to AC_COMPILE_IFELSE.
#
#   NOTE: This macro depends on the AX_APPEND_FLAG and AX_CHECK_LINK_FLAG.
#   Please keep this macro in sync with AX_APPEND_COMPILE_FLAGS.
#
# LICENSE
#
#   Copyright (c) 2011 Maarten Bosmans <mkbosmans@gmail.com>
#
#   This program is free software: you can redistribute it and/or modify it
#   under the terms of the GNU General Public License as published by the
#   Free Software Foundation, either version 3 of the License, or (at your
#   option) any later version.
#
#   This program is distributed in the hope that it will be useful, but
#   WITHOUT ANY WARRANTY; without even the implied warranty of
#   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
#   Public License for more details.
#
#   You should have received a copy of the GNU General Public License along
#   with this program. If not, see <https://www.gnu.org/licenses/>.
#
#   As a special exception, the respective Autoconf Macro's copyright owner
#   gives unlimited permission to copy, distribute and modify the configure
#   scripts that are the output of Autoconf when processing the Macro. You
#   need not follow the terms of the GNU General Public License when using
#   or distributing such scripts, even though portions of the text of the
#   Macro appear in them. The GNU General Public License (GPL) does govern
#   all other use of the material that constitutes the Autoconf Macro.
#
#   This special exception to the GPL applies to versions of the Autoconf
#   Macro released by the Autoconf Archive. When you make and distribute a
#   modified version of the Autoconf Macro, you may extend this special
#   exception to the GPL to apply to your modified version as well.

#serial 6

AC_DEFUN([AX_APPEND_LINK_FLAGS],
[AX_REQUIRE_DEFINED([AX_CHECK_LINK_FLAG])
AX_REQUIRE_DEFINED([AX_APPEND_FLAG])
for flag in $1; do
  AX_CHECK_LINK_FLAG([$flag], [AX_APPEND_FLAG([$flag], [m4_default([$2], [LDFLAGS])])], [], [$3], [$4])
done
])dnl AX_APPEND_LINK_FLAGS
