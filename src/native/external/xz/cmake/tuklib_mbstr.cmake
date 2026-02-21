# SPDX-License-Identifier: 0BSD

#############################################################################
#
# tuklib_mbstr.cmake - see tuklib_mbstr.m4 for description and comments
#
# Author: Lasse Collin
#
#############################################################################

include("${CMAKE_CURRENT_LIST_DIR}/tuklib_common.cmake")
include(CheckSymbolExists)

function(tuklib_mbstr TARGET_OR_ALL)
    check_symbol_exists(mbrtowc wchar.h HAVE_MBRTOWC)
    tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE_MBRTOWC)

    # NOTE: wcwidth() requires _GNU_SOURCE or _XOPEN_SOURCE on GNU/Linux.
    check_symbol_exists(wcwidth wchar.h HAVE_WCWIDTH)
    tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE_WCWIDTH)

    # NOTE: vasprintf() requires _GNU_SOURCE on GNU/Linux.
    check_symbol_exists(vasprintf stdio.h HAVE_VASPRINTF)
    tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE_VASPRINTF)
endfunction()
