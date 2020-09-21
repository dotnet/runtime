cmake_minimum_required(VERSION 3.14.5)

project(mono)

include(GNUInstallDirs)
include(CheckIncludeFile)
include(CheckFunctionExists)
include(TestBigEndian)
include(FindPkgConfig)
include(CheckCCompilerFlag)

include("cmake/QuietOSXRanlib.cmake")

set(CMAKE_MODULE_PATH
  ${CMAKE_MODULE_PATH}
  "${CMAKE_CURRENT_SOURCE_DIR}/cmake"
  )
set(CMAKE_INSTALL_MESSAGE LAZY)

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

function(process_enable_minimal)
  string(REPLACE "," ";" tmp1 "${ENABLE_MINIMAL}")
  foreach(arg ${tmp1})
    string(TOUPPER "${arg}" var1)
    set(DISABLE_${var1} 1 PARENT_SCOPE)
  endforeach(arg)
endfunction()

if(ENABLE_MINIMAL)
  process_enable_minimal()
endif()

execute_process(
  COMMAND grep ^MONO_CORLIB_VERSION= ${CMAKE_SOURCE_DIR}/configure.ac
  COMMAND cut -d = -f  2
  OUTPUT_VARIABLE CORLIB_VERSION_OUT
)
if(CORLIB_VERSION_OUT STREQUAL "")
  message(FATAL_ERROR)
endif()
string(STRIP "${CORLIB_VERSION_OUT}" MONO_CORLIB_VERSION_BASE)
set(MONO_CORLIB_VERSION "\"${MONO_CORLIB_VERSION_BASE}\"")

if(EXISTS "${CMAKE_SOURCE_DIR}/mono.proj")
  set(ENABLE_NETCORE 1)
endif()

if(ENABLE_NETCORE)
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
  set(DISABLE_VERIFIER 1)
else()
  message(FATAL_ERROR "Building without -DENABLE_NETCORE=1 is not supported.")
endif()

# Dependencies between options
if(DISABLE_ASSEMBLY_REMAPPING)
  set(DISABLE_DESKTOP_LOADER 1)
endif()
if(ENABLE_INTERP_LIB)
  set(DISABLE_INTERPRETER 1)
endif()
if(NOT DISABLE_INTERPRETER AND NOT DISABLE_JIT)
  set(ENABLE_ILGEN 1)
endif()
if(NOT DISABLE_EVENTPIPE)
  set(ENABLE_PERFTRACING 1)
endif()
if(ENABLE_LAZY_GC_THREAD_CREATION)
  set(LAZY_GC_THREAD_CREATION 1)
endif()

#FIXME:
set(VERSION "\"\"")
set(DISABLED_FEATURES "\"\"")

if((CMAKE_CXX_COMPILER_ID STREQUAL "GNU") OR(CMAKE_CXX_COMPILER_ID STREQUAL "Clang") OR(CMAKE_CXX_COMPILER_ID STREQUAL "AppleClang"))
  set(GCC 1)
endif()

add_definitions(-g)
add_definitions(-DHAVE_CONFIG_H)
add_definitions(-D_THREAD_SAFE)

if(GCC)
  add_definitions(-fPIC)
  add_definitions(-DMONO_DLL_EXPORT)
  add_definitions(-fvisibility=hidden)
endif()

set(USE_GCC_ATOMIC_OPS 1)
set(HAVE_CLASSIC_WINAPI_SUPPORT 1)
set(HAVE_MOVING_COLLECTOR 1)
set(HAVE_CONC_GC_AS_DEFAULT 1)
set(MONO_INSIDE_RUNTIME 1)
# FIXME:
set(NAME_DEV_RANDOM "/dev/random")

######################################
# AOT CROSS COMPILER SUPPORT
######################################

if(NOT AOT_TARGET_TRIPLE STREQUAL "")
  set(MONO_CROSS_COMPILE 1)
  add_definitions(-DNO_GLOBALIZATION_SHIM)
  if(NOT AOT_OFFSETS_FILE STREQUAL "")
    set(MONO_OFFSETS_FILE "${AOT_OFFSETS_FILE}")
  endif()
  if(AOT_TARGET_TRIPLE STREQUAL "x86_64-apple-darwin10")
    set(TARGET_SYSTEM_NAME "iOS")
    set(TARGET_ARCH "x86_64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "i386-apple-darwin10")
    set(TARGET_SYSTEM_NAME "iOS")
    set(TARGET_ARCH "x86")
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-apple-darwin10")
    set(TARGET_SYSTEM_NAME "iOS")
    set(TARGET_ARCH "arm64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "arm-apple-darwin10")
    set(TARGET_SYSTEM_NAME "iOS")
    set(TARGET_ARCH "arm")
  elseif(AOT_TARGET_TRIPLE STREQUAL "wasm32-unknown-none")
    set(TARGET_SYSTEM_NAME "Emscripten")
    set(TARGET_ARCH "wasm")
  else()
    message(FATAL_ERROR "AOT target '${AOT_TARGET_TRIPLE}' not supported.")
  endif()
endif()

######################################
# GCC CHECKS
######################################

if(GCC)
  # We require C99 with some GNU extensions, e.g. `linux` macro
  set (CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=gnu99")
  # The runtime code does not respect ANSI C strict aliasing rules
  append("-fno-strict-aliasing" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
  # We rely on signed overflow to behave
  append("-fwrapv" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)

  set(WARNINGS "-Wall -Wunused -Wmissing-declarations -Wpointer-arith -Wno-cast-qual -Wwrite-strings -Wno-switch -Wno-switch-enum -Wno-unused-value -Wno-attributes -Wmissing-prototypes -Wstrict-prototypes -Wnested-externs -Wno-format-zero-length -Wno-unused-function")

  if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    set(WARNINGS "${WARNINGS} -Qunused-arguments -Wno-tautological-compare -Wno-parentheses-equality -Wno-self-assign -Wno-return-stack-address -Wno-constant-logical-operand -Wno-zero-length-array")
  endif()

  check_c_compiler_flag("-Werror=incompatible-pointer-types" WERROR_INCOMPATIBLE_POINTER_TYPES)
  if(WERROR_INCOMPATIBLE_POINTER_TYPES)
    set(WERROR "${WERROR} -Werror=incompatible-pointer-types")
  endif()
  set(WERROR "${WERROR} -Werror=return-type -Werror-implicit-function-declaration")

  append("${WARNINGS} ${WERROR}" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)

  set(MONO_ZERO_LEN_ARRAY 0)

  if(ENABLE_WERROR)
    append("-Werror" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
  endif()
endif()

######################################
# HOST OS CHECKS
######################################

if(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
  add_definitions(-D_THREAD_SAFE)
  set(HOST_DARWIN 1)
  set(PTHREAD_POINTER_ID 1)
  set(USE_MACH_SEMA 1)
  if (GC_SUSPEND STREQUAL "default")
    set(GC_SUSPEND "hybrid")
  endif()
elseif(CMAKE_SYSTEM_NAME STREQUAL "iOS" OR CMAKE_SYSTEM_NAME STREQUAL "tvOS")
  # See man cmake-toolchains(7) on which variables
  # control cross-compiling to ios
  add_definitions(-DNO_GLOBALIZATION_SHIM)
  add_definitions(-D_THREAD_SAFE)
  set(HOST_DARWIN 1)
  set(HOST_IOS 1)
  set(PTHREAD_POINTER_ID 1)
  set(USE_MACH_SEMA 1)
  set(DISABLE_EXECUTABLES 1)
  set(DISABLE_CRASH_REPORTING 1)
  set(ENABLE_MONOTOUCH 1)
  add_definitions(-DMONOTOUCH=1)
  set(HAVE_SYSTEM 0)
  set(MONO_DL_NEED_USCORE 1)
  if(CMAKE_SYSTEM_NAME STREQUAL "tvOS")
    set(HOST_TVOS 1)
  endif()

  # Force some defines
  set(HAVE_GETPWUID_R 0)
  set(HAVE_SYS_USER_H 0)
  set(HAVE_GETENTROPY 0)
  if(CMAKE_SYSTEM_NAME STREQUAL "tvOS")
    set(HAVE_PTHREAD_KILL 0)
    set(HAVE_KILL 0)
    set(HAVE_SIGACTION 0)
    set(HAVE_FORK 0)
    set(HAVE_EXECV 0)
    set(HAVE_EXECVE 0)
    set(HAVE_EXECVP 0)
    set(HAVE_SIGNAL 0)
  endif()
  add_definitions("-DSMALL_CONFIG")
  add_definitions("-D_XOPEN_SOURCE")
  add_definitions("-DHAVE_LARGE_FILE_SUPPORT=1")
elseif(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  set(HOST_LINUX 1)
  add_definitions(-D_GNU_SOURCE -D_REENTRANT)
elseif(CMAKE_SYSTEM_NAME STREQUAL "Emscripten")
  set(HOST_WASM 1)
  set(TARGET_ARCH "wasm")
  # CMAKE_SYSTEM_PROCESSOR is set to x86
  set(CMAKE_SYSTEM_PROCESSOR "wasm")
  set(DISABLE_EXECUTABLES 1)
  # FIXME: Is there a cmake option for this ?
  set(DISABLE_SHARED_LIBS 1)
else()
  message(FATAL_ERROR "Host '${CMAKE_SYSTEM_NAME}' not supported.")
endif()

######################################
# TARGET OS CHECKS
######################################

if(NOT TARGET_SYSTEM_NAME)
  set(TARGET_SYSTEM_NAME "${CMAKE_SYSTEM_NAME}")
endif()

if(TARGET_SYSTEM_NAME STREQUAL "Darwin")
  set(TARGET_MACH 1)
  set(TARGET_OSX 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "iOS" OR TARGET_SYSTEM_NAME STREQUAL "tvOS")
  set(TARGET_MACH 1)
  set(TARGET_IOS 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "Linux")
elseif(TARGET_SYSTEM_NAME STREQUAL "Emscripten")
  set(TARGET_WASM 1)
  if (CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_options(-Os)
  endif()
else()
  message(FATAL_ERROR "Target '${TARGET_SYSTEM_NAME}' not supported.")
endif()

######################################
# HOST ARCH CHECKS
######################################

if(NOT "${CMAKE_OSX_ARCHITECTURES}" STREQUAL "")
  set(CMAKE_SYSTEM_PROCESSOR "${CMAKE_OSX_ARCHITECTURES}")
  if(CMAKE_SYSTEM_PROCESSOR STREQUAL "i386")
    set(CMAKE_SYSTEM_PROCESSOR "x86")
  endif()
endif()

if(CMAKE_SYSTEM_PROCESSOR STREQUAL "x86_64")
  set(HOST_AMD64 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "x86")
  set(HOST_X86 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "arm64")
  set(HOST_ARM64 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "arm" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "armv7s")
  set(HOST_ARM 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "wasm")
else()
  message(FATAL_ERROR "CMAKE_SYSTEM_PROCESSOR='${CMAKE_SYSTEM_PROCESSOR}' not supported.")
endif()

######################################
# TARGET ARCH CHECKS
######################################

if(NOT TARGET_ARCH)
  set(TARGET_ARCH "${CMAKE_SYSTEM_PROCESSOR}")
endif()

if(TARGET_ARCH STREQUAL "x86_64")
  set(TARGET_AMD64 1)
  set(MONO_ARCHITECTURE "\"amd64\"")
elseif(TARGET_ARCH STREQUAL "x86")
  set(TARGET_X86 1)
  set(MONO_ARCHITECTURE "\"x86\"")
elseif(TARGET_ARCH STREQUAL "arm64")
  set(TARGET_ARM64 1)
  set(MONO_ARCHITECTURE "\"arm64\"")
elseif(TARGET_ARCH STREQUAL "arm" OR TARGET_ARCH STREQUAL "armv7s")
  set(TARGET_ARM 1)
  set(MONO_ARCHITECTURE "\"arm\"")
  if(TARGET_IOS)
    add_definitions("-DARM_FPU_VFP=1")
  endif()
elseif(TARGET_ARCH STREQUAL "wasm")
  set(MONO_ARCHITECTURE "\"wasm\"")
else()
  message(FATAL_ERROR "TARGET_ARCH='${TARGET_ARCH}' not supported.")
endif()

######################################
# LLVM CHECKS
######################################
if(LLVM_PREFIX)
  # FIXME: windows

  set(LLVM_CONFIG ${LLVM_PREFIX}/bin/llvm-config)
  if (NOT EXISTS ${LLVM_CONFIG})
    message(FATAL_ERROR "LLVM executable '${LLVM_CONFIG}' not found.")
  endif()

  if(TARGET_ARCH STREQUAL "x86_64")
    set(llvm_codegen_libs "x86codegen")
  elseif(TARGET_ARCH STREQUAL "x86")
    set(llvm_codegen_libs "x86codegen")
  elseif(TARGET_ARCH STREQUAL "arm64")
    set(llvm_codegen_libs "aarch64codegen")
  elseif(TARGET_ARCH STREQUAL "arm")
    set(llvm_codegen_libs "armcodegen")
  elseif(TARGET_ARCH STREQUAL "wasm")
    set(llvm_codegen_libs "")
  else()
    message(FATAL_ERROR "FIXME")
  endif()

  execute_process(COMMAND ${LLVM_CONFIG} --mono-api-version RESULT_VARIABLE llvm_config_res OUTPUT_VARIABLE llvm_api_version OUTPUT_STRIP_TRAILING_WHITESPACE)
  if (NOT ${llvm_config_res} EQUAL 0)
    message(FATAL_ERROR "'${LLVM_CONFIG} --mono-api-version' failed. Use the llvm fork at https://github.com/dotnet/llvm-project.")
  endif()
  execute_process(COMMAND ${LLVM_CONFIG} --cflags OUTPUT_VARIABLE llvm_cflags OUTPUT_STRIP_TRAILING_WHITESPACE)
  execute_process(COMMAND ${LLVM_CONFIG} --cxxflags OUTPUT_VARIABLE llvm_cxxflags OUTPUT_STRIP_TRAILING_WHITESPACE)
  execute_process(COMMAND ${LLVM_CONFIG} --system-libs OUTPUT_VARIABLE llvm_system_libs OUTPUT_STRIP_TRAILING_WHITESPACE)
  execute_process(COMMAND ${LLVM_CONFIG} --libs analysis core bitwriter orcjit ${llvm_codegen_libs} OUTPUT_VARIABLE llvm_libs OUTPUT_STRIP_TRAILING_WHITESPACE)

  if (${llvm_api_version} LESS 900)
    message(FATAL_ERROR "LLVM version too old.")
  endif()

  set(ENABLE_LLVM 1)
  set(ENABLE_LLVM_RUNTIME 1)
  set(LLVM_LIBS ${llvm_libs} ${llvm_system_libs})
  set(LLVM_LIBDIR "${LLVM_PREFIX}/lib")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${llvm_cflags}")
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${llvm_cxxflags} -fexceptions -fno-rtti")
  add_definitions(-DLLVM_API_VERSION=${llvm_api_version})
endif()

######################################
# ICU CHECKS
######################################
set(ICU_SHIM_PATH "../../../libraries/Native/Unix/System.Globalization.Native")
if(MONO_CROSS_COMPILE)
elseif(TARGET_OSX)
  # FIXME: Handle errors
  # Defines ICU_INCLUDEDIR/ICU_LIBDIR
  set(ENV{PKG_CONFIG_PATH} "{$PKG_CONFIG_PATH}:/usr/local/lib/pkgconfig:/usr/local/opt/icu4c/lib/pkgconfig")
  pkg_check_modules(ICU icu-uc)
  set(OSX_ICU_LIBRARY_PATH /usr/lib/libicucore.dylib)
  set(ICU_FLAGS "-DTARGET_UNIX -DU_DISABLE_RENAMING -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option -Wno-deprecated-declarations")
  set(HAVE_SYS_ICU 1)
elseif(TARGET_WASM)
  #set(ICU_CFLAGS "-DTARGET_UNIX -DU_DISABLE_RENAMING -DHAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS -DHAVE_SET_MAX_VARIABLE")
  set(ICU_FLAGS "-DPALEXPORT=\"\" -DTARGET_UNIX -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option")
  set(HAVE_SYS_ICU 1)
  set(STATIC_ICU 1)
  set(ICU_LIBS "icucore")
elseif(TARGET_IOS)
  # FIXME:
elseif(HOST_LINUX)
  pkg_check_modules(ICU icu-uc)
  set(ICU_FLAGS "-DTARGET_UNIX -DU_DISABLE_RENAMING -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option -Wno-deprecated-declarations")
  set(HAVE_SYS_ICU 1)
else()
  message(FATAL_ERROR "")
endif()

######################################
# GC CHECKS
######################################

if (GC_SUSPEND STREQUAL "coop")
  set(ENABLE_COOP_SUSPEND 1)
elseif(GC_SUSPEND STREQUAL "hybrid")
  set(ENABLE_HYBRID_SUSPEND 1)
endif()

######################################
# EGLIB CHECKS
######################################

set(GNUC_PRETTY)
set(GNUC_UNUSED)
set(BREAKPOINT "G_STMT_START { raise(SIGTRAP); } G_STMT_END")
if(GCC)
  set(GNUC_UNUSED "__attribute__((__unused__))")
  set(GNUC_NORETURN "__attribute__((__noreturn__))")
  if(CMAKE_SYSTEM_PROCESSOR MATCHES "x86|x86_64")
    set(BREAKPOINT "G_STMT_START { __asm__(\"int \$03\"); } G_STMT_END")
  endif()
endif()

if(IS_BIG_ENDIAN)
  set(ORDER G_BIG_ENDIAN)
else()
  set(ORDER G_LITTLE_ENDIAN)
endif()

if(WIN32)
  # FIXME:
  message(FATAL_ERROR "FIXME")
else()
  set(PATHSEP "/")
  set(SEARCHSEP ":")
  set(OS "UNIX")
  set(PIDTYPE "int")
endif()

# FIXME:
set(GSIZE_FORMAT "\"lu\"")

set(GSIZE "size_t")
set(GSSIZE "ptrdiff_t")

#
# END OF EGLIB CHECKS
#

TEST_BIG_ENDIAN(IS_BIG_ENDIAN)

# FIXME:
set(TARGET_SIZEOF_VOID_P "${SIZEOF_VOID_P}")
set(SIZEOF_REGISTER "${SIZEOF_VOID_P}")

if(IS_BIG_ENDIAN)
  set(TARGET_BYTE_ORDER G_BIG_ENDIAN)
else()
  set(TARGET_BYTE_ORDER G_LITTLE_ENDIAN)
endif()

add_subdirectory(mono)

configure_file(cmake/config.h.in config.h)
configure_file(cmake/eglib-config.h.cmake.in mono/eglib/eglib-config.h)
configure_file(cmake/netcore-config.make.in netcore/config.make)
