cmake_minimum_required(VERSION 2.8.12.2)

include(configure.cmake)

project(CoreClrPal)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Include directories

include_directories(include)

# Compile options

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
add_definitions(-DBIT64=1)
add_definitions(-D_WIN64=1)

add_compile_options(-fno-builtin)
add_compile_options(-fPIC)

set(SOURCES
  arch/i386/context.cpp
  arch/i386/context2.S
  arch/i386/processor.cpp
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
  misc/dbgmsg.cpp
  misc/environ.cpp
  misc/error.cpp
  misc/fmtmessage.cpp
  misc/interlock.cpp
  misc/miscpalapi.cpp
  misc/dactableaddress.cpp
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
  thread/process.cpp
  thread/thread.cpp
  thread/threadsusp.cpp
  thread/tls.cpp
)

add_library(CoreClrPal
  STATIC
  ${SOURCES}
  ${PLATFORM_SOURCES}
)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
target_link_libraries(CoreClrPal
  pthread
  rt
  dl
  unwind
)
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
  find_library(COREFOUNDATION CoreFoundation)
  find_library(CORESERVICES CoreServices)
  find_library(SECURITY Security)
  find_library(SYSTEM System)
  target_link_libraries(CoreClrPal
    ${COREFOUNDATION}
    ${CORESERVICES}
    ${SECURITY}
    ${SYSTEM}
  )
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

add_subdirectory(examples)
