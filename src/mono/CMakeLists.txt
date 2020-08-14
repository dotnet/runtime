cmake_minimum_required (VERSION 3.14.5)

project (mono)

include(GNUInstallDirs)
include(CheckIncludeFile)
include(CheckFunctionExists)
include(TestBigEndian)
include (FindPkgConfig)

set(CMAKE_MODULE_PATH
  ${CMAKE_MODULE_PATH}
  "${CMAKE_CURRENT_SOURCE_DIR}/cmake"
  )

find_program(CCACHE_PROGRAM ccache)
if(CCACHE_PROGRAM)
  set_property(GLOBAL PROPERTY RULE_LAUNCH_COMPILE "${CCACHE_PROGRAM}")
endif()

function(append value)
  foreach(variable ${ARGN})
    set(${variable} "${${variable}} ${value}" PARENT_SCOPE)
  endforeach(variable)
endfunction()

# User options
include(options)
# Header/function checks
include(configure)

# FIXME:
set(VERSION "")

execute_process(
  COMMAND grep ^MONO_CORLIB_VERSION= ${CMAKE_SOURCE_DIR}/configure.ac
  COMMAND cut -d = -f  2
  OUTPUT_VARIABLE CORLIB_VERSION_OUT
)
if (CORLIB_VERSION_OUT STREQUAL "")
  message(FATAL_ERROR)
endif()
string(STRIP "${CORLIB_VERSION_OUT}" MONO_CORLIB_VERSION_BASE)
set(MONO_CORLIB_VERSION "\"${MONO_CORLIB_VERSION_BASE}\"")

if(EXISTS "${CMAKE_SOURCE_DIR}/mono.proj")
  set(ENABLE_NETCORE 1)
endif()

if (ENABLE_NETCORE)
  set(DISABLE_REMOTING 1)
  set(DISABLE_REFLECTION_EMIT_SAVE 1)
  set(DISABLE_APPDOMAINS 1)
  set(DISABLE_CLEANUP 1)
  set(DISABLE_ASSEMBLY_REMAPPING 1)
  set(DISABLE_SECURITY 1)
  set(DISABLE_MDB 1)
  set(DISABLE_COM 1)
  set(DISABLE_GAC 1)
  set(DISABLE_PERFCOUNTERS 1)
  set(DISABLE_ATTACH 1)
  set(DISABLE_DLLMAP 1)
  set(DISABLE_CONFIG 1)
  set(DISABLE_CFGDIR_CONFIG 1)
else()
  message(FATAL_ERROR "Building without -DENABLE_NETCORE=1 is not supported.")
endif()

#FIXME:
set(VERSION "\"\"")
set(DISABLED_FEATURES "\"\"")

if ((CMAKE_CXX_COMPILER_ID STREQUAL "GNU") OR (CMAKE_CXX_COMPILER_ID STREQUAL "Clang") OR (CMAKE_CXX_COMPILER_ID STREQUAL "AppleClang"))
  set (GCC 1)
endif ()

add_definitions(-g)
add_definitions(-DHAVE_CONFIG_H)
add_definitions(-D_THREAD_SAFE)

if (GCC)
  add_definitions(-fPIC)
  add_definitions(-DMONO_DLL_EXPORT)
  add_definitions(-fvisibility=hidden)
endif()

set(USE_GCC_ATOMIC_OPS 1)
set(HAVE_CLASSIC_WINAPI_SUPPORT 1)
set(ENABLE_ILGEN 1)
set(HAVE_MOVING_COLLECTOR 1)
set(HAVE_CONC_GC_AS_DEFAULT 1)
set(MONO_INSIDE_RUNTIME 1)

######################################
# GCC CHECKS
######################################
if(GCC)
  # We require C99 with some GNU extensions, e.g. `linux` macro
  add_definitions(-std=gnu99)
  # The runtime code does not respect ANSI C strict aliasing rules
  append("-fno-strict-aliasing" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
  # We rely on signed overflow to behave
  append("-fwrapv" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)

  set(WARNINGS "-Wall -Wunused -Wmissing-declarations -Wpointer-arith -Wno-cast-qual -Wwrite-strings -Wno-switch -Wno-switch-enum -Wno-unused-value -Wno-attributes -Qunused-arguments -Wno-unused-function -Wno-tautological-compare -Wno-parentheses-equality -Wno-self-assign -Wno-return-stack-address -Wno-constant-logical-operand -Wno-zero-length-array -Wmissing-prototypes -Wstrict-prototypes -Wnested-externs -Wno-format-zero-length")

  set(WERROR "-Werror=incompatible-pointer-types -Werror=return-type -Werror-implicit-function-declaration")

  append("${WARNINGS} ${WERROR}" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
endif()

######################################
# HOST OS CHECKS
######################################

if(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
  add_definitions(-D_THREAD_SAFE)
  set(HOST_DARWIN 1)
  set(PTHREAD_POINTER_ID 1)
  set(USE_MACH_SEMA 1)
else()
  message(FATAL_ERROR "")
endif()

######################################
# TARGET OS CHECKS
######################################

# FIXME:
set(TARGET_SYSTEM_NAME "${CMAKE_SYSTEM_NAME}")

if(TARGET_SYSTEM_NAME STREQUAL "Darwin")
  set(TARGET_MACH 1)
  set(TARGET_OSX 1)
  set(RID_PREFIX "osx")
  set(CORETARGETS "-p:TargetsUnix=true -p:TargetsOSX=true")
  set(NETCORE_HOST_PLATFORM "macos")
else()
  message(FATAL_ERROR "")
endif()

######################################
# HOST ARCH CHECKS
######################################

if(CMAKE_SYSTEM_PROCESSOR STREQUAL "x86_64")
  set(HOST_AMD64 1)
else()
  message(FATAL_ERROR "")
endif()

######################################
# TARGET ARCH CHECKS
######################################
# FIXME:
set(TARGET_ARCH "${CMAKE_SYSTEM_PROCESSOR}")

if(TARGET_ARCH STREQUAL "x86_64")
  set(TARGET_AMD64 1)
  set(MONO_ARCHITECTURE "\"amd64\"")
  set(RID_SUFFIX "x64")
  set(COREARCH "x64")
else()
  message(FATAL_ERROR "")
endif()

######################################
# ICU CHECKS
######################################
set(ICU_SHIM_PATH "../../../libraries/Native/Unix/System.Globalization.Native")
if(TARGET_OSX)
  # FIXME: Handle errors
  # Defines ICU_INCLUDEDIR/ICU_LIBDIR
  set(ENV{PKG_CONFIG_PATH} "{$PKG_CONFIG_PATH}:/usr/local/lib/pkgconfig:/usr/local/opt/icu4c/lib/pkgconfig")
  pkg_check_modules(ICU icu-uc)
  set(OSX_ICU_LIBRARY_PATH /usr/lib/libicucore.dylib)
  set(ICU_FLAGS "-DTARGET_UNIX -DU_DISABLE_RENAMING -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option -Wno-deprecated-declarations")
  set(HAVE_SYS_ICU 1)
else()
  message(FATAL_ERROR "")
endif()

######################################
# EGLIB CHECKS
######################################

set (GNUC_PRETTY)
set (GNUC_UNUSED)
set (BREAKPOINT "G_STMT_START { raise (SIGTRAP); } G_STMT_END")
if (GCC)
  set (GNUC_UNUSED "__attribute__((__unused__))")
  set (GNUC_NORETURN "__attribute__((__noreturn__))")
  if (CMAKE_SYSTEM_PROCESSOR MATCHES "x86|x86_64")
	set (BREAKPOINT "G_STMT_START { __asm__ (\"int \$03\"); } G_STMT_END")
  endif ()
endif ()

if (IS_BIG_ENDIAN)
  set (ORDER G_BIG_ENDIAN)
else ()
  set (ORDER G_LITTLE_ENDIAN)
endif ()

if (WIN32)
  # FIXME:
  message (FATAL_ERROR "FIXME")
else ()
  set(PATHSEP "/")
  set(SEARCHSEP ":")
  set(OS "UNIX")
  set(PIDTYPE "int")
endif ()

# FIXME:
set(GSIZE_FORMAT "\"lu\"")

set(GSIZE "size_t")
set(GSSIZE "ptrdiff_t")

#
# END OF EGLIB CHECKS
#

TEST_BIG_ENDIAN (IS_BIG_ENDIAN)

# FIXME:
set(TARGET_SIZEOF_VOID_P "${SIZEOF_VOID_P}")
set(SIZEOF_REGISTER "${SIZEOF_VOID_P}")

if (IS_BIG_ENDIAN)
  set(TARGET_BYTE_ORDER G_BIG_ENDIAN)
else()
  set(TARGET_BYTE_ORDER G_LITTLE_ENDIAN)
endif()

set (RID "${RID_PREFIX}-${RID_SUFFIX}")

add_subdirectory (mono)

configure_file (cmake/config.h.in config.h)
configure_file (cmake/eglib-config.h.cmake.in mono/eglib/eglib-config.h)
configure_file (cmake/netcore-config.make.in netcore/config.make)
