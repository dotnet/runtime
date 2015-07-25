cmake_minimum_required(VERSION 2.8.12.2)

include(configure.cmake)

project(coreclrpal)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Enable @rpath support for shared libraries.
set(MACOSX_RPATH ON)

if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
    cmake_policy(SET CMP0042 NEW)
endif()

# Include directories

include_directories(include)

if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
  # required for libunwind
  include_directories("/usr/local/include/")
endif()

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
    arch/i386/context.S
    arch/i386/dispatchexceptionwrapper.S
    exception/machexception.cpp
    exception/machmessage.cpp
  )
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
  add_definitions(-D__LINUX__=1)
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

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
  add_definitions(-D_WIN32=1)
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-DBIT64=1)
  add_definitions(-D_WIN64=1)
endif()


add_compile_options(-fno-builtin)
add_compile_options(-fPIC)

if(PAL_CMAKE_PLATFORM_ARCH_AMD64)
  set(ARCH_SOURCES
    arch/i386/context2.S
    arch/i386/processor.cpp
  )
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM)
  set(ARCH_SOURCES
    arch/arm/context2.S
    arch/arm/processor.cpp
  )
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM64)
  set(ARCH_SOURCES
    arch/arm64/context2.S
    arch/arm64/processor.cpp
  )
endif()

set(SOURCES
  cruntime/file.cpp
  cruntime/filecrt.cpp
  cruntime/finite.cpp
  cruntime/lstr.cpp
  cruntime/malloc.cpp
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
  exception/console.cpp
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
  misc/corefx.cpp
  misc/dbgmsg.cpp
  misc/environ.cpp
  misc/error.cpp
  misc/errorstrings.cpp
  misc/fmtmessage.cpp
  misc/interlock.cpp
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

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
  # On ARM linking with libunwind will break C++ exceptions unless we first
  # link with gcc_s, this is a libunwind issue
  if(PAL_CMAKE_PLATFORM_ARCH_ARM)
    target_link_libraries(coreclrpal
      gcc_s
    )
  endif()
    
  target_link_libraries(coreclrpal
    pthread
    rt
    dl
    unwind
  )
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

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

add_subdirectory(examples)

# Install the static PAL library for VS
install (TARGETS coreclrpal DESTINATION lib)
