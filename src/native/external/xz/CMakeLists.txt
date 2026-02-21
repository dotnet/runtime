# SPDX-License-Identifier: 0BSD

#############################################################################
#
# CMake support for building XZ Utils
#
# Requirements:
#
#   - CMake 3.20 or later
#
#   - To get translated messages, install GNU gettext tools (the command
#     msgfmt is needed). Alternatively disable translations by setting
#     XZ_NLS=OFF.
#
#   - If building from xz.git instead of a release tarball: To generate
#     translated man pages, run po4a/update-po which requires the po4a
#     tool. The build works without this step too.
#
# About CMAKE_BUILD_TYPE:
#
#   - CMake's standard choices are fine to use for production builds,
#     including "Release" and "RelWithDebInfo".
#
#     NOTE: While "Release" uses -O3 by default with some compilers,
#     this file overrides -O3 to -O2 for "Release" builds if
#     CMAKE_C_FLAGS_RELEASE is not defined by the user. At least
#     with GCC and Clang/LLVM, -O3 doesn't seem useful for this
#     package as it can result in bigger binaries without any
#     improvement in speed compared to -O2.
#
#   - Empty value (the default) is handled slightly specially: It
#     adds -DNDEBUG to disable debugging code (assert() and a few
#     other things). No optimization flags are added so an empty
#     CMAKE_BUILD_TYPE is an easy way to build with whatever
#     optimization flags one wants, and so this method is also
#     suitable for production builds.
#
#     If debugging is wanted when using empty CMAKE_BUILD_TYPE,
#     include -UNDEBUG in the CFLAGS environment variable or
#     in the CMAKE_C_FLAGS CMake variable to override -DNDEBUG.
#     With empty CMAKE_BUILD_TYPE, the -UNDEBUG option will go
#     after the -DNDEBUG option on the compiler command line and
#     thus NDEBUG will be undefined.
#
#   - Non-standard build types like "None" aren't treated specially
#     and thus won't have -DNEBUG. Such non-standard build types
#     SHOULD BE AVOIDED FOR PRODUCTION BUILDS. Or at least one
#     should remember to add -DNDEBUG.
#
# This file provides the following installation components (if you only
# need liblzma, install only its components!):
#   - liblzma_Runtime (shared library only)
#   - liblzma_Development
#   - liblzma_Documentation (examples and Doxygen-generated API docs as HTML)
#   - xz_Runtime (xz, the symlinks, and possibly translation files)
#   - xz_Documentation (xz man pages and the symlinks)
#   - xzdec_Runtime
#   - xzdec_Documentation (xzdec *and* lzmadec man pages)
#   - lzmadec_Runtime
#   - lzmainfo_Runtime
#   - lzmainfo_Documentation (lzmainfo man pages)
#   - scripts_Runtime (xzdiff, xzgrep, xzless, xzmore)
#   - scripts_Documentation (their man pages)
#   - Documentation (generic docs like README and licenses)
#
# To find the target liblzma::liblzma from other packages, use the CONFIG
# option with find_package() to avoid a conflict with the FindLibLZMA module
# with case-insensitive file systems. For example, to require liblzma 5.2.5
# or a newer compatible version:
#
#     find_package(liblzma 5.2.5 REQUIRED CONFIG)
#     target_link_libraries(my_application liblzma::liblzma)
#
#############################################################################
#
# Author: Lasse Collin
#
#############################################################################

cmake_minimum_required(VERSION 3.20...4.2 FATAL_ERROR)

include(CMakePushCheckState)
include(CheckIncludeFile)
include(CheckSymbolExists)
include(CheckStructHasMember)
include(CheckCSourceCompiles)
include(CheckCCompilerFlag)
include(CheckLinkerFlag)
include(cmake/tuklib_large_file_support.cmake)
include(cmake/tuklib_integer.cmake)
include(cmake/tuklib_cpucores.cmake)
include(cmake/tuklib_physmem.cmake)
include(cmake/tuklib_progname.cmake)
include(cmake/tuklib_mbstr.cmake)

set(PACKAGE_NAME "XZ Utils")
set(PACKAGE_BUGREPORT "xz@tukaani.org")
set(PACKAGE_URL "https://tukaani.org/xz/")

# Get the package version from version.h into PACKAGE_VERSION_SHORT and
# PACKAGE_VERSION. The former variable won't include the possible "alpha"
# or "beta" suffix.
file(READ src/liblzma/api/lzma/version.h PACKAGE_VERSION)
string(REGEX REPLACE
"^.*\n\
#define LZMA_VERSION_MAJOR ([0-9]+)\n\
.*\
#define LZMA_VERSION_MINOR ([0-9]+)\n\
.*\
#define LZMA_VERSION_PATCH ([0-9]+)\n\
.*$"
       "\\1.\\2.\\3" PACKAGE_VERSION_SHORT "${PACKAGE_VERSION}")

if(PACKAGE_VERSION MATCHES "\n#define [A-Z_ ]*_ALPHA\n")
    set(PACKAGE_VERSION "${PACKAGE_VERSION_SHORT}alpha")
elseif(PACKAGE_VERSION MATCHES "\n#define [A-Z_ ]*_BETA\n")
    set(PACKAGE_VERSION "${PACKAGE_VERSION_SHORT}beta")
else()
    set(PACKAGE_VERSION "${PACKAGE_VERSION_SHORT}")
endif()

# With several compilers, CMAKE_BUILD_TYPE=Release uses -O3 optimization
# which results in bigger code without a clear difference in speed. If
# no user-defined CMAKE_C_FLAGS_RELEASE is present, override -O3 to -O2
# to make it possible to recommend CMAKE_BUILD_TYPE=Release.
if(NOT DEFINED CMAKE_C_FLAGS_RELEASE)
    set(OVERRIDE_O3_IN_C_FLAGS_RELEASE ON)
endif()

# Among other things, this gives us variables xz_VERSION and xz_VERSION_MAJOR.
project(xz VERSION "${PACKAGE_VERSION_SHORT}" LANGUAGES C)

if(OVERRIDE_O3_IN_C_FLAGS_RELEASE)
    # Looking at CMake's source, there aren't any _FLAGS_RELEASE_INIT
    # entries where "-O3" would appear as part of some other option,
    # thus a simple search and replace should be fine.
    string(REPLACE -O3 -O2 CMAKE_C_FLAGS_RELEASE "${CMAKE_C_FLAGS_RELEASE}")

    # Update the cache value while keeping its docstring unchanged.
    set_property(CACHE CMAKE_C_FLAGS_RELEASE
                 PROPERTY VALUE "${CMAKE_C_FLAGS_RELEASE}")
endif()

# Reject unsupported MSVC versions.
if(MSVC AND MSVC_VERSION LESS 1900)
    message(FATAL_ERROR "Visual Studio older than 2015 is not supported")
endif()

# We need a compiler that supports enough C99 or newer (variable-length arrays
# aren't needed, those are optional in C11/C17). C11 is preferred since C11
# features may be optionally used if they are available.
#
# Setting CMAKE_C_STANDARD here makes it the default for all targets.
# It doesn't affect the INTERFACE so liblzma::liblzma won't end up with
# INTERFACE_COMPILE_FEATURES "c_std_99" or such (the API headers are C89
# and C++ compatible).
#
# Avoid set(CMAKE_C_STANDARD_REQUIRED ON) because it's fine to decay
# to C99 if C11 isn't supported.
set(CMAKE_C_STANDARD 11)

# On Apple OSes, don't build executables as bundles:
set(CMAKE_MACOSX_BUNDLE OFF)

# The targets defined here don't support compiling as a unity build. Encoder
# and decoder source files define different types with the same name, and some
# internal header files don't have header guards leading to redeclaration
# errors.
set(CMAKE_UNITY_BUILD OFF)

# Set CMAKE_INSTALL_LIBDIR and friends. This needs to be done before
# the LOCALEDIR_DEFINITION workaround below.
include(GNUInstallDirs)

# windres from GNU binutils can be tricky with command line arguments
# that contain spaces or other funny characters. Unfortunately we need
# a space in PACKAGE_NAME. Using \x20 to encode the US-ASCII space seems
# to work in both cmd.exe and /bin/sh.
#
# However, even \x20 isn't enough in all situations, resulting in
# "syntax error" from windres. Using --use-temp-file prevents windres
# from using popen() and this seems to fix the problem.
#
# llvm-windres from Clang/LLVM 16.0.6 and older: The \x20 results
# in "XZx20Utils" in the compiled binary. The option --use-temp-file
# makes no difference.
#
# llvm-windres 17.0.0 and later: It emulates GNU windres more accurately, so
# the workarounds used with GNU windres must be used with llvm-windres too.
#
# CMake 3.27 doesn't have CMAKE_RC_COMPILER_ID so we rely on
# CMAKE_C_COMPILER_ID.
if((MINGW OR CYGWIN) AND (
        NOT CMAKE_C_COMPILER_ID STREQUAL "Clang" OR
        CMAKE_C_COMPILER_VERSION VERSION_GREATER_EQUAL "17"))
    # Use workarounds with GNU windres and llvm-windres >= 17.0.0. The \x20
    # in PACKAGE_NAME_DEFINITION works with gcc and clang too so we don't need
    # to worry how to pass different flags to windres and the C compiler.
    # Keep the original PACKAGE_NAME intact for generation of liblzma.pc.
    string(APPEND CMAKE_RC_FLAGS " --use-temp-file")
    string(REPLACE " " "\\040" PACKAGE_NAME_DEFINITION "${PACKAGE_NAME}")

    # Use octal because "Program Files" would become \x20F.
    string(REPLACE " " "\\040" LOCALEDIR_DEFINITION
           "${CMAKE_INSTALL_FULL_LOCALEDIR}")
else()
    # Elsewhere a space is safe. This also keeps things compatible with
    # EBCDIC in case CMake-based build is ever done on such a system.
    set(PACKAGE_NAME_DEFINITION "${PACKAGE_NAME}")
    set(LOCALEDIR_DEFINITION "${CMAKE_INSTALL_FULL_LOCALEDIR}")
endif()

# When used with MSVC, CMake can merge .manifest files with
# linker-generated manifests and embed the result in an executable.
# However, when paired with MinGW-w64, CMake (3.30) ignores .manifest
# files. Embedding a manifest with a resource file works with both
# toochains. It's also the way to do it with Autotools.
#
# With MSVC, we need to disable the default manifest; attempting to add
# two manifest entries would break the build. The flag /MANIFEST:NO
# goes to the linker and it also affects behavior of CMake itself: it
# looks what flags are being passed to the linker and when CMake sees
# the /MANIFEST:NO option, other manifest-related linker flags are
# no longer added (see the file Source/cmcmd.cxx in CMake).
#
# See: https://gitlab.kitware.com/cmake/cmake/-/issues/23066
if(MSVC)
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /MANIFEST:NO")
endif()

# Dependencies for all Windows resource files:
set(W32RES_DEPENDENCIES
    "${CMAKE_CURRENT_SOURCE_DIR}/src/common/common_w32res.rc"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/common/w32_application.manifest"
)

# Definitions common to all targets:
add_compile_definitions(
    # Package info:
    PACKAGE_NAME="${PACKAGE_NAME_DEFINITION}"
    PACKAGE_BUGREPORT="${PACKAGE_BUGREPORT}"
    PACKAGE_URL="${PACKAGE_URL}"

    # Standard headers and types are available:
    HAVE_STDBOOL_H
    HAVE__BOOL
    HAVE_STDINT_H
    HAVE_INTTYPES_H

    # Always enable CRC32 since liblzma should never build without it.
    HAVE_CHECK_CRC32

    # Disable assert() checks when no build type has been specified. Non-empty
    # build types like "Release" and "Debug" handle this by default.
    $<$<CONFIG:>:NDEBUG>
)

# Support 32-bit x86 assembly files.
if(NOT MSVC)
    # It's simplest to ask the compiler for the architecture because we
    # know that on supported platforms __i386__ is defined.
    #
    # Checking CMAKE_SYSTEM_PROCESSOR wouldn't be so simple or as reliable
    # because it could indicate x86-64 even if building for 32-bit x86
    # because one doesn't necessarily use a CMake toolchain file when
    # building 32-bit executables on a 64-bit system. Also, the strings
    # that identify 32-bit or 64-bit x86 aren't standardized in CMake.
    if(MINGW OR CYGWIN OR CMAKE_SYSTEM_NAME MATCHES
            "^FreeBSD$|^GNU$|^Linux$|^MirBSD$|^NetBSD$|^OpenBSD$")
        check_symbol_exists("__i386__" "" ASM_I386_DEFAULT)
    else()
        set(ASM_I386_DEFAULT OFF)
    endif()

    option(XZ_ASM_I386 "Enable 32-bit x86 assembly code"
           "${ASM_I386_DEFAULT}")

    if(XZ_ASM_I386)
        enable_language(ASM)
    endif()
endif()


######################
# System definitions #
######################

# _GNU_SOURCE and such definitions. This specific macro is special since
# it also adds the definitions to CMAKE_REQUIRED_DEFINITIONS.
tuklib_use_system_extensions()

# Check for large file support. It's required on some 32-bit platforms and
# even on 64-bit MinGW-w64 to get 64-bit off_t. This can be forced off on
# the CMake command line if needed: -DLARGE_FILE_SUPPORT=OFF
tuklib_large_file_support(ALL)

# This is needed by liblzma and xz.
tuklib_integer(ALL)

# This is used for liblzma.pc generation to add -lrt and -lmd if needed.
#
# The variable name LIBS comes from Autoconf where AC_SEARCH_LIBS adds the
# libraries it finds into the shell variable LIBS. These libraries need to
# be put into liblzma.pc too, thus liblzma.pc.in has @LIBS@ because that
# matches the Autoconf's variable. When CMake support was added, using
# the same variable with configure_file() was the simplest method.
set(LIBS)

# Check for clock_gettime(). Do this before checking for threading so
# that we know there if CLOCK_MONOTONIC is available.
check_symbol_exists(clock_gettime time.h HAVE_CLOCK_GETTIME)

if(NOT HAVE_CLOCK_GETTIME)
    # With glibc <= 2.17 or Solaris 10 this needs librt.
    # Add librt for the next check for HAVE_CLOCK_GETTIME. If it is
    # found after including the library, we know that librt is required.
    list(PREPEND CMAKE_REQUIRED_LIBRARIES rt)
    check_symbol_exists(clock_gettime time.h HAVE_CLOCK_GETTIME_LIBRT)

    # If it was found now, add librt to all targets and keep it in
    # CMAKE_REQUIRED_LIBRARIES for further tests too.
    if(HAVE_CLOCK_GETTIME_LIBRT)
        link_libraries(rt)
        set(LIBS "-lrt ${LIBS}") # For liblzma.pc
    else()
        list(POP_FRONT CMAKE_REQUIRED_LIBRARIES)
    endif()
endif()

if(HAVE_CLOCK_GETTIME OR HAVE_CLOCK_GETTIME_LIBRT)
    add_compile_definitions(HAVE_CLOCK_GETTIME)

    # Check if CLOCK_MONOTONIC is available for clock_gettime().
    check_symbol_exists(CLOCK_MONOTONIC time.h HAVE_CLOCK_MONOTONIC)
    tuklib_add_definition_if(ALL HAVE_CLOCK_MONOTONIC)
endif()


# The definition ENABLE_NLS is added only to those targets that use it, thus
# it's not done here. (xz has translations, xzdec doesn't.)
set(XZ_NLS_DEFAULT OFF)
find_package(Intl)
find_package(Gettext)
if(Intl_FOUND AND GETTEXT_FOUND)
    set(XZ_NLS_DEFAULT ON)
endif()

option(XZ_NLS "Native Language Support (translated messages and man pages)"
       "${XZ_NLS_DEFAULT}")

if(XZ_NLS)
    if(NOT Intl_FOUND)
        message(FATAL_ERROR "Native language support (NLS) was enabled but "
                            "find_package(Intl) failed. "
                            "Install libintl or set XZ_NLS=OFF.")
    endif()

    if(NOT GETTEXT_FOUND)
        message(FATAL_ERROR "Native language support (NLS) was enabled but "
                            "find_package(Gettext) failed. "
                            "Install gettext tools or set XZ_NLS=OFF.")
    endif()

    if(WIN32)
        # The command line tools use UTF-8 on native Windows.
        # Non-ASCII characters display correctly only when
        # using UCRT and gettext-runtime >= 0.23.1.
        check_c_source_compiles(
                "#define WIN32_LEAN_AND_MEAN
                #include <windows.h>
                #include <libintl.h>

                #ifndef _UCRT
                #error \"Not UCRT\"
                #endif

                #if LIBINTL_VERSION < 0x001701
                #error \"gettext-runtime < 0.23.1\"
                #endif

                int main(void) { return 0; }
            "
            USING_UCRT_AND_RECENT_GETTEXT)
        if(NOT USING_UCRT_AND_RECENT_GETTEXT)
            message(FATAL_ERROR "Native language support (NLS) was enabled "
                                "but it requires UCRT and "
                                "gettext-runtime >= 0.23.1. To build with "
                                "MSVCRT or old gettext-runtime, "
                                "set XZ_NLS=OFF.")
        endif()
    endif()

    # Warn if translated man pages are missing.
    if(UNIX AND NOT EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/po4a/man")
        message(WARNING "Native language support (NLS) was enabled "
                        "but pre-generated translated man pages "
                        "were not found and thus they won't be installed. "
                        "Run 'po4a/update-po' to generate them.")
    endif()

    # The *installed* name of the translation files is "xz.mo".
    set(TRANSLATION_DOMAIN "xz")
endif()


# Add warning options for GCC or Clang. Keep this in sync with configure.ac.
#
# NOTE: add_compile_options() doesn't affect the feature checks;
# only the new targets being created use these flags. Thus
# the -Werror usage in checks won't be break because of these.
if(CMAKE_C_COMPILER_ID MATCHES GNU|Clang)
    foreach(OPT -Wall
                -Wextra
                -Wvla
                -Wformat=2
                -Winit-self
                -Wmissing-include-dirs
                -Wshift-overflow=2
                -Wstrict-overflow=3
                -Walloc-zero
                -Wduplicated-cond
                -Wfloat-equal
                -Wundef
                -Wshadow
                -Wpointer-arith
                -Wbad-function-cast
                -Wwrite-strings
                -Wdate-time
                -Wsign-conversion
                -Wfloat-conversion
                -Wlogical-op
                -Waggregate-return
                -Wstrict-prototypes
                -Wold-style-definition
                -Wmissing-prototypes
                -Wmissing-declarations
                -Wredundant-decls
                -Wimplicit-fallthrough
                -Wimplicit-fallthrough=5

                -Wc99-compat
                -Wc11-extensions
                -Wc2x-compat
                -Wc2x-extensions
                -Wpre-c2x-compat
                -Warray-bounds-pointer-arithmetic
                -Wassign-enum
                -Wconditional-uninitialized
                -Wdocumentation
                -Wduplicate-enum
                -Wempty-translation-unit
                -Wextra-semi-stmt
                -Wflexible-array-extensions
                -Wmissing-variable-declarations
                -Wnewline-eof
                -Wshift-sign-overflow
                -Wstring-conversion
                )
        # A variable name cannot have = in it so replace = with _.
        string(REPLACE = _ CACHE_VAR "HAVE_COMPILER_OPTION_${OPT}")

        check_c_compiler_flag("${OPT}" "${CACHE_VAR}")

        if("${${CACHE_VAR}}")
            add_compile_options("${OPT}")
        endif()
    endforeach()
endif()


#############################################################################
# liblzma
#############################################################################

option(BUILD_SHARED_LIBS "Build liblzma as a shared library instead of static")

# Symbol versioning is supported with ELF shared libraries on certain OSes.
# First assume that symbol versioning isn't supported.
set(SYMBOL_VERSIONING "no")

if(NOT WIN32)
    # The XZ_SYMBOL_VERSIONING option is ignored for static libraries but
    # we keep the option visible still in case the project is reconfigured
    # to build a shared library.
    #
    # auto      Autodetect between no, generic, and linux
    # yes       Force on by autodetecting between linux and generic
    # no        Disable symbol versioning
    # generic   FreeBSD, some Linux/glibc systems, and GNU/Hurd
    # linux     Linux/glibc with extra symbol versions for compatibility
    #           with binaries that have been linked against a liblzma version
    #           that has been patched with "xz-5.2.2-compat-libs.patch" from
    #           RHEL/CentOS 7.
    set(SUPPORTED_SYMBOL_VERSIONING_VARIANTS auto yes no generic linux)
    set(XZ_SYMBOL_VERSIONING "auto" CACHE STRING "Enable ELF shared library \
symbol versioning (${SUPPORTED_SYMBOL_VERSIONING_VARIANTS})")

    # Show a dropdown menu in CMake GUI:
    set_property(CACHE XZ_SYMBOL_VERSIONING
                 PROPERTY STRINGS "${SUPPORTED_SYMBOL_VERSIONING_VARIANTS}")

    if(NOT XZ_SYMBOL_VERSIONING IN_LIST SUPPORTED_SYMBOL_VERSIONING_VARIANTS)
        message(FATAL_ERROR "'${XZ_SYMBOL_VERSIONING}' is not a supported "
                            "symbol versioning variant")
    endif()

    if(NOT XZ_SYMBOL_VERSIONING MATCHES "^auto$|^yes$")
        # Autodetection was disabled. Use the user-specified value as is.
        set(SYMBOL_VERSIONING "${XZ_SYMBOL_VERSIONING}")
    else()
        # Autodetect the symbol versioning variant.
        #
        # Avoid symvers on Linux with non-glibc like musl and uClibc.
        # In Autoconf it's enough to check that $host_os equals linux-gnu
        # instead of, for example, linux-musl. CMake doesn't provide such
        # a method.
        #
        # This check is here for now since it's not strictly required
        # by anything else.
        if(CMAKE_SYSTEM_NAME STREQUAL "Linux")
            check_c_source_compiles(
                    "#include <features.h>
                    #if defined(__GLIBC__) && !defined(__UCLIBC__)
                    int main(void) { return 0; }
                    #else
                    compile error
                    #endif
                "
                IS_LINUX_WITH_GLIBC)
        else()
            set(IS_LINUX_WITH_GLIBC OFF)
        endif()

        if(IS_LINUX_WITH_GLIBC AND
                (CMAKE_SYSTEM_PROCESSOR MATCHES "[Mm]icro[Bb]laze" OR
                 CMAKE_C_COMPILER_ID STREQUAL "NVHPC"))
            # As a special case, GNU/Linux on MicroBlaze gets the generic
            # symbol versioning because GCC 12 doesn't support the __symver__
            # attribute on MicroBlaze. On Linux, CMAKE_SYSTEM_PROCESSOR comes
            # from "uname -m" for native builds (should be "microblaze") or
            # from the CMake toolchain file (not perfectly standardized but
            # it very likely has "microblaze" in lower case or mixed case
            # somewhere in the string).
            #
            # NVIDIA HPC Compiler doesn't support symbol versioning but
            # it uses the linker from the system so the linker script
            # can still be used to get the generic symbol versioning.
            set(SYMBOL_VERSIONING "generic")

        elseif(IS_LINUX_WITH_GLIBC)
            # GNU/Linux-specific symbol versioning for shared liblzma. This
            # includes a few extra compatibility symbols for RHEL/CentOS 7
            # which are pointless on non-glibc non-Linux systems.
            set(SYMBOL_VERSIONING "linux")

        elseif(CMAKE_SYSTEM_NAME MATCHES "^FreeBSD$|^GNU$" OR
               XZ_SYMBOL_VERSIONING STREQUAL "yes")
            set(SYMBOL_VERSIONING "generic")
        endif()
    endif()

    if(NOT SYMBOL_VERSIONING STREQUAL "no")
        # If features are disabled in liblzma, some symbols may be missing.
        # LLVM's lld defaults to --no-undefined-version and the build breaks
        # if not all symbols in the version script exist. That is good for
        # catching errors like typos, but in our case the downside is too big.
        # Avoid the problem by using --undefined-version if the linker
        # supports it.
        #
        # GNU ld has had --no-undefined-version for a long time but it's not
        # the default. The opposite option --undefined-version was only added
        # in 2022, thus we must use --undefined-version conditionally.
        check_linker_flag(C "-Wl,--undefined-version"
                          HAVE_LINKER_FLAG_UNDEFINED_VERSION)
    endif()
endif()

set(LIBLZMA_API_HEADERS
    src/liblzma/api/lzma.h
    src/liblzma/api/lzma/base.h
    src/liblzma/api/lzma/bcj.h
    src/liblzma/api/lzma/block.h
    src/liblzma/api/lzma/check.h
    src/liblzma/api/lzma/container.h
    src/liblzma/api/lzma/delta.h
    src/liblzma/api/lzma/filter.h
    src/liblzma/api/lzma/hardware.h
    src/liblzma/api/lzma/index.h
    src/liblzma/api/lzma/index_hash.h
    src/liblzma/api/lzma/lzma12.h
    src/liblzma/api/lzma/stream_flags.h
    src/liblzma/api/lzma/version.h
    src/liblzma/api/lzma/vli.h
)

add_library(liblzma
    src/common/mythread.h
    src/common/sysdefs.h
    src/common/tuklib_common.h
    src/common/tuklib_config.h
    src/common/tuklib_integer.h
    src/common/tuklib_physmem.c
    src/common/tuklib_physmem.h
    ${LIBLZMA_API_HEADERS}
    src/liblzma/check/check.c
    src/liblzma/check/check.h
    src/liblzma/check/crc_common.h
    src/liblzma/check/crc_x86_clmul.h
    src/liblzma/check/crc32_arm64.h
    src/liblzma/check/crc32_loongarch.h
    src/liblzma/common/block_util.c
    src/liblzma/common/common.c
    src/liblzma/common/common.h
    src/liblzma/common/easy_preset.c
    src/liblzma/common/easy_preset.h
    src/liblzma/common/filter_common.c
    src/liblzma/common/filter_common.h
    src/liblzma/common/hardware_physmem.c
    src/liblzma/common/index.c
    src/liblzma/common/index.h
    src/liblzma/common/memcmplen.h
    src/liblzma/common/stream_flags_common.c
    src/liblzma/common/stream_flags_common.h
    src/liblzma/common/string_conversion.c
    src/liblzma/common/vli_size.c
)

target_include_directories(liblzma PRIVATE
    src/liblzma/api
    src/liblzma/common
    src/liblzma/check
    src/liblzma/lz
    src/liblzma/rangecoder
    src/liblzma/lzma
    src/liblzma/delta
    src/liblzma/simple
    src/common
)


#############
# Threading #
#############

# Supported threading methods:
# yes   - Autodetect the best threading method. The autodetection will
#         prefer Windows threading (win95 or vista) over posix if both are
#         available. vista threads will be used over win95 unless it is a
#         32-bit build. Configuration fails if no threading support is found;
#         threading won't be silently disabled.
# no    - Disable threading.
# posix - Use posix threading (pthreads), or throw an error if not available.
# win95 - Use Windows win95 threading, or throw an error if not available.
# vista - Use Windows vista threading, or throw an error if not available.
set(SUPPORTED_THREADING_METHODS yes no posix win95 vista)

set(XZ_THREADS yes CACHE STRING "Threading method: \
'yes' to autodetect, 'no' to disable, 'posix' (pthreads), \
'win95' (WinXP compatible), 'vista' (needs Windows Vista or later)")

# Create dropdown in CMake GUI since only 1 threading method is possible
# to select in a build.
set_property(CACHE XZ_THREADS
             PROPERTY STRINGS "${SUPPORTED_THREADING_METHODS}")

# This is a flag variable set when win95 threads are used. We must ensure
# that the combination of XZ_SMALL and win95 threads is only used with a
# compiler that supports the __constructor__ attribute.
set(USE_WIN95_THREADS OFF)

# This is a flag variable set when posix threading (pthreads) is used.
# It's needed when creating liblzma-config.cmake where dependency on
# Threads::Threads is only needed with pthreads.
set(USE_POSIX_THREADS OFF)

if(NOT XZ_THREADS IN_LIST SUPPORTED_THREADING_METHODS)
    message(FATAL_ERROR "'${XZ_THREADS}' is not a supported threading method")
endif()

if(XZ_THREADS)
    # Also set THREADS_PREFER_PTHREAD_FLAG since the flag has no effect
    # for Windows threading.
    set(THREADS_PREFER_PTHREAD_FLAG TRUE)
    find_package(Threads REQUIRED)

    # If both Windows and posix threading are available, prefer Windows.
    # Note that on Cygwin, CMAKE_USE_WIN32_THREADS_INIT is false.
    if(CMAKE_USE_WIN32_THREADS_INIT AND NOT XZ_THREADS STREQUAL "posix")
        if(XZ_THREADS STREQUAL "win95"
                OR (XZ_THREADS STREQUAL "yes" AND CMAKE_SIZEOF_VOID_P EQUAL 4))
            # Use Windows 95 (and thus XP) compatible threads.
            # This avoids use of features that were added in
            # Windows Vista. This is used for 32-bit x86 builds for
            # compatibility reasons since it makes no measurable difference
            # in performance compared to Vista threads.
            set(USE_WIN95_THREADS ON)
            add_compile_definitions(MYTHREAD_WIN95)
        else()
            add_compile_definitions(MYTHREAD_VISTA)
        endif()
    elseif(CMAKE_USE_PTHREADS_INIT)
        if(XZ_THREADS MATCHES "^posix$|^yes$")
            # The threading library only needs to be explicitly linked
            # for posix threads, so this is needed for creating
            # liblzma-config.cmake later.
            set(USE_POSIX_THREADS ON)

            target_link_libraries(liblzma PRIVATE Threads::Threads)
            add_compile_definitions(MYTHREAD_POSIX)

            # Make the thread libs available in later checks. In practice
            # only pthread_condattr_setclock check should need this.
            list(PREPEND CMAKE_REQUIRED_LIBRARIES "${CMAKE_THREAD_LIBS_INIT}")

            # Check if pthread_condattr_setclock() exists to
            # use CLOCK_MONOTONIC.
            if(HAVE_CLOCK_MONOTONIC)
                check_symbol_exists(pthread_condattr_setclock pthread.h
                                    HAVE_PTHREAD_CONDATTR_SETCLOCK)
                tuklib_add_definition_if(ALL HAVE_PTHREAD_CONDATTR_SETCLOCK)
            endif()
        else()
            message(SEND_ERROR
                    "Windows threading method was requested but a compatible "
                    "library could not be found")
        endif()
    else()
        message(SEND_ERROR "No supported threading library found")
    endif()

    target_sources(liblzma PRIVATE
        src/common/tuklib_cpucores.c
        src/common/tuklib_cpucores.h
        src/liblzma/common/hardware_cputhreads.c
        src/liblzma/common/outqueue.c
        src/liblzma/common/outqueue.h
    )
endif()


######################
# Size optimizations #
######################

option(XZ_SMALL "Reduce code size at expense of speed. \
This may be useful together with CMAKE_BUILD_TYPE=MinSizeRel.")

if(XZ_SMALL)
    add_compile_definitions(HAVE_SMALL)
endif()


##########
# Checks #
##########

set(SUPPORTED_CHECKS crc32 crc64 sha256)

set(XZ_CHECKS "${SUPPORTED_CHECKS}" CACHE STRING
    "Check types to support (crc32 is always built)")

foreach(CHECK IN LISTS XZ_CHECKS)
    if(NOT CHECK IN_LIST SUPPORTED_CHECKS)
        message(FATAL_ERROR "'${CHECK}' is not a supported check type")
    endif()
endforeach()

if(XZ_SMALL)
    target_sources(liblzma PRIVATE src/liblzma/check/crc32_small.c)
else()
    target_sources(liblzma PRIVATE
        src/liblzma/check/crc32_fast.c
        src/liblzma/check/crc32_table_be.h
        src/liblzma/check/crc32_table_le.h
    )

    if(XZ_ASM_I386)
        target_sources(liblzma PRIVATE src/liblzma/check/crc32_x86.S)
        target_compile_definitions(liblzma PRIVATE HAVE_CRC_X86_ASM)
    endif()
endif()

if("crc64" IN_LIST XZ_CHECKS)
    add_compile_definitions("HAVE_CHECK_CRC64")

    if(XZ_SMALL)
        target_sources(liblzma PRIVATE src/liblzma/check/crc64_small.c)
    else()
        target_sources(liblzma PRIVATE
            src/liblzma/check/crc64_fast.c
            src/liblzma/check/crc64_table_be.h
            src/liblzma/check/crc64_table_le.h
        )

        if(XZ_ASM_I386)
            target_sources(liblzma PRIVATE src/liblzma/check/crc64_x86.S)
            # Adding #define HAVE_CRC_X86_ASM was already handled in
            # the CRC32 case a few lines above. CRC32 is always built.
        endif()
    endif()
endif()

# External SHA-256
#
# At least the following implementations are supported:
#
# OS       Headers                     Library  Type           Function
# FreeBSD  sys/types.h + sha256.h      libmd    SHA256_CTX     SHA256_Init
# NetBSD   sys/types.h + sha2.h                 SHA256_CTX     SHA256_Init
# OpenBSD  sys/types.h + sha2.h                 SHA2_CTX       SHA256Init
# Solaris  sys/types.h + sha2.h        libmd    SHA256_CTX     SHA256Init
# MINIX 3  sys/types.h + sha2.h                 SHA256_CTX     SHA256_Init
# Darwin   CommonCrypto/CommonDigest.h          CC_SHA256_CTX  CC_SHA256_Init
#
# Note that Darwin's CC_SHA256_Update takes buffer size as uint32_t instead
# of size_t.
#
# This is disabled by default because it used to conflict with OpenSSL
# on some platforms and in some cases the builtin code in liblzma was faster.
# See INSTALL and the commit message ac398c3bafa6e4c80e20571373a96947db863b3d.
option(XZ_EXTERNAL_SHA256 "Use SHA-256 code from the operating system \
if possible. See INSTALL for possible subtle problems." OFF)

if("sha256" IN_LIST XZ_CHECKS)
    add_compile_definitions("HAVE_CHECK_SHA256")

    # Assume that external code won't be used. We need this to know
    # if the internal sha256.c should be built.
    set(USE_INTERNAL_SHA256 ON)

    if(XZ_EXTERNAL_SHA256)
        # Find the header.
        set(SHA256_HEADER OFF)

        foreach(X CommonCrypto/CommonDigest.h sha256.h sha2.h)
            string(TOUPPER "HAVE_${X}" HAVE_X)
            string(REGEX REPLACE "[/.]" "_" HAVE_X "${HAVE_X}")
            check_include_file("${X}" "${HAVE_X}")
            if(${HAVE_X})
                target_compile_definitions(liblzma PRIVATE "${HAVE_X}")
                set(SHA256_HEADER "${X}")
                break()
            endif()
        endforeach()

        if(SHA256_HEADER)
            # Find the type to hold the SHA-256 state.
            set(SHA256_TYPE OFF)

            foreach(X CC_SHA256_CTX SHA256_CTX SHA2_CTX)
                string(TOUPPER "HAVE_${X}" HAVE_X)

                # configure.ac uses <sys/types.h> conditionally but it's
                # required on all cases except Darwin where it exists too.
                # So just use it unconditionally here.
                set(SOURCE
                        "#include <sys/types.h>
                        #include <${SHA256_HEADER}>
                        int main(void)
                        {
                            ${X} ctx;
                            return 0;
                        }")
                check_c_source_compiles("${SOURCE}" "${HAVE_X}")
                if(${HAVE_X})
                    target_compile_definitions(liblzma PRIVATE "${HAVE_X}")
                    set(SHA256_TYPE "${X}")
                    break()
                endif()
            endforeach()

            if(SHA256_TYPE)
                # Find the initialization function. It might required libmd.
                foreach(X CC_SHA256_Init SHA256Init SHA256_Init)
                    string(TOUPPER "HAVE_${X}" HAVE_X)

                    # On FreeBSD, <sha256.h> defines the SHA-256 functions as
                    # macros to rename them for namespace reasons. Avoid
                    # check_symbol_exists as that would accept macros without
                    # checking if the program links.
                    set(SOURCE
                            "#include <sys/types.h>
                            #include <${SHA256_HEADER}>
                            int main(void)
                            {
                                ${SHA256_TYPE} ctx;
                                ${X}(&ctx);
                                return 0;
                            }")

                    check_c_source_compiles("${SOURCE}" "${HAVE_X}")
                    if(${HAVE_X})
                        target_compile_definitions(liblzma PRIVATE "${HAVE_X}")
                        set(USE_INTERNAL_SHA256 OFF)
                        break()
                    else()
                        # Try with libmd. Other checks don't need it so we
                        # don't need to leave it into CMAKE_REQUIRED_LIBRARIES.
                        list(PREPEND CMAKE_REQUIRED_LIBRARIES md)
                        check_c_source_compiles("${SOURCE}" "${HAVE_X}_LIBMD")
                        list(POP_FRONT CMAKE_REQUIRED_LIBRARIES)
                        if(${HAVE_X}_LIBMD)
                            # NOTE: Just "${HAVE_X}", not "${HAVE_X}_LIBMD":
                            target_compile_definitions(liblzma PRIVATE
                                                       "${HAVE_X}")
                            target_link_libraries(liblzma PRIVATE md)
                            set(LIBS "-lmd ${LIBS}") # For liblzma.pc
                            set(USE_INTERNAL_SHA256 OFF)
                            break()
                        endif()
                    endif()
                endforeach()
            endif()
        endif()
    endif()

    if(USE_INTERNAL_SHA256)
        target_sources(liblzma PRIVATE src/liblzma/check/sha256.c)
    endif()
endif()


#################
# Match finders #
#################

set(SUPPORTED_MATCH_FINDERS hc3 hc4 bt2 bt3 bt4)

set(XZ_MATCH_FINDERS "${SUPPORTED_MATCH_FINDERS}" CACHE STRING
    "Match finders to support (at least one is required for LZMA1 or LZMA2)")

foreach(MF IN LISTS XZ_MATCH_FINDERS)
    if(MF IN_LIST SUPPORTED_MATCH_FINDERS)
        string(TOUPPER "${MF}" MF_UPPER)
        add_compile_definitions("HAVE_MF_${MF_UPPER}")
    else()
        message(FATAL_ERROR "'${MF}' is not a supported match finder")
    endif()
endforeach()


############
# Encoders #
############

set(SIMPLE_FILTERS
    x86
    arm
    armthumb
    arm64
    powerpc
    ia64
    sparc
    riscv
)

# The SUPPORTED_FILTERS are shared between Encoders and Decoders
# since only lzip does not appear in both lists. lzip is a special
# case anyway, so it is handled separately in the Decoders section.
set(SUPPORTED_FILTERS
    lzma1
    lzma2
    delta
    "${SIMPLE_FILTERS}"
)

set(XZ_ENCODERS "${SUPPORTED_FILTERS}" CACHE STRING "Encoders to support")

# If LZMA2 is enabled, then LZMA1 must also be enabled.
if(NOT "lzma1" IN_LIST XZ_ENCODERS AND "lzma2" IN_LIST XZ_ENCODERS)
    message(FATAL_ERROR "LZMA2 encoder requires that LZMA1 is also enabled")
endif()

# If LZMA1 is enabled, then at least one match finder must be enabled.
if(XZ_MATCH_FINDERS STREQUAL "" AND "lzma1" IN_LIST XZ_ENCODERS)
    message(FATAL_ERROR "At least 1 match finder is required for an "
                        "LZ-based encoder")
endif()

set(HAVE_DELTA_CODER OFF)
set(SIMPLE_ENCODERS OFF)
set(HAVE_ENCODERS OFF)

foreach(ENCODER IN LISTS XZ_ENCODERS)
    if(ENCODER IN_LIST SUPPORTED_FILTERS)
        set(HAVE_ENCODERS ON)

        if(NOT SIMPLE_ENCODERS AND ENCODER IN_LIST SIMPLE_FILTERS)
            set(SIMPLE_ENCODERS ON)
        endif()

        string(TOUPPER "${ENCODER}" ENCODER_UPPER)
        add_compile_definitions("HAVE_ENCODER_${ENCODER_UPPER}")
    else()
        message(FATAL_ERROR "'${ENCODER}' is not a supported encoder")
    endif()
endforeach()

if(HAVE_ENCODERS)
    add_compile_definitions(HAVE_ENCODERS)

    target_sources(liblzma PRIVATE
        src/liblzma/common/alone_encoder.c
        src/liblzma/common/block_buffer_encoder.c
        src/liblzma/common/block_buffer_encoder.h
        src/liblzma/common/block_encoder.c
        src/liblzma/common/block_encoder.h
        src/liblzma/common/block_header_encoder.c
        src/liblzma/common/easy_buffer_encoder.c
        src/liblzma/common/easy_encoder.c
        src/liblzma/common/easy_encoder_memusage.c
        src/liblzma/common/filter_buffer_encoder.c
        src/liblzma/common/filter_encoder.c
        src/liblzma/common/filter_encoder.h
        src/liblzma/common/filter_flags_encoder.c
        src/liblzma/common/index_encoder.c
        src/liblzma/common/index_encoder.h
        src/liblzma/common/stream_buffer_encoder.c
        src/liblzma/common/stream_encoder.c
        src/liblzma/common/stream_flags_encoder.c
        src/liblzma/common/vli_encoder.c
    )

    if(XZ_THREADS)
        target_sources(liblzma PRIVATE
            src/liblzma/common/stream_encoder_mt.c
        )
    endif()

    if(SIMPLE_ENCODERS)
        target_sources(liblzma PRIVATE
            src/liblzma/simple/simple_encoder.c
            src/liblzma/simple/simple_encoder.h
        )
    endif()

    if("lzma1" IN_LIST XZ_ENCODERS)
        target_sources(liblzma PRIVATE
            src/liblzma/lzma/lzma_encoder.c
            src/liblzma/lzma/lzma_encoder.h
            src/liblzma/lzma/lzma_encoder_optimum_fast.c
            src/liblzma/lzma/lzma_encoder_optimum_normal.c
            src/liblzma/lzma/lzma_encoder_private.h
            src/liblzma/lzma/fastpos.h
            src/liblzma/lz/lz_encoder.c
            src/liblzma/lz/lz_encoder.h
            src/liblzma/lz/lz_encoder_hash.h
            src/liblzma/lz/lz_encoder_hash_table.h
            src/liblzma/lz/lz_encoder_mf.c
            src/liblzma/rangecoder/price.h
            src/liblzma/rangecoder/price_table.c
            src/liblzma/rangecoder/range_encoder.h
        )

        if(NOT XZ_SMALL)
            target_sources(liblzma PRIVATE src/liblzma/lzma/fastpos_table.c)
        endif()
    endif()

    if("lzma2" IN_LIST XZ_ENCODERS)
        target_sources(liblzma PRIVATE
            src/liblzma/lzma/lzma2_encoder.c
            src/liblzma/lzma/lzma2_encoder.h
        )
    endif()

    if("delta" IN_LIST XZ_ENCODERS)
        set(HAVE_DELTA_CODER ON)
        target_sources(liblzma PRIVATE
            src/liblzma/delta/delta_encoder.c
            src/liblzma/delta/delta_encoder.h
        )
    endif()
endif()


############
# Decoders #
############

set(XZ_DECODERS "${SUPPORTED_FILTERS}" CACHE STRING "Decoders to support")

set(SIMPLE_DECODERS OFF)
set(HAVE_DECODERS OFF)

foreach(DECODER IN LISTS XZ_DECODERS)
    if(DECODER IN_LIST SUPPORTED_FILTERS)
        set(HAVE_DECODERS ON)

        if(NOT SIMPLE_DECODERS AND DECODER IN_LIST SIMPLE_FILTERS)
            set(SIMPLE_DECODERS ON)
        endif()

        string(TOUPPER "${DECODER}" DECODER_UPPER)
        add_compile_definitions("HAVE_DECODER_${DECODER_UPPER}")
    else()
        message(FATAL_ERROR "'${DECODER}' is not a supported decoder")
    endif()
endforeach()

if(HAVE_DECODERS)
    add_compile_definitions(HAVE_DECODERS)

    target_sources(liblzma PRIVATE
        src/liblzma/common/alone_decoder.c
        src/liblzma/common/alone_decoder.h
        src/liblzma/common/auto_decoder.c
        src/liblzma/common/block_buffer_decoder.c
        src/liblzma/common/block_decoder.c
        src/liblzma/common/block_decoder.h
        src/liblzma/common/block_header_decoder.c
        src/liblzma/common/easy_decoder_memusage.c
        src/liblzma/common/file_info.c
        src/liblzma/common/filter_buffer_decoder.c
        src/liblzma/common/filter_decoder.c
        src/liblzma/common/filter_decoder.h
        src/liblzma/common/filter_flags_decoder.c
        src/liblzma/common/index_decoder.c
        src/liblzma/common/index_decoder.h
        src/liblzma/common/index_hash.c
        src/liblzma/common/stream_buffer_decoder.c
        src/liblzma/common/stream_decoder.c
        src/liblzma/common/stream_flags_decoder.c
        src/liblzma/common/stream_decoder.h
        src/liblzma/common/vli_decoder.c
    )

    if(XZ_THREADS)
        target_sources(liblzma PRIVATE
            src/liblzma/common/stream_decoder_mt.c
        )
    endif()

    if(SIMPLE_DECODERS)
        target_sources(liblzma PRIVATE
            src/liblzma/simple/simple_decoder.c
            src/liblzma/simple/simple_decoder.h
        )
    endif()

    if("lzma1" IN_LIST XZ_DECODERS)
        target_sources(liblzma PRIVATE
            src/liblzma/lzma/lzma_decoder.c
            src/liblzma/lzma/lzma_decoder.h
            src/liblzma/rangecoder/range_decoder.h
            src/liblzma/lz/lz_decoder.c
            src/liblzma/lz/lz_decoder.h
        )
    endif()

    if("lzma2" IN_LIST XZ_DECODERS)
        target_sources(liblzma PRIVATE
            src/liblzma/lzma/lzma2_decoder.c
            src/liblzma/lzma/lzma2_decoder.h
        )
    endif()

    if("delta" IN_LIST XZ_DECODERS)
        set(HAVE_DELTA_CODER ON)
        target_sources(liblzma PRIVATE
            src/liblzma/delta/delta_decoder.c
            src/liblzma/delta/delta_decoder.h
        )
    endif()
endif()

# Some sources must appear if the filter is configured as either
# an encoder or decoder.
if("lzma1" IN_LIST XZ_ENCODERS OR "lzma1" IN_LIST XZ_DECODERS)
    target_sources(liblzma PRIVATE
        src/liblzma/rangecoder/range_common.h
        src/liblzma/lzma/lzma_encoder_presets.c
        src/liblzma/lzma/lzma_common.h
    )
endif()

if(HAVE_DELTA_CODER)
    target_sources(liblzma PRIVATE
        src/liblzma/delta/delta_common.c
        src/liblzma/delta/delta_common.h
        src/liblzma/delta/delta_private.h
    )
endif()

if(SIMPLE_ENCODERS OR SIMPLE_DECODERS)
    target_sources(liblzma PRIVATE
        src/liblzma/simple/simple_coder.c
        src/liblzma/simple/simple_coder.h
        src/liblzma/simple/simple_private.h
    )
endif()

foreach(SIMPLE_CODER IN LISTS SIMPLE_FILTERS)
    if(SIMPLE_CODER IN_LIST XZ_ENCODERS OR SIMPLE_CODER IN_LIST XZ_DECODERS)
        target_sources(liblzma PRIVATE "src/liblzma/simple/${SIMPLE_CODER}.c")
    endif()
endforeach()


#############
# MicroLZMA #
#############

option(XZ_MICROLZMA_ENCODER
       "MicroLZMA encoder (needed by specific applications only)" ON)

option(XZ_MICROLZMA_DECODER
       "MicroLZMA decoder (needed by specific applications only)" ON)

if(XZ_MICROLZMA_ENCODER)
    if(NOT "lzma1" IN_LIST XZ_ENCODERS)
        message(FATAL_ERROR "The LZMA1 encoder is required to support the "
                            "MicroLZMA encoder")
    endif()

    target_sources(liblzma PRIVATE src/liblzma/common/microlzma_encoder.c)
endif()

if(XZ_MICROLZMA_DECODER)
    if(NOT "lzma1" IN_LIST XZ_DECODERS)
        message(FATAL_ERROR "The LZMA1 decoder is required to support the "
                            "MicroLZMA decoder")
    endif()

    target_sources(liblzma PRIVATE src/liblzma/common/microlzma_decoder.c)
endif()


#############################
# lzip (.lz) format support #
#############################

option(XZ_LZIP_DECODER "Support lzip decoder" ON)

if(XZ_LZIP_DECODER)
    # If lzip decoder support is requested, make sure LZMA1 decoder is enabled.
    if(NOT "lzma1" IN_LIST XZ_DECODERS)
        message(FATAL_ERROR "The LZMA1 decoder is required to support the "
                            "lzip decoder")
    endif()

    add_compile_definitions(HAVE_LZIP_DECODER)

    target_sources(liblzma PRIVATE
        src/liblzma/common/lzip_decoder.c
        src/liblzma/common/lzip_decoder.h
    )
endif()

###

# Put the tuklib functions under the lzma_ namespace.
target_compile_definitions(liblzma PRIVATE TUKLIB_SYMBOL_PREFIX=lzma_)
tuklib_cpucores(liblzma)
tuklib_physmem(liblzma)

# While liblzma can be built without tuklib_cpucores or tuklib_physmem
# modules, the liblzma API functions lzma_cputhreads() and lzma_physmem()
# will then be useless (which isn't too bad but still unfortunate). Since
# I expect the CMake-based builds to be only used on systems that are
# supported by these tuklib modules, problems with these tuklib modules
# are considered a hard error for now. This hopefully helps to catch bugs
# in the CMake versions of the tuklib checks.
if(NOT TUKLIB_CPUCORES_FOUND OR NOT TUKLIB_PHYSMEM_FOUND)
    # Use SEND_ERROR instead of FATAL_ERROR. If someone reports a bug,
    # seeing the results of the remaining checks can be useful too.
    message(SEND_ERROR
            "tuklib_cpucores() or tuklib_physmem() failed. "
            "Unless you really are building for a system where these "
            "modules are not supported (unlikely), this is a bug in the "
            "included cmake/tuklib_*.cmake files that should be fixed. "
            "To build anyway, edit this CMakeLists.txt to ignore this error.")
endif()

# Check for __attribute__((__constructor__)) support.
# This needs -Werror because some compilers just warn
# about this being unsupported.
cmake_push_check_state()
set(CMAKE_REQUIRED_FLAGS "-Werror")
check_c_source_compiles("
        __attribute__((__constructor__))
        static void my_constructor_func(void) { return; }
        int main(void) { return 0; }
    "
    HAVE_FUNC_ATTRIBUTE_CONSTRUCTOR)
cmake_pop_check_state()
tuklib_add_definition_if(liblzma HAVE_FUNC_ATTRIBUTE_CONSTRUCTOR)

# The Win95 threading lacks a thread-safe one-time initialization function.
# The one-time initialization is needed for crc32_small.c and crc64_small.c
# create the CRC tables. So if small mode is enabled, the threading mode is
# win95, and the compiler does not support attribute constructor, then we
# would end up with a multithreaded build that is thread-unsafe. As a
# result this configuration is not allowed.
if(USE_WIN95_THREADS AND XZ_SMALL AND NOT HAVE_FUNC_ATTRIBUTE_CONSTRUCTOR)
    message(SEND_ERROR "Threading method win95 and XZ_SMALL "
                       "cannot be used at the same time because the compiler "
                       "doesn't support __attribute__((__constructor__))")
endif()


# cpuid.h
check_include_file(cpuid.h HAVE_CPUID_H)
tuklib_add_definition_if(liblzma HAVE_CPUID_H)

# immintrin.h:
check_include_file(immintrin.h HAVE_IMMINTRIN_H)
if(HAVE_IMMINTRIN_H)
    target_compile_definitions(liblzma PRIVATE HAVE_IMMINTRIN_H)

    # SSE2 intrinsics:
    check_c_source_compiles("
            #include <immintrin.h>
            int main(void)
            {
                __m128i x = { 0 };
                _mm_movemask_epi8(x);
                return 0;
            }
        "
        HAVE__MM_MOVEMASK_EPI8)
    tuklib_add_definition_if(liblzma HAVE__MM_MOVEMASK_EPI8)

    # CLMUL intrinsic:
    option(XZ_CLMUL_CRC "Use carryless multiplication for CRC \
calculation (with runtime detection) if supported by the compiler" ON)

    if(XZ_CLMUL_CRC)
        check_c_source_compiles("
                #include <immintrin.h>
                #if defined(__e2k__) && __iset__ < 6
                #   error
                #endif
                #if (defined(__GNUC__) || defined(__clang__)) \
                        && !defined(__EDG__)
                __attribute__((__target__(\"ssse3,sse4.1,pclmul\")))
                #endif
                int main(void)
                {
                    __m128i a = _mm_set_epi64x(1, 2);
                    a = _mm_clmulepi64_si128(a, a, 0);
                    return 0;
                }
            "
            HAVE_USABLE_CLMUL)
        tuklib_add_definition_if(liblzma HAVE_USABLE_CLMUL)
    endif()
endif()

# ARM64 C Language Extensions define CRC32 functions in arm_acle.h.
# These are supported by at least GCC and Clang which both need
# __attribute__((__target__("+crc"))), unless the needed compiler flags
# are used to support the CRC instruction.
option(XZ_ARM64_CRC32 "Use ARM64 CRC32 instructions (with runtime detection) \
if supported by the compiler" ON)

if(XZ_ARM64_CRC32)
    check_c_source_compiles("
            #include <stdint.h>

            #ifndef _MSC_VER
            #include <arm_acle.h>
            #endif

            #if (defined(__GNUC__) || defined(__clang__)) && !defined(__EDG__)
            __attribute__((__target__(\"+crc\")))
            #endif
            int main(void)
            {
                return __crc32d(1, 2) != 0;
            }
        "
        HAVE_ARM64_CRC32)

    if(HAVE_ARM64_CRC32)
        target_compile_definitions(liblzma PRIVATE HAVE_ARM64_CRC32)

        # Check for ARM64 CRC32 instruction runtime detection.
        # getauxval() is supported on Linux.
        check_symbol_exists(getauxval sys/auxv.h HAVE_GETAUXVAL)
        tuklib_add_definition_if(liblzma HAVE_GETAUXVAL)

        # With getauxval() we also need HWCAP_CRC32 which was
        # added in glibc 2.24.
        if(HAVE_GETAUXVAL)
            check_symbol_exists(HWCAP_CRC32 sys/auxv.h HAVE_HWCAP_CRC32)
            tuklib_add_definition_if(liblzma HAVE_HWCAP_CRC32)
        endif()

        # elf_aux_info() is supported on FreeBSD and OpenBSD >= 7.6.
        check_symbol_exists(elf_aux_info sys/auxv.h HAVE_ELF_AUX_INFO)
        tuklib_add_definition_if(liblzma HAVE_ELF_AUX_INFO)

        # sysctlbyname("hw.optional.armv8_crc32", ...) is supported on Darwin
        # (macOS, iOS, etc.). Note that sysctlbyname() is supported on FreeBSD,
        # NetBSD, and possibly others too but the string is specific to
        # Apple OSes. The C code is responsible for checking
        # defined(__APPLE__) before using
        # sysctlbyname("hw.optional.armv8_crc32", ...).
        check_symbol_exists(sysctlbyname sys/sysctl.h HAVE_SYSCTLBYNAME)
        tuklib_add_definition_if(liblzma HAVE_SYSCTLBYNAME)
    endif()
endif()

option(XZ_LOONGARCH_CRC32
       "Use LoongArch CRC32 instructions if supported by the compiler" ON)

if(XZ_LOONGARCH_CRC32)
    # LoongArch CRC32 intrinsics are in larchintrin.h.
    # These are supported by at least GCC and Clang.
    #
    # Only 64-bit LoongArch is currently supported.
    # It doesn't need runtime detection.
    check_c_source_compiles("
            #if !(defined(__loongarch__) && __loongarch_grlen >= 64)
            #   error
            #endif

            #include <larchintrin.h>
            int main(void)
            {
                return __crc_w_w_w(1, 2);
            }
        "
        HAVE_LOONGARCH_CRC32)
    tuklib_add_definition_if(liblzma HAVE_LOONGARCH_CRC32)
endif()


# Symbol visibility support:
#
# The C_VISIBILITY_PRESET property takes care of adding the compiler
# option -fvisibility=hidden (or equivalent) if and only if it is supported.
#
# HAVE_VISIBILITY should always be defined to 0 or 1. It tells liblzma
# if __attribute__((__visibility__("default")))
# and __attribute__((__visibility__("hidden"))) are supported.
# Those are useful only when the compiler supports -fvisibility=hidden
# or such option so HAVE_VISIBILITY should be 1 only when both option and
# the attribute support are present. HAVE_VISIBILITY is ignored on Windows
# and Cygwin by the liblzma C code; __declspec(dllexport) is used instead.
#
# CMake's GenerateExportHeader module is too fancy since liblzma already
# has the necessary macros. Instead, check CMake's internal variable
# CMAKE_C_COMPILE_OPTIONS_VISIBILITY (it's the C-specific variant of
# CMAKE_<LANG>_COMPILE_OPTIONS_VISIBILITY) which contains the compiler
# command line option for visibility support. It's empty or unset when
# visibility isn't supported. (It was added to CMake 2.8.12 in the commit
# 0e9f4bc00c6b26f254e74063e4026ac33b786513 in 2013.) This way we don't
# set HAVE_VISIBILITY to 1 when visibility isn't actually supported.
if(BUILD_SHARED_LIBS AND CMAKE_C_COMPILE_OPTIONS_VISIBILITY)
    set_target_properties(liblzma PROPERTIES C_VISIBILITY_PRESET hidden)
    target_compile_definitions(liblzma PRIVATE HAVE_VISIBILITY=1)
else()
    target_compile_definitions(liblzma PRIVATE HAVE_VISIBILITY=0)
endif()

if(WIN32 OR CYGWIN)
    if(BUILD_SHARED_LIBS)
        # Add the Windows resource file for liblzma.dll.
        target_sources(liblzma PRIVATE src/liblzma/liblzma_w32res.rc)

        set_source_files_properties(src/liblzma/liblzma_w32res.rc PROPERTIES
            OBJECT_DEPENDS "${W32RES_DEPENDENCIES}"
        )

        # Export the public API symbols with __declspec(dllexport).
        target_compile_definitions(liblzma PRIVATE DLL_EXPORT)

        if(NOT MSVC AND NOT CYGWIN)
            # Create a DEF file. The Autotools-based build creates a DEF file
            # under Cygwin & MSYS2 too but it almost certainly is a useless
            # file in that context, so the CMake build omits it.
            #
            # The linker puts the ordinal numbers in the DEF file
            # too so the output from the linker isn't our final file.
            target_link_options(liblzma PRIVATE
                                "-Wl,--output-def,liblzma.def.in")

            # Remove the ordinal numbers from the DEF file so that
            # no one will create an import library that links by ordinal
            # instead of by name. We don't maintain a DEF file so the
            # ordinal numbers aren't stable.
            add_custom_command(TARGET liblzma POST_BUILD
                COMMAND "${CMAKE_COMMAND}"
                    -DINPUT_FILE=liblzma.def.in
                    -DOUTPUT_FILE=liblzma.def
                    -P
                    "${CMAKE_CURRENT_SOURCE_DIR}/cmake/remove-ordinals.cmake"
                BYPRODUCTS "liblzma.def"
                VERBATIM)
        endif()
    else()
        # Disable __declspec(dllimport) when linking against static liblzma.
        target_compile_definitions(liblzma INTERFACE LZMA_API_STATIC)
    endif()
elseif(BUILD_SHARED_LIBS AND SYMBOL_VERSIONING STREQUAL "linux")
    # Note that adding link options doesn't affect static builds
    # but HAVE_SYMBOL_VERSIONS_LINUX must not be used with static builds
    # because it would put symbol versions into the static library which
    # can cause problems. It's clearer if all symver related things are
    # omitted when not building a shared library.
    #
    # NOTE: Set it explicitly to 1 to make it clear that versioning is
    # done unconditionally in the C files.
    target_compile_definitions(liblzma PRIVATE HAVE_SYMBOL_VERSIONS_LINUX=1)
    if(HAVE_LINKER_FLAG_UNDEFINED_VERSION)
        target_link_options(liblzma PRIVATE "-Wl,--undefined-version")
    endif()
    target_link_options(liblzma PRIVATE
        "-Wl,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/src/liblzma/liblzma_linux.map"
    )
    set_target_properties(liblzma PROPERTIES
        LINK_DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/src/liblzma/liblzma_linux.map"
    )
elseif(BUILD_SHARED_LIBS AND SYMBOL_VERSIONING STREQUAL "generic")
    if(HAVE_LINKER_FLAG_UNDEFINED_VERSION)
        target_link_options(liblzma PRIVATE "-Wl,--undefined-version")
    endif()
    target_link_options(liblzma PRIVATE
        "-Wl,--version-script=${CMAKE_CURRENT_SOURCE_DIR}/src/liblzma/liblzma_generic.map"
    )
    set_target_properties(liblzma PROPERTIES
        LINK_DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/src/liblzma/liblzma_generic.map"
    )
endif()

# Calculate Libtool-compatible Mach-O versions for Apple OSes.
# Switching from CMake's or Meson's default Mach-O versioning style to
# GNU Libtool style is always a backward compatible change because the
# Libtool style always results in higher Mach-O version values.
#
# The other way would be a breaking change, and switching from Autotools
# to CMake used to result in "Incompatible library version" error from the
# dynamic linker ("dyld") when trying to run existing executables. This
# happened because the Mach-O current_version in the CMake-built library
# was less than the compatibility_version stored in the executable.
#
# Example: If on GNU/Linux one had libfoo.so.5.6.7, on macOS one would
# have libfoo.5.dylib containing the following Mach-O versions:
#
#                compatibility_version    current_version
#     Libtool    12.0.0                   12.7.0
#     CMake      5.0.0                    5.6.7
#     Meson      5.0.0                    5.0.0
#
# Apple's docs say that the major version is encoded in the library filename,
# and the Mach-O version fields are for tracking backward compatible changes
# (minor versions). The default Mach-O versioning styles in CMake and
# Meson don't store the minor version in the compatibility_version though
# but Libtool does (using its own idiosyncratic encoding). In practice the
# lack of minor ABI version tracking doesn't matter much; it's the
# compatibility between the build systems that counts.
#
# It's unclear how much this matters in 2024. It might be that the
# dynamic linker in macOS >= 12 doesn't enforce the version checks.
# But this is a simple and safe change so it's fine to do it anyway.
#
# Apple docs:
# https://developer.apple.com/library/archive/documentation/DeveloperTools/Conceptual/DynamicLibraries/100-Articles/DynamicLibraryDesignGuidelines.html
#
# This change was made in XZ Utils 5.7.1alpha.
#
# * * * * *
#
# At least for now the xz package versioning matches the rules used for
# shared library versioning (excluding development releases) so it is
# fine to use the package version when setting the liblzma ABI version.
math(EXPR LIBLZMA_MACHO_COMPATIBILITY_VERSION
          "${xz_VERSION_MAJOR} + ${xz_VERSION_MINOR} + 1")
set(LIBLZMA_MACHO_CURRENT_VERSION
    "${LIBLZMA_MACHO_COMPATIBILITY_VERSION}.${xz_VERSION_PATCH}")

set_target_properties(liblzma PROPERTIES
    SOVERSION "${xz_VERSION_MAJOR}"
    VERSION "${xz_VERSION}"
    MACHO_COMPATIBILITY_VERSION "${LIBLZMA_MACHO_COMPATIBILITY_VERSION}"
    MACHO_CURRENT_VERSION "${LIBLZMA_MACHO_CURRENT_VERSION}"

    # The name liblzma a mess because in many places "lib" is just a prefix
    # and not part of the actual name. (Don't name a new library this way!)
    # Cygwin uses "cyg", MSYS2 uses "msys-", and some platforms use no prefix.
    # However, we want to avoid lzma.dll on Windows as that would conflict
    # with LZMA SDK. liblzma has been liblzma.dll on Windows since the
    # beginning so try to stick with it.
    #
    # Up to XZ Utils 5.6.2 we set PREFIX and IMPORT_PREFIX properties to ""
    # while keeping the default "liblzma" OUTPUT_NAME that was derived from
    # the target name. But this broke naming on Cygwin and MSYS2.
    #
    # Setting OUTPUT_NAME without the "lib" prefix means that CMake will add
    # the platform-specific prefix as needed. So on most systems CMake will
    # add "lib" but on Cygwin and MSYS2 the naming will be correct too.
    #
    # On Windows, CMake uses the "lib" prefix with MinGW-w64 but not with
    # other toolchains. Those need to be handled specially to get the DLL
    # file named liblzma.dll instead of lzma.dll.
    OUTPUT_NAME "lzma"
)

if(WIN32 AND NOT MINGW)
    # Up to XZ Utils 5.6.2 and building with MSVC, we produced liblzma.dll
    # and liblzma.lib. The downside of liblzma.lib is that it's not
    # compatible with pkgconf usage. liblzma.pc contains "-llzma" which
    # "pkgconf --msvc-syntax --libs liblzma" converts to "lzma.lib".
    # So as a compromise, we can keep the liblzma.dll name but the import
    # library and static liblzma need to be named lzma.lib so that pkgconf
    # can be used with MSVC. (MinGW-w64 finds both names with "-llzma".)
    set_target_properties(liblzma PROPERTIES RUNTIME_OUTPUT_NAME "liblzma")
endif()

# Create liblzma-config-version.cmake.
#
# NOTE: SameMajorVersion is correct for stable releases but it is wrong
# for development releases where each release may have incompatible changes.
include(CMakePackageConfigHelpers)
write_basic_package_version_file(
    "${CMAKE_CURRENT_BINARY_DIR}/liblzma-config-version.cmake"
    VERSION "${xz_VERSION}"
    COMPATIBILITY SameMajorVersion)

# Create liblzma-config.cmake. We use this spelling instead of
# liblzmaConfig.cmake to make find_package work in case insensitive
# manner even with case sensitive file systems. This gives more consistent
# behavior between operating systems. This optionally includes a dependency
# on a threading library, so the contents are created in two separate parts.
# The "second half" is always needed, so create it first.
set(LZMA_CONFIG_CONTENTS
"include(\"\${CMAKE_CURRENT_LIST_DIR}/liblzma-targets.cmake\")

if(NOT TARGET LibLZMA::LibLZMA)
    # Be compatible with the spelling used by the FindLibLZMA module. This
    # doesn't use ALIAS because it would make CMake resolve LibLZMA::LibLZMA
    # to liblzma::liblzma instead of keeping the original spelling. Keeping
    # the original spelling is important for good FindLibLZMA compatibility.
    add_library(LibLZMA::LibLZMA INTERFACE IMPORTED)
    set_target_properties(LibLZMA::LibLZMA PROPERTIES
                          INTERFACE_LINK_LIBRARIES liblzma::liblzma)
endif()
")

if(USE_POSIX_THREADS)
    set(LZMA_CONFIG_CONTENTS
"include(CMakeFindDependencyMacro)
set(THREADS_PREFER_PTHREAD_FLAG TRUE)
find_dependency(Threads)

${LZMA_CONFIG_CONTENTS}
")
endif()

file(WRITE "${CMAKE_CURRENT_BINARY_DIR}/liblzma-config.cmake"
        "${LZMA_CONFIG_CONTENTS}")


# Create liblzma.pc. If CMAKE_INSTALL_<dir> paths are relative to
# CMAKE_INSTALL_PREFIX, the .pc file will be relocatable (that is,
# all paths will be relative to ${prefix}). Otherwise absolute
# paths will be used.
set(prefix "${CMAKE_INSTALL_PREFIX}")
set(exec_prefix "\${prefix}")
cmake_path(APPEND libdir "\${exec_prefix}" "${CMAKE_INSTALL_LIBDIR}")
cmake_path(APPEND includedir "\${prefix}" "${CMAKE_INSTALL_INCLUDEDIR}")

# Threads::Threads is linked in only when using POSIX threads.
# Use an empty value if using Windows threads or if threading is disabled.
set(PTHREAD_CFLAGS)
if(USE_POSIX_THREADS)
    set(PTHREAD_CFLAGS "${CMAKE_THREAD_LIBS_INIT}")
endif()

configure_file(src/liblzma/liblzma.pc.in liblzma.pc @ONLY)


# Install the library binary. The INCLUDES specifies the include path that
# is exported for other projects to use but it doesn't install any files.
install(TARGETS liblzma EXPORT liblzmaTargets
        RUNTIME  DESTINATION "${CMAKE_INSTALL_BINDIR}"
                 COMPONENT liblzma_Runtime
        LIBRARY  DESTINATION "${CMAKE_INSTALL_LIBDIR}"
                 COMPONENT liblzma_Runtime
                 NAMELINK_COMPONENT liblzma_Development
        ARCHIVE  DESTINATION "${CMAKE_INSTALL_LIBDIR}"
                 COMPONENT liblzma_Development
        INCLUDES DESTINATION "${CMAKE_INSTALL_INCLUDEDIR}")

# Install the liblzma API headers. These use a subdirectory so
# this has to be done as a separate step.
install(DIRECTORY src/liblzma/api/
        COMPONENT liblzma_Development
        DESTINATION "${CMAKE_INSTALL_INCLUDEDIR}"
        FILES_MATCHING PATTERN "*.h")

# Install the CMake files that other packages can use to find liblzma.
set(XZ_INSTALL_CMAKEDIR
    "${CMAKE_INSTALL_LIBDIR}/cmake/liblzma"
    CACHE STRING "Path to liblzma's .cmake files")

install(EXPORT liblzmaTargets
        NAMESPACE liblzma::
        FILE liblzma-targets.cmake
        DESTINATION "${XZ_INSTALL_CMAKEDIR}"
        COMPONENT liblzma_Development)

install(FILES "${CMAKE_CURRENT_BINARY_DIR}/liblzma-config.cmake"
              "${CMAKE_CURRENT_BINARY_DIR}/liblzma-config-version.cmake"
        DESTINATION "${XZ_INSTALL_CMAKEDIR}"
        COMPONENT liblzma_Development)

install(FILES "${CMAKE_CURRENT_BINARY_DIR}/liblzma.pc"
        DESTINATION "${CMAKE_INSTALL_LIBDIR}/pkgconfig"
        COMPONENT liblzma_Development)


#############################################################################
# Helper functions for installing files
#############################################################################

# For each non-empty element in the list LINK_NAMES, creates symbolic links
# ${LINK_NAME}${LINK_SUFFIX} -> ${TARGET_NAME} in the directory ${DIR}.
# The target file should exist because on Cygwin and MSYS2 symlink creation
# can fail under certain conditions if the target doesn't exist.
function(my_install_symlinks COMPONENT DIR TARGET_NAME LINK_SUFFIX LINK_NAMES)
    install(CODE "set(D \"\$ENV{DESTDIR}\${CMAKE_INSTALL_PREFIX}/${DIR}\")
                 foreach(L ${LINK_NAMES})
                     file(CREATE_LINK \"${TARGET_NAME}\"
                                      \"\${D}/\${L}${LINK_SUFFIX}\"
                                      SYMBOLIC)
                 endforeach()"
            COMPONENT "${COMPONENT}")
endfunction()

# Installs a man page file of a given language ("" for the untranslated file)
# and optionally its alternative names as symlinks. This is a helper function
# for my_install_man() below.
function(my_install_man_lang COMPONENT SRC_FILE MAN_LANG LINK_NAMES)
    # Get the man page section from the filename suffix.
    string(REGEX REPLACE "^.*\.([^/.]+)$" "\\1" MAN_SECTION "${SRC_FILE}")

    # A few man pages might be missing from translations.
    # Don't attempt to install them or create the related symlinks.
    if(NOT MAN_LANG STREQUAL "" AND NOT EXISTS "${SRC_FILE}")
        return()
    endif()

    # Installing the file must be done before creating the symlinks
    # due to Cygwin and MSYS2.
    install(FILES "${SRC_FILE}"
            DESTINATION "${CMAKE_INSTALL_MANDIR}/${MAN_LANG}/man${MAN_SECTION}"
            COMPONENT "${COMPONENT}")

    # Get the basename of the file to be used as the symlink target.
    get_filename_component(BASENAME "${SRC_FILE}" NAME)

    # LINK_NAMES don't contain the man page filename suffix (like ".1")
    # so it needs to be told to my_install_symlinks.
    my_install_symlinks("${COMPONENT}"
                        "${CMAKE_INSTALL_MANDIR}/${MAN_LANG}/man${MAN_SECTION}"
                        "${BASENAME}" ".${MAN_SECTION}" "${LINK_NAMES}")
endfunction()

# Installs a man page file and optionally its alternative names as symlinks.
# Does the same for translations if XZ_NLS.
function(my_install_man COMPONENT SRC_FILE LINK_NAMES)
    my_install_man_lang("${COMPONENT}" "${SRC_FILE}" "" "${LINK_NAMES}")

    if(XZ_NLS)
        # Find the translated versions of this man page.
        get_filename_component(BASENAME "${SRC_FILE}" NAME)
        file(GLOB MAN_FILES "po4a/man/*/${BASENAME}")

        foreach(F ${MAN_FILES})
            get_filename_component(MAN_LANG "${F}" DIRECTORY)
            get_filename_component(MAN_LANG "${MAN_LANG}" NAME)
            my_install_man_lang("${COMPONENT}" "${F}" "${MAN_LANG}"
                                "${LINK_NAMES}")
        endforeach()
    endif()
endfunction()


#############################################################################
# libgnu (getopt_long)
#############################################################################

# This mirrors how the Autotools build system handles the getopt_long
# replacement, calling the object library libgnu since the replacement
# version comes from Gnulib.
add_library(libgnu OBJECT)

# CMake requires that even an object library must have at least once source
# file. So we give it a header file that results in no output files.
#
# NOTE: Using a file outside the lib directory makes it possible to
# delete lib/*.h and lib/*.c and still keep the build working if
# getopt_long replacement isn't needed. It's convenient if one wishes
# to be certain that no GNU LGPL code gets included in the binaries.
target_sources(libgnu PRIVATE src/common/sysdefs.h)

# The Ninja Generator requires setting the linker language since it cannot
# guess the programming language of just a header file. Setting this
# property avoids needing an empty .c file or an non-empty unnecessary .c
# file.
set_target_properties(libgnu PROPERTIES LINKER_LANGUAGE C)

# Create /lib directory in the build directory and add it to the include path.
file(MAKE_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/lib")
target_include_directories(libgnu PUBLIC "${CMAKE_CURRENT_BINARY_DIR}/lib")

# Include /lib from the source directory. It does no harm even if none of
# the Gnulib replacements are used.
target_include_directories(libgnu PUBLIC lib)

# The command line tools need getopt_long in order to parse arguments. If
# the system does not have a getopt_long implementation we can use the one
# from Gnulib instead.
check_symbol_exists(getopt_long getopt.h HAVE_GETOPT_LONG)

if(NOT HAVE_GETOPT_LONG)
    # Set the __GETOPT_PREFIX definition to "rpl_" (replacement) to avoid
    # name conflicts with libc symbols. The same prefix is set if using
    # the Autotools build (m4/getopt.m4).
    target_compile_definitions(libgnu PUBLIC "__GETOPT_PREFIX=rpl_")

    # Copy the getopt header to the build directory and re-copy it
    # if it is updated. (Gnulib does it this way because it allows
    # choosing which .in.h files to actually use in the build. We
    # need just getopt.h so this is a bit overcomplicated for
    # a single header file only.)
    configure_file("${CMAKE_CURRENT_SOURCE_DIR}/lib/getopt.in.h"
                   "${CMAKE_CURRENT_BINARY_DIR}/lib/getopt.h"
                   COPYONLY)

    target_sources(libgnu PRIVATE
        lib/getopt1.c
        lib/getopt.c
        lib/getopt_int.h
        lib/getopt-cdefs.h
        lib/getopt-core.h
        lib/getopt-ext.h
        lib/getopt-pfx-core.h
        lib/getopt-pfx-ext.h
        "${CMAKE_CURRENT_BINARY_DIR}/lib/getopt.h"
    )
endif()


#############################################################################
# Sandboxing for the command line tools
#############################################################################

# auto      Use sandboxing if a supported method is available in the OS.
# no        Disable sandboxing.
# capsicum  Require Capsicum (FreeBSD >= 10.2) and fail if not found.
# pledge    Require pledge(2) (OpenBSD >= 5.9) and fail if not found.
# landlock  Require Landlock (Linux >= 5.13) and fail if not found.
set(SUPPORTED_SANDBOX_METHODS auto no capsicum pledge landlock)

set(XZ_SANDBOX auto CACHE STRING
    "Sandboxing method to use in 'xz', 'xzdec', and 'lzmadec'")

set_property(CACHE XZ_SANDBOX PROPERTY STRINGS "${SUPPORTED_SANDBOX_METHODS}")

if(NOT XZ_SANDBOX IN_LIST SUPPORTED_SANDBOX_METHODS)
    message(FATAL_ERROR "'${XZ_SANDBOX}' is not a supported "
                        "sandboxing method")
endif()

# When autodetecting, the search order is fixed and we must not find
# more than one method.
if(XZ_SANDBOX STREQUAL "no")
    set(SANDBOX_FOUND ON)
else()
    set(SANDBOX_FOUND OFF)
endif()

# Since xz and xzdec can both use sandboxing, the compile definition needed
# to use the sandbox must be added to both targets.
set(SANDBOX_COMPILE_DEFINITION OFF)

# Sandboxing: Capsicum
if(NOT SANDBOX_FOUND AND XZ_SANDBOX MATCHES "^auto$|^capsicum$")
    check_symbol_exists(cap_rights_limit sys/capsicum.h
                        HAVE_CAP_RIGHTS_LIMIT)
    if(HAVE_CAP_RIGHTS_LIMIT)
        set(SANDBOX_COMPILE_DEFINITION "HAVE_CAP_RIGHTS_LIMIT")
        set(SANDBOX_FOUND ON)
    endif()
endif()

# Sandboxing: pledge(2)
if(NOT SANDBOX_FOUND AND XZ_SANDBOX MATCHES "^auto$|^pledge$")
    check_symbol_exists(pledge unistd.h HAVE_PLEDGE)
    if(HAVE_PLEDGE)
        set(SANDBOX_COMPILE_DEFINITION "HAVE_PLEDGE")
        set(SANDBOX_FOUND ON)
    endif()
endif()

# Sandboxing: Landlock
if(NOT SANDBOX_FOUND AND XZ_SANDBOX MATCHES "^auto$|^landlock$")
    # A compile check is done here because some systems have
    # linux/landlock.h, but do not have the syscalls defined
    # in order to actually use Linux Landlock.
    check_c_source_compiles("
            #include <linux/landlock.h>
            #include <sys/syscall.h>
            #include <sys/prctl.h>

            int main(void)
            {
                (void)prctl(PR_SET_NO_NEW_PRIVS, 1, 0, 0, 0);
                (void)SYS_landlock_create_ruleset;
                (void)SYS_landlock_restrict_self;
                (void)LANDLOCK_CREATE_RULESET_VERSION;
                return 0;
            }
        "
        HAVE_LINUX_LANDLOCK)

    if(HAVE_LINUX_LANDLOCK)
        set(SANDBOX_COMPILE_DEFINITION "HAVE_LINUX_LANDLOCK")
        set(SANDBOX_FOUND ON)

        # Of our three sandbox methods, only Landlock is incompatible
        # with -fsanitize. FreeBSD 13.2 with Capsicum was tested with
        # -fsanitize=address,undefined and had no issues. OpenBSD (as
        # of version 7.4) has minimal support for process instrumentation.
        # OpenBSD does not distribute the additional libraries needed
        # (libasan, libubsan, etc.) with GCC or Clang needed for runtime
        # sanitization support and instead only support
        # -fsanitize-minimal-runtime for minimal undefined behavior
        # sanitization. This minimal support is compatible with our use
        # of the Pledge sandbox. So only Landlock will result in a
        # build that cannot compress or decompress a single file to
        # standard out.
        if(CMAKE_C_FLAGS MATCHES "-fsanitize=")
            message(SEND_ERROR
                    "CMAKE_C_FLAGS or the environment variable CFLAGS "
                    "contains '-fsanitize=' which is incompatible "
                    "with Landlock sandboxing. Use -DXZ_SANDBOX=no "
                    "as an argument to 'cmake' when using '-fsanitize'.")
        endif()
    endif()
endif()

if(NOT SANDBOX_FOUND AND NOT XZ_SANDBOX MATCHES "^auto$|^no$")
    message(SEND_ERROR "XZ_SANDBOX=${XZ_SANDBOX} was used but "
                        "support for the sandboxing method wasn't found.")
endif()


#############################################################################
# xzdec and lzmadec
#############################################################################

option(XZ_TOOL_XZDEC "Build and install the xzdec command line tool" ON)
option(XZ_TOOL_LZMADEC "Build and install the lzmadec command line tool" ON)

if(HAVE_DECODERS)
    set(XZDEC_TOOLS)

    if(XZ_TOOL_XZDEC)
        list(APPEND XZDEC_TOOLS xzdec)
    endif()

    if(XZ_TOOL_LZMADEC)
        list(APPEND XZDEC_TOOLS lzmadec)
    endif()

    foreach(XZDEC ${XZDEC_TOOLS})
        add_executable("${XZDEC}"
            src/common/my_landlock.h
            src/common/sysdefs.h
            src/common/tuklib_common.h
            src/common/tuklib_config.h
            src/common/tuklib_mbstr_nonprint.c
            src/common/tuklib_mbstr_nonprint.h
            src/common/tuklib_exit.c
            src/common/tuklib_exit.h
            src/common/tuklib_gettext.h
            src/common/tuklib_progname.c
            src/common/tuklib_progname.h
            src/xzdec/xzdec.c
        )

        target_include_directories("${XZDEC}" PRIVATE
            src/common
            src/liblzma/api
        )

        target_link_libraries("${XZDEC}" PRIVATE liblzma libgnu)

        if(WIN32 OR CYGWIN)
            # Add the Windows resource file for xzdec.exe or lzmadec.exe.
            target_sources("${XZDEC}" PRIVATE "src/xzdec/${XZDEC}_w32res.rc")
            set_source_files_properties(
                "src/xzdec/${XZDEC}_w32res.rc" PROPERTIES
                OBJECT_DEPENDS "${W32RES_DEPENDENCIES}"
            )
        endif()

        if(SANDBOX_COMPILE_DEFINITION)
            target_compile_definitions("${XZDEC}" PRIVATE
                                       "${SANDBOX_COMPILE_DEFINITION}")
        endif()

        tuklib_progname("${XZDEC}")
        tuklib_mbstr("${XZDEC}")

        install(TARGETS "${XZDEC}"
                RUNTIME DESTINATION "${CMAKE_INSTALL_BINDIR}"
                        COMPONENT "${XZDEC}_Runtime")
    endforeach()

    if(XZ_TOOL_LZMADEC)
        # This is the only build-time difference with lzmadec.
        target_compile_definitions(lzmadec PRIVATE "LZMADEC")
    endif()

    if(UNIX AND XZ_TOOL_XZDEC)
        # NOTE: This puts the lzmadec.1 symlinks into xzdec_Documentation.
        # This isn't great but doing them separately with translated
        # man pages would require extra code. So this has to suffice for now.
        #
        # Also, if xzdec is disabled but lzmadec isn't, then the man page
        # isn't installed at all. It could be done but it's not a typical
        # situation so let's keep this simpler.
        if(XZ_TOOL_LZMADEC)
            my_install_man(xzdec_Documentation src/xzdec/xzdec.1 lzmadec)
        else()
            my_install_man(xzdec_Documentation src/xzdec/xzdec.1 "")
        endif()
    endif()
endif()


#############################################################################
# lzmainfo
#############################################################################

option(XZ_TOOL_LZMAINFO "Build and install the lzmainfo command line tool" ON)

if(XZ_TOOL_LZMAINFO AND HAVE_DECODERS)
    add_executable(lzmainfo
        src/common/sysdefs.h
        src/common/tuklib_common.h
        src/common/tuklib_config.h
        src/common/tuklib_mbstr.h
        src/common/tuklib_mbstr_nonprint.c
        src/common/tuklib_mbstr_nonprint.h
        src/common/tuklib_mbstr_width.c
        src/common/tuklib_mbstr_wrap.c
        src/common/tuklib_mbstr_wrap.h
        src/common/tuklib_exit.c
        src/common/tuklib_exit.h
        src/common/tuklib_gettext.h
        src/common/tuklib_progname.c
        src/common/tuklib_progname.h
        src/lzmainfo/lzmainfo.c
    )

    target_include_directories(lzmainfo PRIVATE
        src/common
        src/liblzma/api
    )

    target_link_libraries(lzmainfo PRIVATE liblzma libgnu)

    if(WIN32 OR CYGWIN)
        # Add the Windows resource file for lzmainfo.exe.
        target_sources(lzmainfo PRIVATE src/lzmainfo/lzmainfo_w32res.rc)
        set_source_files_properties(src/lzmainfo/lzmainfo_w32res.rc PROPERTIES
            OBJECT_DEPENDS "${W32RES_DEPENDENCIES}"
        )
    endif()

    tuklib_progname(lzmainfo)
    tuklib_mbstr(lzmainfo)

    # NOTE: The translations are in the "xz" domain and the .mo files are
    # installed as part of the "xz" target.
    if(XZ_NLS)
        target_link_libraries(lzmainfo PRIVATE Intl::Intl)

        target_compile_definitions(lzmainfo PRIVATE
                ENABLE_NLS
                PACKAGE="${TRANSLATION_DOMAIN}"
                LOCALEDIR="${LOCALEDIR_DEFINITION}"
        )
    endif()

    install(TARGETS lzmainfo
            RUNTIME DESTINATION "${CMAKE_INSTALL_BINDIR}"
                    COMPONENT lzmainfo_Runtime)

    if(UNIX)
        my_install_man(lzmainfo_Documentation src/lzmainfo/lzmainfo.1 "")
    endif()
endif()


#############################################################################
# xz
#############################################################################

option(XZ_TOOL_XZ "Build and install the xz command line tool" ON)

if(XZ_TOOL_XZ)
    add_executable(xz
        src/common/my_landlock.h
        src/common/mythread.h
        src/common/sysdefs.h
        src/common/tuklib_common.h
        src/common/tuklib_config.h
        src/common/tuklib_mbstr_nonprint.c
        src/common/tuklib_mbstr_nonprint.h
        src/common/tuklib_exit.c
        src/common/tuklib_exit.h
        src/common/tuklib_gettext.h
        src/common/tuklib_integer.h
        src/common/tuklib_mbstr.h
        src/common/tuklib_mbstr_fw.c
        src/common/tuklib_mbstr_width.c
        src/common/tuklib_mbstr_wrap.c
        src/common/tuklib_mbstr_wrap.h
        src/common/tuklib_open_stdxxx.c
        src/common/tuklib_open_stdxxx.h
        src/common/tuklib_progname.c
        src/common/tuklib_progname.h
        src/xz/args.c
        src/xz/args.h
        src/xz/coder.c
        src/xz/coder.h
        src/xz/file_io.c
        src/xz/file_io.h
        src/xz/hardware.c
        src/xz/hardware.h
        src/xz/main.c
        src/xz/main.h
        src/xz/message.c
        src/xz/message.h
        src/xz/mytime.c
        src/xz/mytime.h
        src/xz/options.c
        src/xz/options.h
        src/xz/private.h
        src/xz/sandbox.c
        src/xz/sandbox.h
        src/xz/signals.c
        src/xz/signals.h
        src/xz/suffix.c
        src/xz/suffix.h
        src/xz/util.c
        src/xz/util.h
    )

    target_include_directories(xz PRIVATE
        src/common
        src/liblzma/api
    )

    if(HAVE_DECODERS)
        target_sources(xz PRIVATE
            src/xz/list.c
            src/xz/list.h
        )
    endif()

    target_link_libraries(xz PRIVATE liblzma libgnu)

    if(USE_POSIX_THREADS)
        # src/xz/signals.c uses mythread_sigmask() which with POSIX
        # threads calls pthread_sigmask(). Thus, we need the threading
        # library as a dependency for xz. The liblzma target links against
        # Threads::Threads PRIVATEly, thus that won't provide the pthreads
        # symbols for xz.
        #
        # NOTE: The build may work without this if the symbol is in libc
        # but it is mandatory to have this here to keep it working with
        # all pthread implementations.
        target_link_libraries(xz PRIVATE Threads::Threads)
    endif()

    set(XZ_ASSUME_RAM "128" CACHE STRING "Assume that the system has \
this many MiB of RAM if xz cannot determine the amount at runtime")
    target_compile_definitions(xz PRIVATE "ASSUME_RAM=${XZ_ASSUME_RAM}")

    if(WIN32 OR CYGWIN)
        # Add the Windows resource file for xz.exe.
        target_sources(xz PRIVATE src/xz/xz_w32res.rc)
        set_source_files_properties(src/xz/xz_w32res.rc PROPERTIES
            OBJECT_DEPENDS "${W32RES_DEPENDENCIES}"
        )
    endif()

    if(SANDBOX_COMPILE_DEFINITION)
        target_compile_definitions(xz PRIVATE "${SANDBOX_COMPILE_DEFINITION}")
    endif()

    tuklib_progname(xz)
    tuklib_mbstr(xz)

    if(HAVE_GETOPT_LONG)
        check_symbol_exists(optreset getopt.h HAVE_OPTRESET)
        tuklib_add_definition_if(xz HAVE_OPTRESET)
    endif()

    check_symbol_exists(getrlimit sys/resource.h HAVE_GETRLIMIT)
    tuklib_add_definition_if(xz HAVE_GETRLIMIT)

    check_symbol_exists(posix_fadvise fcntl.h HAVE_POSIX_FADVISE)
    tuklib_add_definition_if(xz HAVE_POSIX_FADVISE)

    # How to get file time:
    check_struct_has_member("struct stat" st_atim.tv_nsec
                            "sys/types.h;sys/stat.h"
                            HAVE_STRUCT_STAT_ST_ATIM_TV_NSEC)
    if(HAVE_STRUCT_STAT_ST_ATIM_TV_NSEC)
        tuklib_add_definitions(xz HAVE_STRUCT_STAT_ST_ATIM_TV_NSEC)
    else()
        check_struct_has_member("struct stat" st_atimespec.tv_nsec
                                "sys/types.h;sys/stat.h"
                                HAVE_STRUCT_STAT_ST_ATIMESPEC_TV_NSEC)
        if(HAVE_STRUCT_STAT_ST_ATIMESPEC_TV_NSEC)
            tuklib_add_definitions(xz HAVE_STRUCT_STAT_ST_ATIMESPEC_TV_NSEC)
        else()
            check_struct_has_member("struct stat" st_atimensec
                                    "sys/types.h;sys/stat.h"
                                    HAVE_STRUCT_STAT_ST_ATIMENSEC)
            tuklib_add_definition_if(xz HAVE_STRUCT_STAT_ST_ATIMENSEC)
        endif()
    endif()

    # How to set file time:
    check_symbol_exists(futimens "sys/types.h;sys/stat.h" HAVE_FUTIMENS)
    if(HAVE_FUTIMENS)
        tuklib_add_definitions(xz HAVE_FUTIMENS)
    else()
        check_symbol_exists(futimes "sys/time.h" HAVE_FUTIMES)
        if(HAVE_FUTIMES)
            tuklib_add_definitions(xz HAVE_FUTIMES)
        else()
            check_symbol_exists(futimesat "sys/time.h" HAVE_FUTIMESAT)
            if(HAVE_FUTIMESAT)
                tuklib_add_definitions(xz HAVE_FUTIMESAT)
            else()
                check_symbol_exists(utimes "sys/time.h" HAVE_UTIMES)
                if(HAVE_UTIMES)
                    tuklib_add_definitions(xz HAVE_UTIMES)
                else()
                    check_symbol_exists(_futime "sys/utime.h" HAVE__FUTIME)
                    if(HAVE__FUTIME)
                        tuklib_add_definitions(xz HAVE__FUTIME)
                    else()
                        check_symbol_exists(utime "utime.h" HAVE_UTIME)
                        tuklib_add_definition_if(xz HAVE_UTIME)
                    endif()
                endif()
            endif()
        endif()
    endif()

    if(XZ_NLS)
        target_link_libraries(xz PRIVATE Intl::Intl)

        target_compile_definitions(xz PRIVATE
                ENABLE_NLS
                PACKAGE="${TRANSLATION_DOMAIN}"
                LOCALEDIR="${LOCALEDIR_DEFINITION}"
        )

        file(STRINGS po/LINGUAS LINGUAS)

        # NOTE: gettext_process_po_files' INSTALL_DESTINATION is
        # incompatible with how Autotools requires the .po files to
        # be named. CMake would require each .po file to be named with
        # the translation domain and thus each .po file would need its
        # own language-specific directory (like "po/fi/xz.po"). On top
        # of this, INSTALL_DESTINATION doesn't allow specifying COMPONENT
        # and thus the .mo files go into "Unspecified" component. So we
        # can use gettext_process_po_files to convert the .po files but
        # installation needs to be done with our own code.
        #
        # Also, the .gmo files will go to root of the build directory
        # instead of neatly into a subdirectory. This is hardcoded in
        # CMake's FindGettext.cmake.
        foreach(LANG IN LISTS LINGUAS)
            gettext_process_po_files("${LANG}" ALL
                    PO_FILES "${CMAKE_CURRENT_SOURCE_DIR}/po/${LANG}.po")
        endforeach()

        foreach(LANG IN LISTS LINGUAS)
            install(
                FILES "${CMAKE_CURRENT_BINARY_DIR}/${LANG}.gmo"
                DESTINATION "${CMAKE_INSTALL_LOCALEDIR}/${LANG}/LC_MESSAGES"
                RENAME "${TRANSLATION_DOMAIN}.mo"
                COMPONENT xz_Runtime)
        endforeach()
    endif()

    # This command must be before the symlink creation to keep things working
    # on Cygwin and MSYS2 in all cases.
    #
    #   - Cygwin can encode symlinks in multiple ways. This can be
    #     controlled via the environment variable "CYGWIN". If it contains
    #     "winsymlinks:nativestrict" then symlink creation will fail if
    #     the link target doesn't exist. This mode isn't the default though.
    #     See: https://cygwin.com/faq.html#faq.api.symlinks
    #
    #   - MSYS2 supports the same winsymlinks option in the environment
    #     variable "MSYS" (not "MSYS2). The default in MSYS2 is to make
    #     a copy of the file instead of any kind of symlink. Thus the link
    #     target must exist or the creation of the "symlink" (copy) will fail.
    #
    # Our installation order must be such that when a symbolic link is created
    # its target must already exists. There is no race condition for parallel
    # builds because the generated cmake_install.cmake executes serially.
    install(TARGETS xz
            RUNTIME DESTINATION "${CMAKE_INSTALL_BINDIR}"
                    COMPONENT xz_Runtime)

    if(UNIX)
        option(XZ_TOOL_SYMLINKS "Create unxz and xzcat symlinks" ON)
        option(XZ_TOOL_SYMLINKS_LZMA
               "Create 'lzma' and other symlinks for LZMA Utils compatibility"
               ON)
        set(XZ_LINKS)

        if(XZ_TOOL_SYMLINKS)
            list(APPEND XZ_LINKS "unxz" "xzcat")
        endif()

        if(XZ_TOOL_SYMLINKS_LZMA)
            list(APPEND XZ_LINKS "lzma" "unlzma" "lzcat")
        endif()

        # On Cygwin, don't add the .exe suffix to the symlinks.
        #
        # FIXME? Does this make sense on MSYS & MSYS2 where "ln -s"
        # by default makes copies? Inside MSYS & MSYS2 it is possible
        # to execute files without the .exe suffix but not outside
        # (like in Command Prompt). Omitting the suffix matches
        # what configure.ac has done for many years though.
        my_install_symlinks(xz_Runtime "${CMAKE_INSTALL_BINDIR}"
                            "xz${CMAKE_EXECUTABLE_SUFFIX}" "" "${XZ_LINKS}")

        # Install the man pages and (optionally) their symlinks
        # and translations.
        my_install_man(xz_Documentation src/xz/xz.1 "${XZ_LINKS}")
    endif()
endif()


#############################################################################
# Scripts
#############################################################################

set(ENABLE_SCRIPTS OFF)

if(UNIX)
    # NOTE: These depend on the xz tool and decoder support.
    option(XZ_TOOL_SCRIPTS "Install the scripts \
xzdiff, xzgrep, xzmore, xzless, and their symlinks" ON)

    if(XZ_TOOL_SCRIPTS AND XZ_TOOL_XZ AND HAVE_DECODERS)
        set(ENABLE_SCRIPTS ON)
    endif()

    # NOTE: This isn't as sophisticated as in the Autotools build which
    # uses posix-shell.m4 but hopefully this doesn't need to be either.
    # CMake likely won't be used on as many (old) obscure systems as the
    # Autotools-based builds are.
    if(CMAKE_SYSTEM_NAME STREQUAL "SunOS" AND EXISTS "/usr/xpg4/bin/sh")
        set(POSIX_SHELL_DEFAULT "/usr/xpg4/bin/sh")
    else()
        set(POSIX_SHELL_DEFAULT "/bin/sh")
    endif()

    set(XZ_POSIX_SHELL "${POSIX_SHELL_DEFAULT}" CACHE STRING
        "Shell to use for scripts (xzgrep and others)")

    # Guess the extra path to add from XZ_POSIX_SHELL. Autotools-based build
    # has a separate option --enable-path-for-scripts=PREFIX but this is
    # enough for Solaris.
    set(enable_path_for_scripts)
    get_filename_component(POSIX_SHELL_DIR "${XZ_POSIX_SHELL}" DIRECTORY)

    if(NOT POSIX_SHELL_DIR MATCHES "^/bin$|^/usr/bin$")
        set(enable_path_for_scripts "PATH=${POSIX_SHELL_DIR}:\$PATH")
    endif()

    set(XZDIFF_LINKS xzcmp)
    set(XZGREP_LINKS xzegrep xzfgrep)
    set(XZMORE_LINKS)
    set(XZLESS_LINKS)

    if(XZ_TOOL_SYMLINKS_LZMA)
        list(APPEND XZDIFF_LINKS lzdiff lzcmp)
        list(APPEND XZGREP_LINKS lzgrep lzegrep lzfgrep)
        list(APPEND XZMORE_LINKS lzmore)
        list(APPEND XZLESS_LINKS lzless)
    endif()

    set(xz "xz")
    set(POSIX_SHELL "${XZ_POSIX_SHELL}")

    foreach(S xzdiff xzgrep xzmore xzless)
        configure_file("src/scripts/${S}.in" "${S}"
               @ONLY
               NEWLINE_STYLE LF
               FILE_PERMISSIONS OWNER_READ OWNER_WRITE OWNER_EXECUTE
                                GROUP_READ GROUP_EXECUTE
                                WORLD_READ WORLD_EXECUTE)

        if(ENABLE_SCRIPTS)
            install(PROGRAMS "${CMAKE_CURRENT_BINARY_DIR}/${S}"
                    DESTINATION "${CMAKE_INSTALL_BINDIR}"
                    COMPONENT scripts_Runtime)
        endif()
    endforeach()

    unset(xz)
    unset(POSIX_SHELL)
    unset(enable_path_for_scripts)

    if(ENABLE_SCRIPTS)
        my_install_symlinks(scripts_Runtime "${CMAKE_INSTALL_BINDIR}"
                            xzdiff "" "${XZDIFF_LINKS}")

        my_install_symlinks(scripts_Runtime "${CMAKE_INSTALL_BINDIR}"
                            xzgrep "" "${XZGREP_LINKS}")

        my_install_symlinks(scripts_Runtime "${CMAKE_INSTALL_BINDIR}"
                            xzmore "" "${XZMORE_LINKS}")

        my_install_symlinks(scripts_Runtime "${CMAKE_INSTALL_BINDIR}"
                            xzless "" "${XZLESS_LINKS}")

        my_install_man(scripts_Documentation
                       src/scripts/xzdiff.1 "${XZDIFF_LINKS}")

        my_install_man(scripts_Documentation
                       src/scripts/xzgrep.1 "${XZGREP_LINKS}")

        my_install_man(scripts_Documentation
                       src/scripts/xzmore.1 "${XZMORE_LINKS}")

        my_install_man(scripts_Documentation
                       src/scripts/xzless.1 "${XZLESS_LINKS}")
    endif()
endif()


#############################################################################
# Documentation
#############################################################################

if(UNIX)
    option(XZ_DOXYGEN "Use Doxygen to generate liblzma API docs" OFF)

    if (XZ_DOXYGEN)
        file(MAKE_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/doc")

        add_custom_command(
            VERBATIM
            COMMAND "${CMAKE_CURRENT_SOURCE_DIR}/doxygen/update-doxygen"
            ARGS "api"
                 "${CMAKE_CURRENT_SOURCE_DIR}"
                 "${CMAKE_CURRENT_BINARY_DIR}/doc"
            OUTPUT doc/api/index.html
            DEPENDS "${CMAKE_CURRENT_SOURCE_DIR}/doxygen/update-doxygen"
                    "${CMAKE_CURRENT_SOURCE_DIR}/doxygen/Doxyfile"
                    ${LIBLZMA_API_HEADERS}
        )

        add_custom_target(
            liblzma-doc-api ALL
            DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/doc/api/index.html"
        )

        install(DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/doc/api"
                DESTINATION "${CMAKE_INSTALL_DOCDIR}"
                COMPONENT liblzma_Documentation)
    endif()
endif()

option(XZ_DOC "Install basic documentation, examples, and license files" ON)
if(XZ_DOC)
    install(DIRECTORY doc/examples
            DESTINATION "${CMAKE_INSTALL_DOCDIR}"
            COMPONENT liblzma_Documentation)

    # GPLv2 applies to the scripts. If GNU getopt_long is used then
    # LGPLv2.1 applies to the command line tools but, using the
    # section 3 of LGPLv2.1, GNU getopt_long can be handled as GPLv2 too.
    # Thus GPLv2 should be enough here.
    install(FILES AUTHORS
                  COPYING
                  COPYING.0BSD
                  COPYING.GPLv2
                  NEWS
                  README
                  THANKS
                  doc/faq.txt
                  doc/history.txt
                  doc/lzma-file-format.txt
                  doc/xz-file-format.txt
            DESTINATION "${CMAKE_INSTALL_DOCDIR}"
            COMPONENT Documentation)
endif()


#############################################################################
# Tests
#############################################################################

# Tests are in a separate file so that it's possible to delete the whole
# "tests" directory and still have a working build, just without the tests.
include(tests/tests.cmake OPTIONAL)
