# SPDX-License-Identifier: 0BSD

#############################################################################
#
# tuklib_progname.cmake - see tuklib_progname.m4 for description and comments
#
# Author: Lasse Collin
#
#############################################################################

include("${CMAKE_CURRENT_LIST_DIR}/tuklib_common.cmake")
include(CheckSymbolExists)

function(tuklib_progname TARGET_OR_ALL)
    # NOTE: This glibc extension requires _GNU_SOURCE.
    check_symbol_exists(program_invocation_name errno.h
                        HAVE_PROGRAM_INVOCATION_NAME)
    tuklib_add_definition_if("${TARGET_OR_ALL}" HAVE_PROGRAM_INVOCATION_NAME)
endfunction()
