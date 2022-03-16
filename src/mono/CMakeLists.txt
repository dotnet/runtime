cmake_minimum_required(VERSION 3.14.5)

if (CMAKE_VERSION VERSION_GREATER 3.15 OR CMAKE_VERSION VERSION_EQUAL 3.15)
  cmake_policy(SET CMP0091 NEW)
endif()

project(mono)

include(../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/functions.cmake)

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

set(DISABLE_COM 1)
set(DISABLE_PERFCOUNTERS 1)

# Dependencies between options
if(ENABLE_INTERP_LIB)
  set(DISABLE_INTERPRETER 1)
endif()
if(NOT DISABLE_INTERPRETER OR NOT DISABLE_JIT)
  set(ENABLE_ILGEN 1)
endif()
if(NOT DISABLE_EVENTPIPE)
  set(ENABLE_PERFTRACING 1)
endif()
if(ENABLE_LAZY_GC_THREAD_CREATION)
  set(LAZY_GC_THREAD_CREATION 1)
endif()

set(DISABLED_FEATURES "\"\"")

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

set(HAVE_CLASSIC_WINAPI_SUPPORT 1)
set(HAVE_MOVING_COLLECTOR 1)
set(HAVE_CONC_GC_AS_DEFAULT 1)
set(MONO_INSIDE_RUNTIME 1)

TEST_BIG_ENDIAN(IS_BIG_ENDIAN)

if(IS_BIG_ENDIAN)
  set(TARGET_BYTE_ORDER G_BIG_ENDIAN)
else()
  set(TARGET_BYTE_ORDER G_LITTLE_ENDIAN)
endif()

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
  elseif(AOT_TARGET_TRIPLE STREQUAL "x86_64-apple-maccatalyst")
    set(TARGET_SYSTEM_NAME "Darwin")
    set(TARGET_ARCH "x86_64")
    set(TARGET_MACCAT 1)
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-apple-maccatalyst")
    set(TARGET_SYSTEM_NAME "Darwin")
    set(TARGET_ARCH "arm64")
    set(TARGET_MACCAT 1)
  elseif(AOT_TARGET_TRIPLE STREQUAL "wasm32-unknown-none")
    set(TARGET_SYSTEM_NAME "Emscripten")
    set(TARGET_ARCH "wasm")
  elseif(AOT_TARGET_TRIPLE STREQUAL "x86_64-none-linux-android")
    set(TARGET_SYSTEM_NAME "Android")
    set(TARGET_ARCH "x86_64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "i686-none-linux-android")
    set(TARGET_SYSTEM_NAME "Android")
    set(TARGET_ARCH "x86")
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-v8a-linux-android")
    set(TARGET_SYSTEM_NAME "Android")
    set(TARGET_ARCH "arm64")
  elseif(AOT_TARGET_TRIPLE STREQUAL "armv7-none-linux-androideabi")
    set(TARGET_SYSTEM_NAME "Android")
    set(TARGET_ARCH "arm")
  elseif(AOT_TARGET_TRIPLE STREQUAL "aarch64-linux-gnu")
    set(TARGET_SYSTEM_NAME "Linux")
    set(TARGET_ARCH "arm64")
  else()
    message(FATAL_ERROR "AOT target '${AOT_TARGET_TRIPLE}' not supported.")
  endif()
endif()

######################################
# HOST OS CHECKS
######################################

message ("CMAKE_SYSTEM_NAME=${CMAKE_SYSTEM_NAME}")
message ("CMAKE_SYSTEM_VARIANT=${CMAKE_SYSTEM_VARIANT}")

# TODO: check if we can enable this on more platforms
# set(MONO_KEYWORD_THREAD "__thread")

if(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
  add_definitions(-D_THREAD_SAFE)
  set(HOST_DARWIN 1)
  set(HOST_OSX 1)
  set(PTHREAD_POINTER_ID 1)
  set(USE_MACH_SEMA 1)
  if(CMAKE_SYSTEM_VARIANT STREQUAL "MacCatalyst")
    set(HOST_MACCAT 1)
  endif()
elseif(CMAKE_SYSTEM_NAME STREQUAL "iOS" OR CMAKE_SYSTEM_NAME STREQUAL "tvOS")
  # See man cmake-toolchains(7) on which variables
  # control cross-compiling to ios
  add_definitions(-D_THREAD_SAFE)
  set(HOST_DARWIN 1)
  set(HOST_IOS 1)
  if(CMAKE_SYSTEM_NAME STREQUAL "tvOS")
    set(HOST_TVOS 1)
    # with -fembed-bitcode passing -headerpad_max_install_names is not allowed so remove it from the CMake flags
    string(REPLACE "-Wl,-headerpad_max_install_names" "" CMAKE_C_LINK_FLAGS ${CMAKE_C_LINK_FLAGS})
    string(REPLACE "-Wl,-headerpad_max_install_names" "" CMAKE_CXX_LINK_FLAGS ${CMAKE_CXX_LINK_FLAGS})
    string(REPLACE "-Wl,-headerpad_max_install_names" "" CMAKE_SHARED_LIBRARY_CREATE_C_FLAGS ${CMAKE_SHARED_LIBRARY_CREATE_C_FLAGS})
    string(REPLACE "-Wl,-headerpad_max_install_names" "" CMAKE_SHARED_LIBRARY_CREATE_CXX_FLAGS ${CMAKE_SHARED_LIBRARY_CREATE_CXX_FLAGS})
    add_compile_options(-fembed-bitcode)
    add_link_options(-fembed-bitcode)
  endif()
  set(PTHREAD_POINTER_ID 1)
  set(USE_MACH_SEMA 1)
  set(DISABLE_EXECUTABLES 1)
  set(TARGET_APPLE_MOBILE 1)
  add_definitions("-DSMALL_CONFIG")
  add_definitions("-D_XOPEN_SOURCE")
  add_definitions("-DHAVE_LARGE_FILE_SUPPORT=1")
elseif(CMAKE_SYSTEM_NAME STREQUAL "Linux")
  set(HOST_LINUX 1)
  add_definitions(-D_GNU_SOURCE -D_REENTRANT)
  add_definitions(-D_THREAD_SAFE)
  # Enable the "full RELRO" options (RELRO & BIND_NOW) at link time
  add_link_options(-Wl,-z,relro)
  add_link_options(-Wl,-z,now)
elseif(CMAKE_SYSTEM_NAME STREQUAL "Android")
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
elseif(CMAKE_SYSTEM_NAME STREQUAL "Emscripten")
  set(HOST_BROWSER 1)
  add_definitions(-DNO_GLOBALIZATION_SHIM)
  add_definitions(-D_THREAD_SAFE)
  set(DISABLE_EXECUTABLES 1)
  # FIXME: Is there a cmake option for this ?
  set(DISABLE_SHARED_LIBS 1)
  # sys/random.h exists, but its not found
  set(HAVE_SYS_RANDOM_H 1)
  set(INTERNAL_ZLIB 1)
elseif(CMAKE_SYSTEM_NAME STREQUAL "WASI")
  set(HOST_WASI 1)
  add_definitions(-D_WASI_EMULATED_SIGNAL -D_WASI_EMULATED_MMAN)
  add_definitions(-DNO_GLOBALIZATION_SHIM)
  add_definitions(-D_THREAD_SAFE)
  set(DISABLE_SHARED_LIBS 1)
  set(INTERNAL_ZLIB 1)
  set(DISABLE_EXECUTABLES 1)
  set(DISABLE_COMPONENTS 1)

  set(WASI_DRIVER_SOURCES
    wasi/mono-wasi-driver/driver.c
    wasi/mono-wasi-driver/stubs.c
    wasi/mono-wasi-driver/synthetic-pthread.c
  )
  add_library(mono-wasi-driver STATIC ${WASI_DRIVER_SOURCES})
  target_compile_options(mono-wasi-driver PRIVATE -Wno-missing-prototypes -Wno-strict-prototypes)
  install(TARGETS mono-wasi-driver LIBRARY)
elseif(CMAKE_SYSTEM_NAME STREQUAL "Windows")
  set(HOST_WIN32 1)
  set(EXE_SUFFIX ".exe")
  set(HOST_NO_SYMLINKS 1)
  set(MONO_KEYWORD_THREAD "__declspec (thread)")
  set(MONO_ZERO_LEN_ARRAY 1)
  set(INTERNAL_ZLIB 1)
  set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>") # statically link VC runtime library
  add_compile_options(/W3)   # set warning level 3
  add_compile_options(/EHsc) # set exception handling behavior
  add_compile_options(/FC)   # use full pathnames in diagnostics
  add_link_options(/STACK:0x800000)  # set stack size to 8MB (default is 1MB)
  if(CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_options(/Oi)   # enable intrinsics
    add_compile_options(/GF)   # enable string pooling
    add_compile_options(/Zi)   # enable debugging information
    add_compile_options(/GL)   # whole program optimization
    add_link_options(/LTCG)    # link-time code generation
    add_link_options(/DEBUG)   # enable debugging information
    add_link_options(/OPT:REF) # optimize: remove unreferenced functions & data
    add_link_options(/OPT:ICF) # optimize: enable COMDAT folding
    # the combination of /Zi compiler flag and /DEBUG /OPT:REF /OPT:ICF
    # linker flags is needed to create .pdb output on release builds
  endif()
elseif(CMAKE_SYSTEM_NAME STREQUAL "SunOS")
  set(HOST_SOLARIS 1)
  add_definitions(-DGC_SOLARIS_THREADS -DGC_SOLARIS_PTHREADS -D_REENTRANT -D_POSIX_PTHREAD_SEMANTICS -DUSE_MMAP -DUSE_MUNMAP -DHOST_SOLARIS -D__EXTENSIONS__ -D_XPG4_2)
elseif(CMAKE_SYSTEM_NAME STREQUAL "FreeBSD")
  set(HOST_FREEBSD 1)
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
  set(TARGET_DARWIN 1)
  if(CMAKE_SYSTEM_VARIANT STREQUAL "MacCatalyst")
    set(TARGET_MACCAT 1)
  endif()
elseif(TARGET_SYSTEM_NAME STREQUAL "iOS" OR TARGET_SYSTEM_NAME STREQUAL "tvOS")
  set(TARGET_MACH 1)
  set(TARGET_IOS 1)
  set(TARGET_DARWIN 1)
  if(TARGET_SYSTEM_NAME STREQUAL "tvOS")
    set(TARGET_TVOS 1)
  endif()
  set(TARGET_APPLE_MOBILE 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "Linux")
  set(TARGET_LINUX 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "Android")
  set(TARGET_ANDROID 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "Emscripten")
  set(TARGET_BROWSER 1)
  if (CMAKE_BUILD_TYPE STREQUAL "Release")
    add_compile_options(-Os)
  endif()
elseif(TARGET_SYSTEM_NAME STREQUAL "WASI")
  set(TARGET_WASI 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "Windows")
  set(TARGET_WIN32 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "SunOS")
  set(TARGET_SOLARIS 1)
elseif(TARGET_SYSTEM_NAME STREQUAL "FreeBSD")
  set(TARGET_FREEBSD 1)
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

if(HOST_BROWSER)
  # CMAKE_SYSTEM_PROCESSOR is set to x86 by emscripten
  set(CMAKE_SYSTEM_PROCESSOR "wasm")
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
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "s390x")
  set(HOST_S390X 1)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "wasm" OR CMAKE_SYSTEM_PROCESSOR STREQUAL "wasm32")
  set(HOST_WASM 1)
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
  if(TARGET_SYSTEM_NAME STREQUAL "watchOS")
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
  # TODO: not complete
  set(__mono_ppc64__ 1)
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
  if(HOST_WASI)
    # WASI SDK only includes some required definitions if the C version is at least this new
    set (CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=gnu11")
  else()
    # We require C99 with some GNU extensions, e.g. `linux` macro
    set (CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -std=gnu99")
  endif()

  # Turn off floating point expression contraction because it is considered a value changing
  # optimization in the IEEE 754 specification and is therefore considered unsafe.
  add_compile_options(-ffp-contract=off)

  # The runtime code does not respect ANSI C strict aliasing rules
  append("-fno-strict-aliasing" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)
  # We rely on signed overflow to behave
  append("-fwrapv" CMAKE_C_FLAGS CMAKE_CXX_FLAGS)

  set(WARNINGS "-Wall -Wunused -Wmissing-declarations -Wpointer-arith -Wno-cast-qual -Wwrite-strings -Wno-switch -Wno-switch-enum -Wno-unused-value -Wno-attributes -Wno-format-zero-length -Wno-unused-function")
  set(WARNINGS_C "-Wmissing-prototypes -Wstrict-prototypes -Wnested-externs")

  if (CMAKE_C_COMPILER_ID MATCHES "Clang")
    set(WARNINGS "${WARNINGS} -Qunused-arguments -Wno-tautological-compare -Wno-parentheses-equality -Wno-self-assign -Wno-return-stack-address -Wno-constant-logical-operand -Wno-zero-length-array")
  endif()

  set(WERROR "-Werror=return-type")
  set(WERROR_C "-Werror=implicit-function-declaration")

  check_c_compiler_flag("-Werror=incompatible-pointer-types" WERROR_INCOMPATIBLE_POINTER_TYPES)
  if(WERROR_INCOMPATIBLE_POINTER_TYPES)
    set(WERROR_C "${WERROR_C} -Werror=incompatible-pointer-types")
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
# LLVM FLAGS AND LIBRARIES
######################################

# llvm-config --libs analysis core bitwriter mcjit orcjit
set(MONO_llvm_core_libs_1100 "LLVMOrcJIT" "LLVMPasses" "LLVMCoroutines" "LLVMipo" "LLVMInstrumentation" "LLVMVectorize" "LLVMScalarOpts" "LLVMLinker" "LLVMIRReader" "LLVMAsmParser" "LLVMInstCombine" "LLVMFrontendOpenMP" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMOrcError" "LLVMJITLink" "LLVMMCJIT" "LLVMExecutionEngine" "LLVMTarget" "LLVMRuntimeDyld" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMBinaryFormat" "LLVMSupport" "LLVMDemangle")

# llvm-config --libs x86codegen
set(MONO_llvm_extra_libs_x86codegen_1100 "LLVMX86CodeGen" "LLVMCFGuard" "LLVMGlobalISel" "LLVMX86Desc" "LLVMX86Info" "LLVMMCDisassembler" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMBinaryFormat" "LLVMSupport" "LLVMDemangle")

# llvm-config --libs armcodegen
set(MONO_llvm_extra_libs_armcodegen_1100 "LLVMARMCodeGen" "LLVMCFGuard" "LLVMGlobalISel" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMARMDesc" "LLVMMCDisassembler" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBinaryFormat" "LLVMARMUtils" "LLVMARMInfo" "LLVMSupport" "LLVMDemangle")

# llvm-config --libs aarch64codegen
set(MONO_llvm_extra_libs_aarch64codegen_1100 "LLVMAArch64CodeGen" "LLVMCFGuard" "LLVMGlobalISel" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMTextAPI" "LLVMMCParser" "LLVMBitReader" "LLVMCore" "LLVMRemarks" "LLVMBitstreamReader" "LLVMAArch64Desc" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBinaryFormat" "LLVMAArch64Utils" "LLVMAArch64Info" "LLVMSupport" "LLVMDemangle")


# llvm-config --libs analysis core bitwriter mcjit orcjit
set(MONO_llvm_core_libs_900 "LLVMOrcJIT" "LLVMJITLink" "LLVMMCJIT" "LLVMExecutionEngine" "LLVMRuntimeDyld" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMMCParser" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBitReader" "LLVMBitstreamReader" "LLVMCore" "LLVMRemarks" "LLVMBinaryFormat" "LLVMSupport" "LLVMDemangle")

# llvm-config --libs x86codegen
set(MONO_llvm_extra_libs_x86codegen_900 "LLVMX86CodeGen" "LLVMGlobalISel" "LLVMX86Desc" "LLVMX86Utils" "LLVMX86Info" "LLVMMCDisassembler" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMMCParser" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBitReader" "LLVMBitstreamReader" "LLVMCore" "LLVMRemarks" "LLVMBinaryFormat" "LLVMSupport" "LLVMDemangle")

# llvm-config --libs armcodegen
set(MONO_llvm_extra_libs_armcodegen_900 "LLVMARMCodeGen" "LLVMGlobalISel" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMMCParser" "LLVMBitReader" "LLVMBitstreamReader" "LLVMCore" "LLVMRemarks" "LLVMARMDesc" "LLVMMCDisassembler" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBinaryFormat" "LLVMARMUtils" "LLVMARMInfo" "LLVMSupport" "LLVMDemangle")

# llvm-config --libs aarch64codegen
set(MONO_llvm_extra_libs_aarch64codegen_900 "LLVMAArch64CodeGen" "LLVMGlobalISel" "LLVMSelectionDAG" "LLVMAsmPrinter" "LLVMDebugInfoDWARF" "LLVMCodeGen" "LLVMTarget" "LLVMScalarOpts" "LLVMInstCombine" "LLVMAggressiveInstCombine" "LLVMTransformUtils" "LLVMBitWriter" "LLVMAnalysis" "LLVMProfileData" "LLVMObject" "LLVMMCParser" "LLVMBitReader" "LLVMBitstreamReader" "LLVMCore" "LLVMRemarks" "LLVMAArch64Desc" "LLVMMC" "LLVMDebugInfoCodeView" "LLVMDebugInfoMSF" "LLVMBinaryFormat" "LLVMAArch64Utils" "LLVMAArch64Info" "LLVMSupport" "LLVMDemangle")

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

  if(CMAKE_CROSSCOMPILING)
    # Can't run llvm-config, hardcode results
    set(llvm_config_path "${LLVM_PREFIX}/include/llvm/Config/llvm-config.h")

    # llvm-config --mono-api-version
    file(STRINGS ${llvm_config_path} llvm_api_version_line REGEX "MONO_API_VERSION ")
    string(REGEX REPLACE ".*MONO_API_VERSION ([0-9]+)" "\\1" llvm_api_version ${llvm_api_version_line})

    # llvm-config --cflags
    set(llvm_cflags "")
    set(llvm_cxxflags "")
    set(llvm_includedir "${LLVM_PREFIX}/include")

    # llvm-config --system-libs
    set(llvm_system_libs "-lz" "-lrt" "-ldl" "-lpthread" "-lm")

    # llvm-config --libs analysis core bitwriter mcjit orcjit
    set(llvm_core_libs ${MONO_llvm_core_libs_${llvm_api_version}})

    # Check codegen libs and add needed libraries.
    set(llvm_extra ${MONO_llvm_extra_libs_${llvm_codegen_libs}_${llvm_api_version}})
    if("${llvm_extra}" STREQUAL "")
      message(FATAL_ERROR "FIXME: ${TARGET_ARCH}")
    endif()

    set(llvm_libs ${llvm_core_libs} ${llvm_extra})
    list(TRANSFORM llvm_libs PREPEND "${LLVM_PREFIX}/lib/${CMAKE_STATIC_LIBRARY_PREFIX}")
    list(TRANSFORM llvm_libs APPEND "${CMAKE_STATIC_LIBRARY_SUFFIX}")
  else()
    set(LLVM_CONFIG ${LLVM_PREFIX}/bin/llvm-config${EXE_SUFFIX})
    if (NOT EXISTS ${LLVM_CONFIG})
      message(FATAL_ERROR "LLVM executable '${LLVM_CONFIG}' not found.")
    endif()

    execute_process(COMMAND ${LLVM_CONFIG} --mono-api-version RESULT_VARIABLE llvm_config_res OUTPUT_VARIABLE llvm_api_version OUTPUT_STRIP_TRAILING_WHITESPACE)
    if (NOT ${llvm_config_res} EQUAL 0)
      message(FATAL_ERROR "'${LLVM_CONFIG} --mono-api-version' failed. Use the llvm fork at https://github.com/dotnet/llvm-project.")
    endif()
    execute_process(COMMAND ${LLVM_CONFIG} --cflags OUTPUT_VARIABLE llvm_cflags OUTPUT_STRIP_TRAILING_WHITESPACE)
    execute_process(COMMAND ${LLVM_CONFIG} --cxxflags OUTPUT_VARIABLE llvm_cxxflags OUTPUT_STRIP_TRAILING_WHITESPACE)
    execute_process(COMMAND ${LLVM_CONFIG} --includedir OUTPUT_VARIABLE llvm_includedir OUTPUT_STRIP_TRAILING_WHITESPACE)
    execute_process(COMMAND ${LLVM_CONFIG} --system-libs OUTPUT_VARIABLE llvm_system_libs_space_separated OUTPUT_STRIP_TRAILING_WHITESPACE)
    execute_process(COMMAND ${LLVM_CONFIG} --libfiles analysis core bitwriter mcjit orcjit ${llvm_codegen_libs} OUTPUT_VARIABLE llvm_libs_space_separated OUTPUT_STRIP_TRAILING_WHITESPACE)
    separate_arguments(llvm_system_libs NATIVE_COMMAND ${llvm_system_libs_space_separated})
    separate_arguments(llvm_libs NATIVE_COMMAND ${llvm_libs_space_separated})
  endif()

  if (${llvm_api_version} LESS 900)
    message(FATAL_ERROR "LLVM version too old.")
  endif()

  set(ENABLE_LLVM 1)
  set(ENABLE_LLVM_RUNTIME 1)
  set(LLVM_LIBS ${llvm_libs} ${llvm_system_libs})
  set(LLVM_LIBDIR "${LLVM_PREFIX}/lib")
  set(LLVM_INCLUDEDIR "${llvm_includedir}")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${llvm_cflags}")
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${llvm_cxxflags} -fexceptions -fno-rtti")
  add_definitions(-DLLVM_API_VERSION=${llvm_api_version})
endif()

######################################
# ICU CHECKS
######################################
set(ICU_SHIM_PATH "${CLR_SRC_NATIVE_DIR}/libs/System.Globalization.Native")
if(MONO_CROSS_COMPILE)
elseif(HOST_OSX AND NOT HOST_MACCAT)
  include(FindPkgConfig)
  # FIXME: Handle errors
  # Defines ICU_INCLUDEDIR/ICU_LIBDIR
  execute_process(COMMAND  brew --prefix OUTPUT_VARIABLE brew_prefix OUTPUT_STRIP_TRAILING_WHITESPACE)
  set(ENV{PKG_CONFIG_PATH} "{$PKG_CONFIG_PATH}:${brew_prefix}/lib/pkgconfig:${brew_prefix}/opt/icu4c/lib/pkgconfig")
  pkg_check_modules(ICU icu-uc)
  set(OSX_ICU_LIBRARY_PATH /usr/lib/libicucore.dylib)
  set(ICU_FLAGS "-DTARGET_UNIX -DU_DISABLE_RENAMING -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option -Wno-deprecated-declarations")
  set(HAVE_SYS_ICU 1)
elseif(HOST_WASM)
  set(ICU_FLAGS "-DPALEXPORT=\"\" -DU_DISABLE_RENAMING -DHAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS -DHAVE_SET_MAX_VARIABLE -DTARGET_UNIX -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option")
  set(HAVE_SYS_ICU 1)
  set(STATIC_ICU 1)
  set(ICU_LIBS "icucore")
elseif(HOST_IOS OR HOST_MACCAT)
  set(ICU_FLAGS "-DTARGET_UNIX -DU_DISABLE_RENAMING -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option -Wno-deprecated-declarations")
  set(HAVE_SYS_ICU 1)
  set(STATIC_ICU 1)
  set(ICU_LIBS icuuc icui18n icudata)
elseif(HOST_ANDROID)
  set(ICU_FLAGS "-DPALEXPORT=\"\" -DHAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS -DHAVE_SET_MAX_VARIABLE -DTARGET_UNIX -DTARGET_ANDROID -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option")
  set(HAVE_SYS_ICU 1)
elseif(HOST_LINUX)
  include(FindPkgConfig)
  if(CROSS_ROOTFS)
    set(ENV{PKG_CONFIG_ICU_UC_INCLUDEDIR} "${CROSS_ROOTFS}/usr/include")
  endif(CROSS_ROOTFS)
  pkg_check_modules(ICU icu-uc)
  set(ICU_FLAGS "-DTARGET_UNIX -DU_DISABLE_RENAMING -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option -Wno-deprecated-declarations")
  set(HAVE_SYS_ICU 1)
elseif(HOST_WIN32)
  set(ICU_FLAGS "-DTARGET_WINDOWS -DPALEXPORT=EXTERN_C")
  set(HAVE_SYS_ICU 1)
elseif(HOST_SOLARIS)
  set(ICU_FLAGS "-DPALEXPORT=\"\" -DTARGET_UNIX -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option")
  set(HAVE_SYS_ICU 1)
elseif(TARGET_FREEBSD)
  set(ICU_FLAGS "-DTARGET_UNIX -Wno-deprecated-declarations")
  set(HAVE_SYS_ICU 1)
  set(ICU_LIBS icuuc icui18n)
  set(ICU_INCLUDEDIR "${CROSS_ROOTFS}/usr/local/include")
  set(ICU_LIBDIR "${CROSS_ROOTFS}/usr/local/lib")
else()
  message(FATAL_ERROR "Unknown host")
endif()

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
  if(TARGET_SYSTEM_NAME STREQUAL "watchOS")
    set(ENABLE_COOP_SUSPEND 1)
  elseif(TARGET_SYSTEM_NAME STREQUAL "Windows")
    # use preemptive
  elseif(TARGET_SYSTEM_NAME STREQUAL "Emscripten")
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
  set(EGLIB_PIDTYPE "void *")
  if(HOST_AMD64 OR HOST_ARM64)
    set(EGLIB_GSIZE_FORMAT "\"Iu\"")
  else()
    set(EGLIB_GSIZE_FORMAT "\"u\"")
  endif()
else()
  set(EGLIB_BREAKPOINT "G_STMT_START { raise(SIGTRAP); } G_STMT_END")
  if(GCC)
    set(EGLIB_GNUC_UNUSED "__attribute__((__unused__))")
    set(EGLIB_GNUC_NORETURN "__attribute__((__noreturn__))")
    if(HOST_AMD64 OR HOST_X86)
      set(EGLIB_BREAKPOINT "G_STMT_START { __asm__(\"int \$03\"); } G_STMT_END")
    endif()
  endif()
  set(EGLIB_PATHSEP "/")
  set(EGLIB_SEARCHSEP ":")
  set(EGLIB_OS "UNIX")
  set(EGLIB_PIDTYPE "int")
  set(EGLIB_GSIZE_FORMAT "\"zu\"")
endif()

set(EGLIB_GSIZE "size_t")
set(EGLIB_GSSIZE "ptrdiff_t")

#
# END OF EGLIB CHECKS
#

######################################
# DEBUG BUILD CHECKS
######################################
function(process_checked_build)
  string(REPLACE "," ";" tmp1 "${CHECKED_BUILD}")
  foreach(arg ${tmp1})
    string(TOUPPER "${arg}" var1)
    set(ENABLE_CHECKED_BUILD_${var1} 1 PARENT_SCOPE)
  endforeach(arg)
endfunction()

if (CMAKE_BUILD_TYPE STREQUAL "Debug")
  # if no explicit -DCHECKED_BUILD=args option, just do ENABLE_CHECKED_BUILD_PRIVATE_TYPES
  if (CHECKED_BUILD)
    set(ENABLE_CHECKED_BUILD 1)
    process_checked_build()
  else()
    set(ENABLE_CHECKED_BUILD 1)
    set(ENABLE_CHECKED_BUILD_PRIVATE_TYPES 1)
  endif()
endif()
### End of debug build checks

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
if(HOST_IOS OR HOST_ANDROID OR HOST_MACCAT)
  # FIXME: the mobile products use mono_dllmap_insert so allow this
  unset(DISABLE_DLLMAP)
else()
  set(DISABLE_DLLMAP 1)
endif()
if (TARGET_BROWSER)
  # sys/errno.h exists, but just emits a warning and includes errno.h
  unset(HAVE_SYS_ERRNO_H)
endif()
### End of OS specific checks

include_directories("${CLR_SRC_NATIVE_DIR}")

if(NOT DISABLE_LIBS)
  set(INSTALL_MONO_API 1)
endif()
add_subdirectory("${CLR_SRC_NATIVE_DIR}/public" public_apis)

add_subdirectory(mono)
if (ENABLE_MSCORDBI AND NOT TARGET_ARCH STREQUAL "arm64"  AND NOT CMAKE_CROSSCOMPILING AND NOT TARGET_IOS AND NOT TARGET_ANDROID AND NOT TARGET_BROWSER AND NOT HOST_MACCAT)
  add_subdirectory(dlls/mscordbi)
  add_subdirectory(dlls/dbgshim)
endif()

configure_file(cmake/config.h.in config.h)
configure_file(cmake/eglib-config.h.cmake.in mono/eglib/eglib-config.h)
