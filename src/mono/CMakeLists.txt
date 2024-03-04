cmake_minimum_required(VERSION 3.20)

project(mono)

include(../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/functions.cmake)
include(${CLR_ENG_NATIVE_DIR}/configuretools.cmake)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)

if (MSVC)
  # Also set by configurecompiler.cmake, which isn't used by mono yet
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/source-charset:utf-8>) # Force MSVC to compile source as UTF-8.

  if(EXISTS ${CLR_SOURCELINK_FILE_PATH})
    add_link_options("/sourcelink:${CLR_SOURCELINK_FILE_PATH}")
  endif()

  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4100>) # W4: unreferenced formal parameter
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4702>) # W4: unreachable code
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4706>) # W4: assignment within conditional expression

  # FIXME we should investigate these mismatches at some point
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4245>) # W4: signed/unsigned mismatch
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4389>) # W4: signed/unsigned mismatch
endif(MSVC)

set(CROSS_ROOTFS $ENV{ROOTFS_DIR})
set(CMAKE_C_FLAGS_CHECKED "")
set(CMAKE_CXX_FLAGS_CHECKED "")
set(CMAKE_EXE_LINKER_FLAGS_CHECKED "")
set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "")

if(NOT MONO_LIB_NAME)
  set(MONO_LIB_NAME "monosgen-2.0")
endif()

if(NOT MONO_SHARED_LIB_NAME)
  set(MONO_SHARED_LIB_NAME "${MONO_LIB_NAME}")
endif()

include(GNUInstallDirs)
include(CheckIncludeFile)
include(CheckFunctionExists)
include(TestBigEndian)
include(CheckCCompilerFlag)

set(CMAKE_MODULE_PATH
  ${CMAKE_MODULE_PATH}
  "${CMAKE_CURRENT_SOURCE_DIR}/cmake"
  )
set(CMAKE_INSTALL_MESSAGE LAZY)

if(USE_CCACHE)
  find_program(CCACHE_PROGRAM ccache)
  if(CCACHE_PROGRAM)
    set_property(GLOBAL PROPERTY RULE_LAUNCH_COMPILE "${CCACHE_PROGRAM}")
  endif()
endif()

function(append value)
  foreach(variable ${ARGN})
    set(${variable} "${${variable}} ${value}" PARENT_SCOPE)
  endforeach(variable)
endfunction()

# User options
include(options)

function(process_enable_minimal)
  string(REPLACE "," ";" tmp1 "${ENABLE_MINIMAL}")
  foreach(arg ${tmp1})
    string(TOUPPER "${arg}" var1)
	if("${DISABLE_${var1}}" STREQUAL "")
	  message(FATAL_ERROR "Unknown DISABLE_ option DISABLE_${var1}.")
	endif()
    set(DISABLE_${var1} 1 PARENT_SCOPE)
  endforeach(arg)
endfunction()

if(ENABLE_MINIMAL)
  process_enable_minimal()
endif()

# Dependencies between options
if(ENABLE_INTERP_LIB)
  set(DISABLE_INTERPRETER 1)
endif()
if(NOT DISABLE_EVENTPIPE)
  set(ENABLE_PERFTRACING 1)
endif()
if(ENABLE_LAZY_GC_THREAD_CREATION)
  set(LAZY_GC_THREAD_CREATION 1)
endif()

if((CMAKE_CXX_COMPILER_ID STREQUAL "GNU") OR(CMAKE_CXX_COMPILER_ID STREQUAL "Clang") OR(CMAKE_CXX_COMPILER_ID STREQUAL "AppleClang"))
  set(GCC 1)
endif()

add_definitions(-DHAVE_CONFIG_H)

if(GCC)
  add_definitions(-g)  # TODO: should this really be on by default?
  add_definitions(-fPIC)
  add_definitions(-fvisibility=hidden)
  set(USE_GCC_ATOMIC_OPS 1)
endif()

set(HAVE_MOVING_COLLECTOR 1)
set(HAVE_CONC_GC_AS_DEFAULT 1)
set(MONO_INSIDE_RUNTIME 1)

if(CMAKE_C_BYTE_ORDER STREQUAL "BIG_ENDIAN")
  set(TARGET_BYTE_ORDER G_BIG_ENDIAN)
else()
  set(TARGET_BYTE_ORDER G_LITTLE_ENDIAN)
endif()

######################################
# AOT CROSS COMPILER SUPPORT
######################################

if(NOT AOT_TARGET_TRIPLE STREQUAL "")
  set(MONO_CROSS_COMPILE 1)
  if(NOT AOT_OFFSETS_FILE STREQUAL "")
    set(MONO_OFFSETS_FILE "${AOT_OFFSETS_FILE}")
  endif()
  if(AOT_TARGET_TRIPLE STREQUAL "x86_64-apple-darwin10")
    set(TARGET_SYSTEM_NAME "ios")
    set(TARGET_ARCH "x86_64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "i386-apple-darwin10")
    set(TARGET_SYSTEM_NAME "ios")
    set(TARGET_ARCH "x86")
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-apple-darwin10")
    set(TARGET_SYSTEM_NAME "ios")
    set(TARGET_ARCH "arm64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "arm-apple-darwin10")
    set(TARGET_SYSTEM_NAME "ios")
    set(TARGET_ARCH "arm")
  elseif(AOT_TARGET_TRIPLE STREQUAL "x86_64-apple-maccatalyst")
    set(TARGET_SYSTEM_NAME "darwin")
    set(TARGET_ARCH "x86_64")
    set(TARGET_MACCAT 1)
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-apple-maccatalyst")
    set(TARGET_SYSTEM_NAME "darwin")
    set(TARGET_ARCH "arm64")
    set(TARGET_MACCAT 1)
  elseif(AOT_TARGET_TRIPLE STREQUAL "wasm32-unknown-none")
    set(TARGET_SYSTEM_NAME "emscripten")
    set(TARGET_ARCH "wasm")
  elseif(AOT_TARGET_TRIPLE STREQUAL "wasm32-unknown-wasi")
    set(TARGET_SYSTEM_NAME "wasi")
    set(TARGET_ARCH "wasm")
  elseif(AOT_TARGET_TRIPLE STREQUAL "x86_64-none-linux-android")
    set(TARGET_SYSTEM_NAME "android")
    set(TARGET_ARCH "x86_64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "i686-none-linux-android")
    set(TARGET_SYSTEM_NAME "android")
    set(TARGET_ARCH "x86")
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-v8a-linux-android")
    set(TARGET_SYSTEM_NAME "android")
    set(TARGET_ARCH "arm64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "armv7-none-linux-androideabi")
    set(TARGET_SYSTEM_NAME "android")
    set(TARGET_ARCH "arm")
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-linux-gnu")
    set(TARGET_SYSTEM_NAME "linux")
    set(TARGET_ARCH "arm64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "riscv64-linux-gnu")
    set(TARGET_SYSTEM_NAME "linux")
    set(TARGET_ARCH "riscv64")
  else()
    message(FATAL_ERROR "AOT target '${AOT_TARGET_TRIPLE}' not supported.")
  endif()
endif()

######################################
# HOST OS CHECKS
######################################

message ("CMAKE_SYSTEM_NAME=${CMAKE_SYSTEM_NAME}")
message ("CMAKE_SYSTEM_VARIANT=${CMAKE_SYSTEM_VARIANT}")

set(CLR_CMAKE_HOST_OS ${CMAKE_SYSTEM_NAME})
string(TOLOWER ${CLR_CMAKE_HOST_OS} CLR_CMAKE_HOST_OS)

# TODO: check if we can enable this on more platforms
# set(MONO_KEYWORD_THREAD "__thread")

if(CLR_CMAKE_HOST_OS STREQUAL "darwin")
  add_definitions(-D_THREAD_SAFE)
  set(HOST_DARWIN 1)
  set(HOST_OSX 1)
  set(PTHREAD_POINTER_ID 1)
  set(USE_MACH_SEMA 1)
  if(CMAKE_SYSTEM_VARIANT STREQUAL "maccatalyst")
    set(HOST_MACCAT 1)
  endif()
elseif(CLR_CMAKE_HOST_OS STREQUAL "ios" OR CLR_CMAKE_HOST_OS STREQUAL "tvos")
  # See man cmake-toolchains(7) on which variables
  # control cross-compiling to ios
  add_definitions(-D_THREAD_SAFE)
  set(HOST_DARWIN 1)
  if(CLR_CMAKE_HOST_OS STREQUAL "ios")
    set(HOST_IOS 1)
  elseif(CLR_CMAKE_HOST_OS STREQUAL "tvos")
    set(HOST_TVOS 1)
  endif()
  set(PTHREAD_POINTER_ID 1)
  set(USE_MACH_SEMA 1)
  set(DISABLE_EXECUTABLES 1)
  set(TARGET_APPLE_MOBILE 1)
  add_definitions("-DSMALL_CONFIG")
  add_definitions("-D_XOPEN_SOURCE")
  add_definitions("-DHAVE_LARGE_FILE_SUPPORT=1")
elseif(CLR_CMAKE_HOST_OS STREQUAL "linux")
  set(HOST_LINUX 1)
  add_definitions(-D_GNU_SOURCE -D_REENTRANT)
  add_definitions(-D_THREAD_SAFE)
  set(HAVE_CGROUP_SUPPORT 1)
  # Enable the "full RELRO" options (RELRO & BIND_NOW) at link time
  add_link_options(-Wl,-z,relro)
  add_link_options(-Wl,-z,now)
elseif(CLR_CMAKE_HOST_OS STREQUAL "android")
  set(HOST_LINUX 1)
  add_definitions(-D_GNU_SOURCE -D_REENTRANT)
  add_definitions(-D_THREAD_SAFE)
  add_compile_options(-Wl,-z,now)
  add_compile_options(-Wl,-z,relro)
  add_compile_options(-Wl,-z,noexecstack)
  # The normal check fails because it uses --isystem <ndk root>/sysroot/usr/include
  set(HAVE_USR_INCLUDE_MALLOC_H 1)
  set(HOST_ANDROID 1)
  set(DISABLE_EXECUTABLES 1)
  # Force some defines
  set(HAVE_SCHED_GETAFFINITY 0)
  set(HAVE_SCHED_SETAFFINITY 0)
  # FIXME: Rest of the flags from configure.ac
elseif(CLR_CMAKE_HOST_OS STREQUAL "emscripten")
  set(HOST_BROWSER 1)
  add_definitions(-D_THREAD_SAFE)
  add_compile_options(-Wno-strict-prototypes)
  add_compile_options(-Wno-unused-but-set-variable)
  add_compile_options(-Wno-single-bit-bitfield-constant-conversion)
  set(DISABLE_EXECUTABLES 1)
  # FIXME: Is there a cmake option for this ?
  set(DISABLE_SHARED_LIBS 1)
  # sys/random.h exists, but its not found
  set(HAVE_SYS_RANDOM_H 1)
  set(INTERNAL_ZLIB 1)
elseif(CLR_CMAKE_HOST_OS STREQUAL "wasi")
  set(HOST_WASI 1)
  add_definitions(-D_WASI_EMULATED_PROCESS_CLOCKS -D_WASI_EMULATED_SIGNAL -D_WASI_EMULATED_MMAN -DHOST_WASI)
  add_definitions(-D_THREAD_SAFE)
  add_definitions(-DDISABLE_SOCKET_TRANSPORT)
  add_definitions(-DDISABLE_EGD_SOCKET)
  add_definitions(-DDISABLE_EVENTPIPE)
  add_compile_options(-Wno-strict-prototypes)
  add_compile_options(-Wno-unused-but-set-variable)
  set(ENABLE_PERFTRACING 0)
  set(DISABLE_SHARED_LIBS 1)
  set(INTERNAL_ZLIB 1)
  set(DISABLE_EXECUTABLES 1)
  set(STATIC_COMPONENTS 1)
elseif(CLR_CMAKE_HOST_OS STREQUAL "windows")
  set(HOST_WIN32 1)
  set(EXE_SUFFIX ".exe")
  set(HOST_NO_SYMLINKS 1)
  set(MONO_KEYWORD_THREAD "__declspec (thread)")
  set(MONO_ZERO_LEN_ARRAY 1)
  set(INTERNAL_ZLIB 1)
  set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>") # statically link VC runtime library
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/W4>)     # set warning level 4
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/WX>)     # treat warnings as errors
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4324>) # 'struct_name' : structure was padded due to __declspec(align())
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/EHsc>)   # set exception handling behavior
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/FC>)     # use full pathnames in diagnostics
  add_link_options(/STACK:0x800000)  # set stack size to 8MB (default is 1MB)
  if(CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Oi>) # enable intrinsics
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/GF>) # enable string pooling
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/GL>) # whole program optimization
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zi>) # enable debugging information
    add_link_options(/LTCG)    # link-time code generation
    add_link_options(/DEBUG)   # enable debugging information
    add_link_options(/OPT:REF) # optimize: remove unreferenced functions & data
    add_link_options(/OPT:ICF) # optimize: enable COMDAT folding
    # the combination of /Zi compiler flag and /DEBUG /OPT:REF /OPT:ICF
    # linker flags is needed to create .pdb output on release builds
  endif()
elseif(CLR_CMAKE_HOST_OS STREQUAL "sunos")
  set(HOST_SOLARIS 1)
  add_definitions(-DGC_SOLARIS_THREADS -DGC_SOLARIS_PTHREADS -D_REENTRANT -D_POSIX_PTHREAD_SEMANTICS -DUSE_MMAP -DUSE_MUNMAP -DHOST_SOLARIS -D__EXTENSIONS__ -D_XPG4_2)
elseif(CLR_CMAKE_HOST_OS STREQUAL "freebsd")
  set(HOST_FREEBSD 1)
elseif(CLR_CMAKE_HOST_OS STREQUAL "haiku")
  set(HOST_HAIKU 1)
  add_definitions(-D_GNU_SOURCE -D_BSD_SOURCE -D_REENTRANT)
  add_definitions(-D_THREAD_SAFE)
else()
  message(FATAL_ERROR "Host '${CLR_CMAKE_HOST_OS}' not supported.")
endif()

######################################
# TARGET OS CHECKS
######################################

if(NOT TARGET_SYSTEM_NAME)
  set(TARGET_SYSTEM_NAME "${CLR_CMAKE_HOST_OS}")
endif()

if(TARGET_SYSTEM_NAME STREQUAL "darwin")
  set(TARGET_UNIX 1)
  set(TARGET_MACH 1)
  set(TARGET_OSX 1)
  set(TARGET_DARWIN 1)
  if(CMAKE_SYSTEM_VARIANT STREQUAL "maccatalyst")
    set(TARGET_MACCAT 1)
  endif()
elseif(TARGET_SYSTEM_NAME STREQUAL "ios" OR TARGET_SYSTEM_NAME STREQUAL "tvos")
  set(TARGET_UNIX 1)
  set(TARGET_MACH 1)
  set(TARGET_DARWIN 1)
  set(TARGET_APPLE_MOBILE 1)
  if(TARGET_SYSTEM_NAME STREQUAL "ios")
    set(TARGET_IOS 1)
  elseif(TARGET_SYSTEM_NAME STREQUAL "tvos")
    set(TARGET_TVOS 1)
  endif()
elseif(TARGET_SYSTEM_NAME STREQUAL "linux")
  set(TARGET_UNIX 1)
  set(TARGET_LINUX 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "alpine")
  set(TARGET_UNIX 1)
  set(TARGET_LINUX 1)
  set(TARGET_LINUX_MUSL 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "android")
  set(TARGET_UNIX 1)
  set(TARGET_LINUX_BIONIC 1)
  set(TARGET_ANDROID 1)
  if (CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_options(-O2)
  endif()
elseif(TARGET_SYSTEM_NAME STREQUAL "emscripten")
  set(TARGET_BROWSER 1)
  if (CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_options(-Os)
  endif()
elseif(TARGET_SYSTEM_NAME STREQUAL "wasi")
  set(TARGET_WASI 1)
  set(DISABLE_THREADS 1)
  if (CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_options(-Os)
  endif()
elseif(TARGET_SYSTEM_NAME STREQUAL "windows")
  set(TARGET_WIN32 1)
  set(TARGET_WINDOWS 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "sunos")
  set(TARGET_UNIX 1)
  set(TARGET_SOLARIS 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "freebsd")
  set(TARGET_UNIX 1)
  set(TARGET_FREEBSD 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "haiku")
  set(TARGET_UNIX 1)
  set(TARGET_HAIKU 1)
else()
  message(FATAL_ERROR "Target '${TARGET_SYSTEM_NAME}' not supported.")
endif()

######################################
# HOST ARCH CHECKS
######################################

if(NOT "${CMAKE_OSX_ARCHITECTURES}" STREQUAL "")
  set(CMAKE_SYSTEM_PROCESSOR "${CMAKE_OSX_ARCHITECTURES}")
endif()

if(NOT "${MSVC_C_ARCHITECTURE_ID}" STREQUAL "")
  set(CMAKE_SYSTEM_PROCESSOR "${MSVC_C_ARCHITECTURE_ID}")
endif()

# Unify naming
if(CMAKE_SYSTEM_PROCESSOR STREQUAL "armv7l" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "ARMV7")
  set(CMAKE_SYSTEM_PROCESSOR "arm")
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "i686" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "i386" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "X86")
  set(CMAKE_SYSTEM_PROCESSOR "x86")
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "aarch64" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "ARM64")
  set(CMAKE_SYSTEM_PROCESSOR "arm64")
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "AMD64" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "amd64" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "x64")
  set(CMAKE_SYSTEM_PROCESSOR "x86_64")
endif()

message ("CMAKE_SYSTEM_PROCESSOR=${CMAKE_SYSTEM_PROCESSOR}")

if(CMAKE_SYSTEM_PROCESSOR STREQUAL "x86_64")
  set(HOST_AMD64 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "x86")
  set(HOST_X86 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "arm64")
  set(HOST_ARM64 1)
elseif(CMAKE_SYSTEM_PROCESSOR MATCHES "arm")
  set(HOST_ARM 1)
  # fixme: use separate defines for host/target
  set(NO_UNALIGNED_ACCESS 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "riscv64")
  set(HOST_RISCV 1)
  set(HOST_RISCV64 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "s390x")
  set(HOST_S390X 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "wasm" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "wasm32")
  set(HOST_WASM 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "ppc64le")
  set(HOST_POWERPC 1)
  set(HOST_POWERPC64 1)
else()
  message(FATAL_ERROR "CMAKE_SYSTEM_PROCESSOR='${CMAKE_SYSTEM_PROCESSOR}' not supported.")
endif()

######################################
# TARGET ARCH CHECKS
######################################

if(NOT TARGET_ARCH)
  set(TARGET_ARCH "${CMAKE_SYSTEM_PROCESSOR}")
endif()

# Unify naming
if(TARGET_ARCH STREQUAL "armv7l" OR TARGET_ARCH STREQUAL "ARMV7")
  set(TARGET_ARCH "arm")
elseif(TARGET_ARCH STREQUAL "i686" OR TARGET_ARCH STREQUAL "i386" OR TARGET_ARCH STREQUAL "X86")
  set(TARGET_ARCH "x86")
elseif(TARGET_ARCH STREQUAL "aarch64" OR TARGET_ARCH STREQUAL "ARM64")
  set(TARGET_ARCH "arm64")
elseif(TARGET_ARCH STREQUAL "AMD64" OR TARGET_ARCH STREQUAL "x64")
  set(TARGET_ARCH "x86_64")
endif()

message("TARGET_ARCH=${TARGET_ARCH}")
message("CMAKE_CROSSCOMPILING=${CMAKE_CROSSCOMPILING}")

if(TARGET_ARCH STREQUAL "x86_64")
  set(TARGET_AMD64 1)
  set(MONO_ARCHITECTURE "\"amd64\"")
  set(TARGET_SIZEOF_VOID_P 8)
  set(SIZEOF_REGISTER 8)
elseif(TARGET_ARCH STREQUAL "x86")
  set(TARGET_X86 1)
  set(MONO_ARCHITECTURE "\"x86\"")
  set(TARGET_SIZEOF_VOID_P 4)
  set(SIZEOF_REGISTER 4)
elseif(TARGET_ARCH STREQUAL "arm64")
  set(TARGET_ARM64 1)
  set(MONO_ARCHITECTURE "\"arm64\"")
  set(TARGET_SIZEOF_VOID_P 8)
  set(SIZEOF_REGISTER 8)
  if(TARGET_SYSTEM_NAME STREQUAL "watchos")
    set(TARGET_SIZEOF_VOID_P 4)
    set(MONO_ARCH_ILP32 1)
  endif()
elseif(TARGET_ARCH MATCHES "arm")
  set(TARGET_ARM 1)
  set(MONO_ARCHITECTURE "\"arm\"")
  if(MONO_ARM_FPU STREQUAL "none")
    add_definitions("-DARM_FPU_NONE=1")
  elseif(MONO_ARM_FPU STREQUAL "vfp-hard")
    add_definitions("-DARM_FPU_VFP_HARD=1")
  else()
    add_definitions("-DARM_FPU_VFP=1")
  endif()
  set(TARGET_SIZEOF_VOID_P 4)
  set(SIZEOF_REGISTER 4)
  # fixme: use separate defines for host/target
  set(NO_UNALIGNED_ACCESS 1)
  set(HAVE_ARMV5 1)
  set(HAVE_ARMV6 1)
  #set(HAVE_ARMV7 1) # TODO: figure out if we should set this
elseif(TARGET_ARCH STREQUAL "riscv64")
  set(TARGET_RISCV 1)
  set(TARGET_RISCV64 1)
  set(MONO_ARCHITECTURE "\"riscv64\"")
  set(TARGET_SIZEOF_VOID_P 8)
  set(SIZEOF_REGISTER 8)
elseif(TARGET_ARCH STREQUAL "s390x")
  set(TARGET_S390X 1)
  set(MONO_ARCHITECTURE "\"s390x\"")
  set(TARGET_SIZEOF_VOID_P 8)
  set(SIZEOF_REGISTER 8)
elseif(TARGET_ARCH STREQUAL "wasm" OR TARGET_ARCH STREQUAL "wasm32")
  set(TARGET_WASM 1)
  set(MONO_ARCHITECTURE "\"wasm\"")
  set(TARGET_SIZEOF_VOID_P 4)
  set(SIZEOF_REGISTER 4)
elseif(TARGET_ARCH STREQUAL "ppc64le")
  set(TARGET_POWERPC 1)
  set(TARGET_POWERPC64 1)
  set(MONO_ARCHITECTURE "\"ppc64le\"")
  set(TARGET_SIZEOF_VOID_P 8)
  set(SIZEOF_REGISTER 8)
else()
  message(FATAL_ERROR "TARGET_ARCH='${TARGET_ARCH}' not supported.")
endif()

# arm64 MacCatalyst runtime host or AOT target is more like Apple mobile targets than x64
if ((HOST_MACCAT AND HOST_ARM64) OR (TARGET_MACCAT AND TARGET_ARM64))
  set(TARGET_APPLE_MOBILE 1)
endif()

# Decide if we need zlib, and if so whether we want the system zlib or the in-tree copy.
if(NOT DISABLE_EMBEDDED_PDB OR NOT DISABLE_LOG_PROFILER_GZ)
  if(INTERNAL_ZLIB)
    # defines ZLIB_SOURCES
    include(${CLR_SRC_NATIVE_DIR}/external/zlib.cmake)
  else()
    # if we're not on a platform where we use the in-tree zlib, require system zlib
    include(${CLR_SRC_NATIVE_DIR}/libs/System.IO.Compression.Native/extra_libs.cmake)
    set(Z_LIBS)
    append_extra_compression_libs(Z_LIBS)
  endif()
endif()

######################################
# HEADER/FUNCTION CHECKS
######################################
include(configure)

######################################
# GCC CHECKS
######################################

if(GCC)
  # We require C11 with some GNU extensions, e.g. `linux` macro
  set(CMAKE_C_EXTENSIONS ON)

  # Turn off floating point expression contraction because it is considered a value changing
  # optimization in the IEEE 754 specification and is therefore considered unsafe.
  add_compile_options(-ffp-contract=off)

  # The runtime code does not respect ANSI C strict aliasing rules
  append("-fno-strict-aliasing" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
  # We rely on signed overflow to behave
  append("-fwrapv" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)

  set(WARNINGS "-Wall -Wunused -Wmissing-declarations -Wpointer-arith -Wno-cast-qual -Wwrite-strings -Wno-switch -Wno-switch-enum -Wno-unused-value -Wno-attributes -Wno-format-zero-length -Wno-unused-function")
  set(WARNINGS_C "-Wmissing-prototypes -Wstrict-prototypes -Wnested-externs")

  set(WERROR "-Werror=return-type")
  set(WERROR_C "-Werror=implicit-function-declaration")

  if (CMAKE_C_COMPILER_ID MATCHES "Clang")
    set(WARNINGS "${WARNINGS} -Qunused-arguments -Wno-tautological-compare -Wno-parentheses-equality -Wno-self-assign -Wno-return-stack-address -Wno-constant-logical-operand -Wno-zero-length-array -Wno-asm-operand-widths")
  endif()

  check_c_compiler_flag("-Werror=incompatible-pointer-types" WERROR_INCOMPATIBLE_POINTER_TYPES)
  if(WERROR_INCOMPATIBLE_POINTER_TYPES)
    set(WERROR_C "${WERROR_C} -Werror=incompatible-pointer-types")
  endif()

  # Check for sometimes suppressed warnings
  check_c_compiler_flag(-Wreserved-identifier COMPILER_SUPPORTS_W_RESERVED_IDENTIFIER)
  if(COMPILER_SUPPORTS_W_RESERVED_IDENTIFIER)
    add_compile_definitions(COMPILER_SUPPORTS_W_RESERVED_IDENTIFIER)
  endif()

  if(HOST_WASI)
    # When building under WASI SDK, it's stricter about discarding 'const' qualifiers, causing some existing
    # code (e.g., mono-rand.c:315) to be rejected
    set(WERROR_C "${WERROR_C} -Wno-incompatible-pointer-types-discards-qualifiers")
  endif()

  append("${WARNINGS} ${WARNINGS_C} ${WERROR} ${WERROR_C}" CMAKE_C_FLAGS)
  append("${WARNINGS} ${WERROR}" CMAKE_CXX_FLAGS)

  set(MONO_ZERO_LEN_ARRAY 0)

  if(ENABLE_WERROR)
    append("-Werror" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
  endif()
endif()

######################################
# LLVM CHECKS
######################################
set(LLVM_LIBS)
if(LLVM_PREFIX)
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
    message(FATAL_ERROR "FIXME: ${TARGET_ARCH}")
  endif()

  set(llvm_config_path "${LLVM_PREFIX}/include/llvm/Config/llvm-config.h")

  # llvm-config --mono-api-version
  file(STRINGS ${llvm_config_path} llvm_api_version_line REGEX "MONO_API_VERSION ")
  string(REGEX REPLACE ".*MONO_API_VERSION ([0-9]+)" "\\1" llvm_api_version ${llvm_api_version_line})

  # llvm-config --libs analysis core bitwriter mcjit orcjit
  set(MONO_llvm_core_libs "LLVMOrcJIT" "LLVMPasses" "LLVMCoroutines" "LLVMipo" "LLVMInstrumentation" "LLVMVectorize" "LLVMScalarOpts" "LLVMLinker" "LLVMIRReader" "LLVMAsmParser" "LLVMInstCombine" "LLVMFrontendOpenMP" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMJITLink" "LLVMMCJIT" "LLVMExecutionEngine" "LLVMTarget" "LLVMRuntimeDyld" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMBinaryFormat" "LLVMSupport" "LLVMDemangle")
  if(${llvm_api_version} LESS 1600)
    set(MONO_llvm_core_libs ${MONO_llvm_core_libs} "LLVMObjCARCOpts" "LLVMMCDisassembler" "LLVMOrcTargetProcess" "LLVMOrcShared" "LLVMDebugInfoDWARF")
  else()
    set(MONO_llvm_core_libs ${MONO_llvm_core_libs} "LLVMIRPrinter" "LLVMCodeGen" "LLVMObjCARCOpts" "LLVMMCDisassembler" "LLVMWindowsDriver" "LLVMOption" "LLVMOrcTargetProcess" "LLVMOrcShared"  "LLVMSymbolize" "LLVMDebugInfoPDB" "LLVMDebugInfoMSF" "LLVMDebugInfoDWARF" "LLVMTargetParser")
  endif()

  # llvm-config --libs x86codegen
  set(MONO_llvm_extra_libs_x86codegen "LLVMX86CodeGen" "LLVMCFGuard" "LLVMGlobalISel" "LLVMX86Desc" "LLVMX86Info" "LLVMMCDisassembler" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMBinaryFormat" "LLVMSupport" "LLVMDemangle")
  if(${llvm_api_version} GREATER_EQUAL 1600)
    set(MONO_llvm_extra_libs_x86codegen ${MONO_llvm_extra_libs_x86codegen} "LLVMInstrumentation" "LLVMObjCARCOpts" "LLVMSymbolize" "LLVMDebugInfoPDB" "LLVMIRReader" "LLVMAsmParser" "LLVMTargetParser")
  endif()

  # llvm-config --libs armcodegen
  set(MONO_llvm_extra_libs_armcodegen "LLVMARMCodeGen" "LLVMCFGuard" "LLVMGlobalISel" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMARMDesc" "LLVMMCDisassembler" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBinaryFormat" "LLVMARMUtils" "LLVMARMInfo" "LLVMSupport" "LLVMDemangle")
  if(${llvm_api_version} GREATER_EQUAL 1600)
    set(MONO_llvm_extra_libs_armcodegen ${MONO_llvm_extra_libs_armcodegen} "LLVMipo" "LLVMInstrumentation" "LLVMVectorize" "LLVMLinker" "LLVMFrontendOpenMP" "LLVMObjCARCOpts" "LLVMSymbolize" "LLVMDebugInfoPDB"  "LLVMIRReader" "LLVMAsmParser" "LLVMTargetParser")
  endif()

  # llvm-config --libs aarch64codegen
  set(MONO_llvm_extra_libs_aarch64codegen "LLVMAArch64CodeGen" "LLVMCFGuard" "LLVMGlobalISel" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMAArch64Desc" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBinaryFormat" "LLVMAArch64Utils" "LLVMAArch64Info" "LLVMSupport" "LLVMDemangle")
  if(${llvm_api_version} GREATER_EQUAL 1600)
    set(MONO_llvm_extra_libs_aarch64codegen ${MONO_llvm_extra_libs_aarch64codegen} "LLVMObjCARCOpts" "LLVMSymbolize" "LLVMDebugInfoPDB"  "LLVMIRReader" "LLVMAsmParser" "LLVMTargetParser")
  endif()

  if(HOST_LINUX AND NOT HOST_WASM AND NOT HOST_WASI AND ${llvm_api_version} GREATER_EQUAL 1600)
    set(MONO_stdlib "-stdlib=libc++")
    set(MONO_cxx_lib "-L${LLVM_PREFIX}/lib -lc++")
    set(MONO_cxx_include "-I${LLVM_PREFIX}/include/c++/v1")
  endif()

  if(${llvm_api_version} GREATER_EQUAL 1600)
    if(HOST_WIN32)
      set(MONO_cxx_std_version "/std:c++17")
    else()
      set(MONO_cxx_std_version "-std=c++17")
    endif()
  else()
    if(HOST_WIN32)
      set(MONO_cxx_std_version "/std:c++14")
    else()
      set(MONO_cxx_std_version "-std=c++14")
    endif()
  endif()

  # llvm-config --cflags
  set(llvm_cflags "-I${LLVM_PREFIX}/include -D__STDC_CONSTANT_MACROS -D__STD_FORMAT_MACROS -D__STDC_LIMIT_MACROS")

  if (HOST_BROWSER)
    set(llvm_cxxflags "-I${LLVM_PREFIX}/include ${MONO_cxx_include} ${MONO_cxx_std_version} ${MONO_stdlib} -fno-rtti -D__STDC_CONSTANT_MACROS -D__STD_FORMAT_MACROS -D__STDC_LIMIT_MACROS")
  else()
    set(llvm_cxxflags "-I${LLVM_PREFIX}/include ${MONO_cxx_include} ${MONO_cxx_std_version} ${MONO_stdlib} -fno-exceptions -fno-rtti -D__STDC_CONSTANT_MACROS -D__STD_FORMAT_MACROS -D__STDC_LIMIT_MACROS")
  endif()
  set(llvm_includedir "${LLVM_PREFIX}/include")

  if(HOST_LINUX)
    # llvm-config --system-libs
    set(llvm_system_libs ${MONO_cxx_lib} "-lz" "-lrt" "-ldl" "-lpthread" "-lm")
  elseif(HOST_OSX)
    # llvm-config --system-libs
    set(llvm_system_libs "-lz" "-lm")
  endif()

  # llvm-config --libs analysis core bitwriter mcjit orcjit
  set(llvm_core_libs ${MONO_llvm_core_libs})

  # Check codegen libs and add needed libraries.
  set(llvm_extra ${MONO_llvm_extra_libs_${llvm_codegen_libs}})
  if("${llvm_extra}" STREQUAL "" AND NOT "${TARGET_ARCH}" STREQUAL "wasm")
    message(FATAL_ERROR "FIXME: ${TARGET_ARCH}")
  endif()

  set(llvm_libs ${llvm_core_libs} ${llvm_extra})
  list(TRANSFORM llvm_libs PREPEND "${LLVM_PREFIX}/lib/${CMAKE_STATIC_LIBRARY_PREFIX}")
  list(TRANSFORM llvm_libs APPEND "${CMAKE_STATIC_LIBRARY_SUFFIX}")

  if (${llvm_api_version} LESS 1100)
    message(FATAL_ERROR "LLVM version too old.")
  endif()

  set(ENABLE_LLVM 1)
  set(ENABLE_LLVM_RUNTIME 1)
  set(LLVM_LIBS ${llvm_libs} ${llvm_system_libs})
  set(LLVM_LIBDIR "${LLVM_PREFIX}/lib")
  set(LLVM_INCLUDEDIR "${llvm_includedir}")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${llvm_cflags}")
  if(HOST_WIN32)
    # /EHsc already enabled, prevent LLVM flags to disable it. Corresponds to -fexceptions.
    string(REPLACE "/EHs-c-" "" llvm_cxxflags "${llvm_cxxflags}")
    # /GR- already enabled and inherited from LLVM flags. Corresponds to -fno-rtti.
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${llvm_cxxflags}")
  elseif(HOST_BROWSER)
    # emscripten's handling of the different exception modes is complex, so having multiple flags
    # passed during a single compile is undesirable. we need to set them elsewhere.
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${llvm_cxxflags} -fno-rtti")
  else()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${llvm_cxxflags} -fexceptions -fno-rtti")
  endif()
  add_definitions(-DLLVM_API_VERSION=${llvm_api_version})
endif()

##

if(NOT MONO_CROSS_COMPILE)
    set(STATIC_LIBS_ONLY 1)
    add_subdirectory(${CLR_SRC_NATIVE_DIR}/libs libs-native)
endif(NOT MONO_CROSS_COMPILE)
##

######################################
# GC CHECKS
######################################

if(NOT MONO_GC STREQUAL "sgen" AND NOT MONO_GC STREQUAL "boehm")
  message(FATAL_ERROR "MONO_GC (set to '${MONO_GC}') must be sgen or boehm")
endif()

if (GC_SUSPEND STREQUAL "coop")
  set(ENABLE_COOP_SUSPEND 1)
elseif(GC_SUSPEND STREQUAL "hybrid")
  set(ENABLE_HYBRID_SUSPEND 1)
elseif(GC_SUSPEND STREQUAL "preemptive")
elseif(GC_SUSPEND STREQUAL "default")
  # set some kind of fallback default
  if(TARGET_SYSTEM_NAME STREQUAL "watchos")
    set(ENABLE_COOP_SUSPEND 1)
  elseif(TARGET_SYSTEM_NAME STREQUAL "windows")
    # use preemptive
  elseif(TARGET_SYSTEM_NAME STREQUAL "emscripten")
    # use preemptive
  elseif(TARGET_SYSTEM_NAME STREQUAL "WASI")
    # use preemptive
  else()
    set(ENABLE_HYBRID_SUSPEND 1)
  endif()
else()
  message(FATAL_ERROR "GC_SUSPEND (set to '${GC_SUSPEND}') must be one of coop, hybrid, preemptive or default")
endif()

######################################
# EGLIB CHECKS
######################################

set(EGLIB_GNUC_PRETTY)
set(EGLIB_GNUC_UNUSED)

if(HOST_WIN32)
  set(EGLIB_GNUC_PRETTY "__FUNCTION__")
  set(EGLIB_BREAKPOINT "__debugbreak()")
  set(EGLIB_PATHSEP "\\\\")
  set(EGLIB_SEARCHSEP ";")
  set(EGLIB_OS "WIN32")
  if(HOST_AMD64 OR HOST_ARM64)
    set(EGLIB_GSIZE_FORMAT "\"Iu\"")
  else()
    set(EGLIB_GSIZE_FORMAT "\"u\"")
  endif()
else()
  set(EGLIB_BREAKPOINT "G_STMT_START { raise(SIGTRAP); } G_STMT_END")
  if(GCC)
    set(EGLIB_GNUC_UNUSED "__attribute__((__unused__))")
    if(HOST_AMD64 OR HOST_X86)
      set(EGLIB_BREAKPOINT "G_STMT_START { __asm__(\"int \$03\"); } G_STMT_END")
    endif()
  endif()
  set(EGLIB_PATHSEP "/")
  set(EGLIB_SEARCHSEP ":")
  set(EGLIB_OS "UNIX")
  set(EGLIB_GSIZE_FORMAT "\"zu\"")
endif()

set(EGLIB_GSIZE "size_t")
set(EGLIB_GSSIZE "ptrdiff_t")

#
# END OF EGLIB CHECKS
#

######################################
# CHECKED BUILD CHECKS
######################################
function(process_checked_build)
  string(REPLACE "," ";" tmp1 "${CHECKED_BUILD}")
  foreach(arg ${tmp1})
    string(TOUPPER "${arg}" var1)
    set(ENABLE_CHECKED_BUILD_${var1} 1 PARENT_SCOPE)
  endforeach(arg)
endfunction()

if(CHECKED_BUILD)
  set(ENABLE_CHECKED_BUILD 1)
  process_checked_build()
elseif (CMAKE_BUILD_TYPE STREQUAL "Debug")
  # if no explicit -DCHECKED_BUILD=args option and we're building debug, just do ENABLE_CHECKED_BUILD_PRIVATE_TYPES
  set(ENABLE_CHECKED_BUILD 1)
  set(ENABLE_CHECKED_BUILD_PRIVATE_TYPES 1)
endif()
### End of checked build checks

######################################
# COMPONENT BUILD CHECKS
######################################
if(DISABLE_COMPONENTS AND NOT STATIC_COMPONENTS)
  set(STATIC_COMPONENTS 1)
endif()

if(DISABLE_LINK_STATIC_COMPONENTS AND NOT STATIC_COMPONENTS)
  set(STATIC_COMPONENTS 1)
endif()
### End of component build checks

######################################
# OTHER CHECKS
######################################
if(HOST_WIN32)
  set(NAME_DEV_RANDOM "\"\"")
else()
  # FIXME:
  set(NAME_DEV_RANDOM "\"/dev/random\"")
endif()

if(HOST_LINUX AND HAVE_SYS_MMAN_H AND HAVE_ELF_H AND HAVE_SYS_SYSCALL_H)
  set(ENABLE_JIT_DUMP 1)
endif()

if(ENABLE_SMALL_CONFIG)
  set(MONO_SMALL_CONFIG 1)
endif()

### End of other checks

######################################
# EXTRACT VERSION
######################################
include_directories(${CLR_ARTIFACTS_OBJ_DIR})
if(CMAKE_HOST_SYSTEM_NAME STREQUAL "Windows")
  if(NOT EXISTS "${VERSION_HEADER_PATH}")
    file(WRITE "${VERSION_HEADER_PATH}" "#undef VER_PRODUCTVERSION_STR\n#define VER_PRODUCTVERSION_STR \"42.42.42.42424\"\n")
  endif()
else()
  if(NOT EXISTS "${VERSION_FILE_PATH}")
    file(WRITE "${VERSION_FILE_PATH}" "static char sccsid[] __attribute__((used)) = \"@(#)Version 42.42.42.42424 @Commit: AAA\";\n")
  endif()
endif()
if (NOT EXISTS "${RUNTIME_VERSION_HEADER_PATH}")
  file(WRITE "${RUNTIME_VERSION_HEADER_PATH}"
"#define RuntimeFileMajorVersion 42\n"
"#define RuntimeFileMinorVersion 42\n"
"#define RuntimeFileBuildVersion 42\n"
"#define RuntimeFileRevisionVersion 42424\n"
"#define RuntimeProductMajorVersion 6\n"
"#define RuntimeProductMinorVersion 0\n"
"#define RuntimeProductPatchVersion 0\n"
)
endif()

if(CMAKE_HOST_SYSTEM_NAME STREQUAL "Windows")
  file(STRINGS "${VERSION_HEADER_PATH}" product_version_string_line REGEX "VER_PRODUCTVERSION_STR ")
  string(REGEX REPLACE ".*VER_PRODUCTVERSION_STR *(.*)" "\\1" product_version_string ${product_version_string_line})
else()
  file(STRINGS "${VERSION_FILE_PATH}" product_version_string_line REGEX "sccsid")
  string(REGEX REPLACE ".*Version *(.*)\";" "\"\\1\"" product_version_string ${product_version_string_line})
endif()

file(STRINGS "${RUNTIME_VERSION_HEADER_PATH}" runtime_version_major_line REGEX "RuntimeProductMajorVersion ")
file(STRINGS "${RUNTIME_VERSION_HEADER_PATH}" runtime_version_minor_line REGEX "RuntimeProductMinorVersion ")
file(STRINGS "${RUNTIME_VERSION_HEADER_PATH}" runtime_version_patch_line REGEX "RuntimeProductPatchVersion ")
string(REGEX REPLACE ".*RuntimeProductMajorVersion *([0-9]+)" "\\1" runtime_version_major ${runtime_version_major_line})
string(REGEX REPLACE ".*RuntimeProductMinorVersion *([0-9]+)" "\\1" runtime_version_minor ${runtime_version_minor_line})
string(REGEX REPLACE ".*RuntimeProductPatchVersion *([0-9]+)" "\\1" runtime_version_patch ${runtime_version_patch_line})

set(VERSION "\"${runtime_version_major}.${runtime_version_minor}.${runtime_version_patch}.0\"")
set(FULL_VERSION ${product_version_string})
### End of extract version

######################################
# OS SPECIFIC CHECKS
######################################

if(CLR_CMAKE_TARGET_LINUX_MUSL)
  # Setting RLIMIT_NOFILE breaks debugging of coreclr on musl-libc for some reason
  add_definitions(-DDONT_SET_RLIMIT_NOFILE)
  # On musl-libc, we need to ensure that the reported stack range for the primary thread is
  # larger than the initial committed stack size.
  add_definitions(-DENSURE_PRIMARY_STACK_SIZE)
endif()

if(CLR_CMAKE_HOST_APPLE)
  # TODO: this is already set by configurecompiler.cmake, remove this once mono uses that
  check_c_compiler_flag(-fno-objc-msgsend-selector-stubs COMPILER_SUPPORTS_FNO_OBJC_MSGSEND_SELECTOR_STUBS)
  if(COMPILER_SUPPORTS_FNO_OBJC_MSGSEND_SELECTOR_STUBS)
    set(CLR_CMAKE_COMMON_OBJC_FLAGS "${CLR_CMAKE_COMMON_OBJC_FLAGS} -fno-objc-msgsend-selector-stubs")
  endif()
endif()

### End of OS specific checks

include_directories("${CLR_SRC_NATIVE_DIR}")

if(NOT DISABLE_LIBS)
  set(INSTALL_MONO_API 1)
endif()
add_subdirectory("${CLR_SRC_NATIVE_DIR}/public" public_apis)

add_subdirectory(mono)
if (ENABLE_MSCORDBI AND NOT TARGET_ARCH STREQUAL "arm64"  AND NOT CMAKE_CROSSCOMPILING AND NOT TARGET_IOS AND NOT TARGET_TVOS AND NOT TARGET_ANDROID AND NOT TARGET_BROWSER AND NOT TARGET_WASI AND NOT HOST_MACCAT)
  add_subdirectory(dlls/mscordbi)
endif()

configure_file(cmake/config.h.in config.h)
configure_file(cmake/eglib-config.h.cmake.in mono/eglib/eglib-config.h)
