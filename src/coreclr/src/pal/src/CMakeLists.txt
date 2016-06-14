cmake_minimum_required(VERSION 2.8.12.2)

include_directories(SYSTEM /usr/local/include)

# set kernel version to detect Alpine
EXEC_PROGRAM(uname ARGS -v OUTPUT_VARIABLE CMAKE_SYSTEM_KERNEL_VERSION)
string(FIND "${CMAKE_SYSTEM_KERNEL_VERSION}" "Alpine" PAL_SYSTEM_ALPINE)
if(PAL_SYSTEM_ALPINE EQUAL -1)
    unset(PAL_SYSTEM_ALPINE)
endif()

include(configure.cmake)

project(coreclrpal)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CORECLR_SET_RPATH)
    # Enable @rpath support for shared libraries.
    set(MACOSX_RPATH ON)
endif(CORECLR_SET_RPATH)

if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
    cmake_policy(SET CMP0042 NEW)
endif()

# Include directories

include_directories(include)

# Compile options

if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL AMD64)
  set(PAL_CMAKE_PLATFORM_ARCH_AMD64 1)
  add_definitions(-D_AMD64_)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l)
  set(PAL_CMAKE_PLATFORM_ARCH_ARM 1)
  add_definitions(-D_ARM_)
elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
  set(PAL_CMAKE_PLATFORM_ARCH_ARM64 1)
  add_definitions(-D_ARM64_)
else()
  message(FATAL_ERROR "Only ARM and AMD64 is supported")
endif()

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
  add_definitions(-D_TARGET_MAC64)
  set(PLATFORM_SOURCES
    arch/i386/activationhandlerwrapper.S
    arch/i386/context.S
    arch/i386/dispatchexceptionwrapper.S
    exception/machexception.cpp
    exception/machmessage.cpp
  )
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

add_definitions(-DPLATFORM_UNIX=1)
add_definitions(-DLP64COMPATIBLE=1)
add_definitions(-DFEATURE_PAL=1)
add_definitions(-DCORECLR=1)
add_definitions(-DPIC=1)
add_definitions(-D_FILE_OFFSET_BITS=64)
if(PAL_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-DBIT64=1)
  add_definitions(-D_WIN64=1)
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-DBIT32=1)
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-DBIT64=1)
  add_definitions(-D_WIN64=1)
endif()

# turn off capability to remove unused functions (which was enabled in debug build with sanitizers)
set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG} -Wl,--no-gc-sections")

add_compile_options(-fPIC)

if(PAL_CMAKE_PLATFORM_ARCH_AMD64)
  set(ARCH_SOURCES
    arch/i386/context2.S
    arch/i386/debugbreak.S
    arch/i386/exceptionhelper.S
    arch/i386/processor.cpp
  )
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM)
  set(ARCH_SOURCES
    arch/arm/context2.S
    arch/arm/debugbreak.S
    arch/arm/exceptionhelper.S
    arch/arm/processor.cpp
  )
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM64)
  set(ARCH_SOURCES
    arch/arm64/context2.S
    arch/arm64/debugbreak.S
    arch/arm64/exceptionhelper.S
    arch/arm64/processor.cpp
  )
endif()

if(PAL_CMAKE_PLATFORM_ARCH_ARM)
  set_source_files_properties(exception/seh.cpp PROPERTIES COMPILE_FLAGS -Wno-error=inline-asm)
endif(PAL_CMAKE_PLATFORM_ARCH_ARM)

set(SOURCES
  cruntime/file.cpp
  cruntime/filecrt.cpp
  cruntime/lstr.cpp
  cruntime/malloc.cpp
  cruntime/math.cpp
  cruntime/mbstring.cpp
  cruntime/misc.cpp
  cruntime/misctls.cpp
  cruntime/path.cpp
  cruntime/printf.cpp
  cruntime/printfcpp.cpp
  cruntime/silent_printf.cpp
  cruntime/string.cpp
  cruntime/stringtls.cpp
  cruntime/thread.cpp
  cruntime/wchar.cpp
  cruntime/wchartls.cpp
  debug/debug.cpp
  exception/seh.cpp
  exception/signal.cpp
  file/directory.cpp
  file/disk.cpp
  file/file.cpp
  file/filetime.cpp
  file/find.cpp
  file/path.cpp
  file/shmfilelockmgr.cpp
  handlemgr/handleapi.cpp
  handlemgr/handlemgr.cpp
  init/pal.cpp
  init/sxs.cpp
  loader/module.cpp
  loader/modulename.cpp
  locale/unicode.cpp
  locale/unicode_data.cpp
  locale/utf8.cpp
  map/common.cpp
  map/map.cpp
  map/virtual.cpp
  memory/heap.cpp
  memory/local.cpp
  misc/dbgmsg.cpp
  misc/environ.cpp
  misc/error.cpp
  misc/errorstrings.cpp
  misc/fmtmessage.cpp
  misc/identity.cpp
  misc/miscpalapi.cpp
  misc/msgbox.cpp
  misc/strutil.cpp
  misc/sysinfo.cpp
  misc/time.cpp
  misc/utils.cpp
  misc/version.cpp
  objmgr/palobjbase.cpp
  objmgr/shmobject.cpp
  objmgr/shmobjectmanager.cpp
  safecrt/makepath_s.c
  safecrt/memcpy_s.c
  safecrt/memmove_s.c
  safecrt/mbusafecrt.c
  safecrt/safecrt_input_s.c
  safecrt/safecrt_output_l.c
  safecrt/safecrt_output_s.c
  safecrt/safecrt_winput_s.c
  safecrt/safecrt_woutput_s.c
  safecrt/splitpath_s.c
  safecrt/sprintf.c
  safecrt/sscanf.c
  safecrt/strcat_s.c
  safecrt/strcpy_s.c
  safecrt/strlen_s.c
  safecrt/strncat_s.c
  safecrt/strncpy_s.c
  safecrt/strtok_s.c
  safecrt/swprintf.c
  safecrt/vsprintf.c
  safecrt/vswprint.c
  safecrt/wcscat_s.c
  safecrt/wcscpy_s.c
  safecrt/wcslen_s.c
  safecrt/wcsncat_s.c
  safecrt/wcsncpy_s.c
  safecrt/wcstok_s.c
  safecrt/wmakepath_s.c
  safecrt/wsplitpath_s.c
  safecrt/xtoa_s.c
  safecrt/xtow_s.c
  sharedmemory/sharedmemory.cpp
  shmemory/shmemory.cpp
  sync/cs.cpp
  synchobj/event.cpp
  synchobj/semaphore.cpp
  synchobj/mutex.cpp
  synchmgr/synchcontrollers.cpp
  synchmgr/synchmanager.cpp
  synchmgr/wait.cpp
  thread/context.cpp
  thread/process.cpp
  thread/thread.cpp
  thread/threadsusp.cpp
  thread/tls.cpp
)

add_library(coreclrpal
  STATIC
  ${SOURCES}
  ${ARCH_SOURCES}
  ${PLATFORM_SOURCES}
)

add_library(tracepointprovider
  STATIC
  misc/tracepointprovider.cpp
)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
  find_library(COREFOUNDATION CoreFoundation)
  find_library(CORESERVICES CoreServices)
  find_library(SECURITY Security)
  find_library(SYSTEM System)
  target_link_libraries(coreclrpal
    ${COREFOUNDATION}
    ${CORESERVICES}
    ${SECURITY}
    ${SYSTEM}
  )
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
  find_library(UNWIND unwind)
  find_library(INTL intl)
  target_link_libraries(coreclrpal
    pthread
    rt
    ${UNWIND}
    ${INTL}
  )
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
  if(PAL_CMAKE_PLATFORM_ARCH_ARM)
    find_library(UNWIND_ARCH NAMES unwind-arm)
  endif()

  if(PAL_CMAKE_PLATFORM_ARCH_AMD64)
    find_library(UNWIND_ARCH NAMES unwind-x86_64)
  endif()

  if(PAL_SYSTEM_ALPINE)
    find_library(INTL intl)
  endif()

  find_library(UNWIND NAMES unwind)
  find_library(UNWIND_GENERIC NAMES unwind-generic)

  target_link_libraries(coreclrpal
    gcc_s
    pthread
    rt
    dl
    uuid
  )

  if(UNWIND STREQUAL UNWIND-NOTFOUND)
    message(FATAL_ERROR "Cannot find libunwind. Try installing libunwind8-dev and libunwind8.")
  endif(UNWIND STREQUAL UNWIND-NOTFOUND)

  target_link_libraries(coreclrpal ${UNWIND})

  if(NOT UNWIND_GENERIC STREQUAL UNWIND_GENERIC-NOTFOUND)
    target_link_libraries(coreclrpal ${UNWIND_GENERIC})
  endif(NOT UNWIND_GENERIC STREQUAL UNWIND_GENERIC-NOTFOUND)

  if(NOT UNWIND_ARCH STREQUAL UNWIND_ARCH-NOTFOUND)
    target_link_libraries(coreclrpal ${UNWIND_ARCH})
  endif(NOT UNWIND_ARCH STREQUAL UNWIND_ARCH-NOTFOUND)

  if(NOT INTL STREQUAL INTL-NOTFOUND)
    target_link_libraries(coreclrpal ${INTL})
  endif(NOT INTL STREQUAL INTL-NOTFOUND)

endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL NetBSD)
  add_definitions(-D_KMEMUSER)
  find_library(UNWIND unwind)
  find_library(INTL intl)
  find_library(KVM kvm)
  target_link_libraries(coreclrpal
    pthread
    rt
    ${UNWIND}
    ${INTL}
    ${KVM}
  )
endif(CMAKE_SYSTEM_NAME STREQUAL NetBSD)

add_subdirectory(examples)

if(FEATURE_EVENT_TRACE)
    add_subdirectory($ENV{__IntermediatesDir}/Generated/eventprovider ${CMAKE_CURRENT_BINARY_DIR}/eventprovider)
endif(FEATURE_EVENT_TRACE)

# Install the static PAL library for VS
install (TARGETS coreclrpal DESTINATION lib)
