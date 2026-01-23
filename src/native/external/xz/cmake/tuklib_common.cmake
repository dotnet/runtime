# SPDX-License-Identifier: 0BSD

#############################################################################
#
# tuklib_common.cmake - common functions and macros for tuklib_*.cmake files
#
# Author: Lasse Collin
#
#############################################################################

function(tuklib_add_definitions TARGET_OR_ALL DEFINITIONS)
    # DEFINITIONS may be an empty string/list but it's fine here. There is
    # no need to quote ${DEFINITIONS} as empty arguments are fine here.
    if(TARGET_OR_ALL STREQUAL "ALL")
        add_compile_definitions(${DEFINITIONS})
    else()
        target_compile_definitions("${TARGET_OR_ALL}" PRIVATE ${DEFINITIONS})
    endif()
endfunction()

function(tuklib_add_definition_if TARGET_OR_ALL VAR)
    if(${VAR})
        tuklib_add_definitions("${TARGET_OR_ALL}" "${VAR}")
    endif()
endfunction()

# This is an over-simplified version of AC_USE_SYSTEM_EXTENSIONS in Autoconf
# or gl_USE_SYSTEM_EXTENSIONS in gnulib.
#
# NOTE: This is a macro because the changes to CMAKE_REQUIRED_DEFINITIONS
# must be visible in the calling scope.
macro(tuklib_use_system_extensions)
    if(NOT MSVC)
        add_compile_definitions(
            _GNU_SOURCE        # glibc, musl, mingw-w64
            _NETBSD_SOURCE     # NetBSD, MINIX 3
            _OPENBSD_SOURCE    # Also NetBSD!
            __EXTENSIONS__     # Solaris
            _POSIX_PTHREAD_SEMANTICS # Solaris
            _DARWIN_C_SOURCE   # macOS
            _TANDEM_SOURCE     # HP NonStop
            _ALL_SOURCE        # AIX, z/OS
        )

        list(APPEND CMAKE_REQUIRED_DEFINITIONS
            -D_GNU_SOURCE
            -D_NETBSD_SOURCE
            -D_OPENBSD_SOURCE
            -D__EXTENSIONS__
            -D_POSIX_PTHREAD_SEMANTICS
            -D_DARWIN_C_SOURCE
            -D_TANDEM_SOURCE
            -D_ALL_SOURCE
        )
    endif()
endmacro()
