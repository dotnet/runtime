cmake_minimum_required(VERSION 2.8.12.2)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
  # On OSX and *BSD, we use the libunwind that's part of the OS
  set(CLR_CMAKE_USE_SYSTEM_LIBUNWIND 1)
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(NOT DEFINED ENV{ROOTFS_DIR})
  include_directories(SYSTEM /usr/local/include)
endif()

add_compile_options(-fPIC)

if(NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
  include_directories(libunwind/include)
  include_directories(libunwind/include/tdep)
  include_directories(${CMAKE_CURRENT_BINARY_DIR}/libunwind/include)
  include_directories(${CMAKE_CURRENT_BINARY_DIR}/libunwind/include/tdep)

  add_subdirectory(libunwind)
elseif(NOT CMAKE_SYSTEM_NAME STREQUAL Darwin)
    if(PAL_CMAKE_PLATFORM_ARCH_ARM)
      find_library(UNWIND_ARCH NAMES unwind-arm)
    endif()

    if(PAL_CMAKE_PLATFORM_ARCH_ARM64)
      find_library(UNWIND_ARCH NAMES unwind-aarch64)
    endif()

    if(PAL_CMAKE_PLATFORM_ARCH_AMD64)
      find_library(UNWIND_ARCH NAMES unwind-x86_64)
    endif()

    if(NOT UNWIND_ARCH STREQUAL UNWIND_ARCH-NOTFOUND)
       set(UNWIND_LIBS ${UNWIND_ARCH})
    endif()

    find_library(UNWIND_GENERIC NAMES unwind-generic)

    if(NOT UNWIND_GENERIC STREQUAL UNWIND_GENERIC-NOTFOUND)
      set(UNWIND_LIBS ${UNWIND_LIBS} ${UNWIND_GENERIC})
    endif()

    find_library(UNWIND NAMES unwind)

    if(UNWIND STREQUAL UNWIND-NOTFOUND)
      message(FATAL_ERROR "Cannot find libunwind. Try installing libunwind8-dev or libunwind-devel.")
    endif()

    set(UNWIND_LIBS ${UNWIND_LIBS} ${UNWIND})
endif(NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
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

if(CLR_CROSS_COMPONENTS_BUILD)
    if(CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL amd64)
        if(CLR_CMAKE_TARGET_ARCH STREQUAL "arm" OR CLR_CMAKE_TARGET_ARCH STREQUAL "armel")
            if($ENV{CROSSCOMPILE} STREQUAL "1")
                set(PAL_CMAKE_PLATFORM_ARCH_I386 1)
                add_definitions(-D_X86_)
            else()
                set(PAL_CMAKE_PLATFORM_ARCH_AMD64 1)
                add_definitions(-D_AMD64_)
            endif()
        else()
            set(PAL_CMAKE_PLATFORM_ARCH_AMD64 1)
            add_definitions(-D_AMD64_)
        endif()
    elseif(CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL i686)
        set(PAL_CMAKE_PLATFORM_ARCH_I386 1)
        add_definitions(-D_X86_)
    else()
        message(FATAL_ERROR "Only AMD64, I386 host for cross-architecture component is supported")
    endif()
else()
    if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL AMD64)
        set(PAL_CMAKE_PLATFORM_ARCH_AMD64 1)
        add_definitions(-D_AMD64_)
    elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l)
        set(PAL_CMAKE_PLATFORM_ARCH_ARM 1)
        add_definitions(-D_ARM_)
    elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
        set(PAL_CMAKE_PLATFORM_ARCH_ARM 1)
        add_definitions(-D_ARM_)
    elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
        set(PAL_CMAKE_PLATFORM_ARCH_ARM64 1)
        add_definitions(-D_ARM64_)
    elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
        set(PAL_CMAKE_PLATFORM_ARCH_I386 1)
        add_definitions(-D_X86_)
    else()
        message(FATAL_ERROR "Only ARM, AMD64, ARM64 and I386 is supported")
    endif()
endif()

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
  add_definitions(-D_TARGET_MAC64)
  add_definitions(-DXSTATE_SUPPORTED)
  set(PLATFORM_SOURCES
    arch/amd64/activationhandlerwrapper.S
    arch/amd64/context.S
    arch/amd64/dispatchexceptionwrapper.S
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
  set(PAL_ARCH_SOURCES_DIR amd64)
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-DBIT32=1)
  set(PAL_ARCH_SOURCES_DIR arm)
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-DBIT64=1)
  add_definitions(-D_WIN64=1)
  set(PAL_ARCH_SOURCES_DIR arm64)
elseif(PAL_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-DBIT32=1)
  set(PAL_ARCH_SOURCES_DIR i386)
endif()

if(PAL_CMAKE_PLATFORM_ARCH_AMD64 AND CMAKE_SYSTEM_NAME STREQUAL Linux AND NOT CLR_CMAKE_PLATFORM_ALPINE_LINUX)
  # Currently the _xstate is not available on Alpine Linux
  add_definitions(-DXSTATE_SUPPORTED)
endif(PAL_CMAKE_PLATFORM_ARCH_AMD64 AND CMAKE_SYSTEM_NAME STREQUAL Linux AND NOT CLR_CMAKE_PLATFORM_ALPINE_LINUX)

if(CLR_CMAKE_PLATFORM_ALPINE_LINUX)
  # Setting RLIMIT_NOFILE breaks debugging of coreclr on Alpine Linux for some reason
  add_definitions(-DDONT_SET_RLIMIT_NOFILE)
  # On Alpine Linux, we need to ensure that the reported stack range for the primary thread is
  # larger than the initial committed stack size.
  add_definitions(-DENSURE_PRIMARY_STACK_SIZE)
endif(CLR_CMAKE_PLATFORM_ALPINE_LINUX)

# turn off capability to remove unused functions (which was enabled in debug build with sanitizers)
set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG} -Wl,--no-gc-sections")

set(ARCH_SOURCES
  arch/${PAL_ARCH_SOURCES_DIR}/context2.S
  arch/${PAL_ARCH_SOURCES_DIR}/debugbreak.S
  arch/${PAL_ARCH_SOURCES_DIR}/exceptionhelper.S
  arch/${PAL_ARCH_SOURCES_DIR}/processor.cpp
)

if(NOT CMAKE_SYSTEM_NAME STREQUAL Darwin)
  list(APPEND PLATFORM_SOURCES
    arch/${PAL_ARCH_SOURCES_DIR}/callsignalhandlerwrapper.S
    arch/${PAL_ARCH_SOURCES_DIR}/signalhandlerhelper.cpp
  )
endif(NOT CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(PAL_CMAKE_PLATFORM_ARCH_ARM)
  set_source_files_properties(exception/seh.cpp PROPERTIES COMPILE_FLAGS -Wno-error=inline-asm)
endif(PAL_CMAKE_PLATFORM_ARCH_ARM)

set(SOURCES
  cruntime/file.cpp
  cruntime/filecrt.cpp
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
  exception/remote-unwind.cpp
  file/directory.cpp
  file/file.cpp
  file/filetime.cpp
  file/find.cpp
  file/path.cpp
  handlemgr/handleapi.cpp
  handlemgr/handlemgr.cpp
  init/pal.cpp
  init/sxs.cpp
  loader/module.cpp
  loader/modulename.cpp
  locale/unicode.cpp
  locale/unicodedata.cpp
  locale/utf8.cpp
  map/common.cpp
  map/map.cpp
  map/virtual.cpp
  memory/heap.cpp
  memory/local.cpp
  misc/cgroup.cpp
  misc/dbgmsg.cpp
  misc/environ.cpp
  misc/error.cpp
  misc/errorstrings.cpp
  misc/fmtmessage.cpp
  misc/jitsupport.cpp
  misc/miscpalapi.cpp
  misc/msgbox.cpp
  misc/strutil.cpp
  misc/sysinfo.cpp
  misc/time.cpp
  misc/utils.cpp
  numa/numa.cpp
  objmgr/palobjbase.cpp
  objmgr/shmobject.cpp
  objmgr/shmobjectmanager.cpp
  safecrt/makepath_s.cpp
  safecrt/memcpy_s.cpp
  safecrt/memmove_s.cpp
  safecrt/mbusafecrt.cpp
  safecrt/safecrt_input_s.cpp
  safecrt/safecrt_output_l.cpp
  safecrt/safecrt_output_s.cpp
  safecrt/safecrt_winput_s.cpp
  safecrt/safecrt_woutput_s.cpp
  safecrt/splitpath_s.cpp
  safecrt/sprintf_s.cpp
  safecrt/sscanf_s.cpp
  safecrt/strcat_s.cpp
  safecrt/strcpy_s.cpp
  safecrt/strlen_s.cpp
  safecrt/strncat_s.cpp
  safecrt/strncpy_s.cpp
  safecrt/strtok_s.cpp
  safecrt/swprintf.cpp
  safecrt/vsprintf.cpp
  safecrt/vswprint.cpp
  safecrt/wcscat_s.cpp
  safecrt/wcscpy_s.cpp
  safecrt/wcslen_s.cpp
  safecrt/wcsncat_s.cpp
  safecrt/wcsncpy_s.cpp
  safecrt/wcstok_s.cpp
  safecrt/wmakepath_s.cpp
  safecrt/wsplitpath_s.cpp
  safecrt/xtoa_s.cpp
  safecrt/xtow_s.cpp
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

if(NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
  set(LIBUNWIND_OBJECTS $<TARGET_OBJECTS:libunwind>)
endif(NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)

add_library(coreclrpal
  STATIC
  ${SOURCES}
  ${ARCH_SOURCES}
  ${PLATFORM_SOURCES}
  ${LIBUNWIND_OBJECTS}
)

# There is only one function exported in 'tracepointprovider.cpp' namely 'PAL_InitializeTracing',
# which is guarded with '#if defined(__linux__)'. On macOS, Xcode issues the following warning:
#
# > warning: /Applications/Xcode-9.4.1.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/bin/ranlib:
# > warning for library: libtracepointprovider.a the table of contents is empty (no object file members in the library define global symbols)
#
if(CMAKE_SYSTEM_NAME STREQUAL Linux)
  add_library(tracepointprovider
    STATIC
    misc/tracepointprovider.cpp
  )
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

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
  find_library(INTL intl)
  target_link_libraries(coreclrpal
    pthread
    rt
    ${UNWIND_LIBS}
    ${INTL}
  )
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
  if(CLR_CMAKE_PLATFORM_ALPINE_LINUX OR CLR_CMAKE_PLATFORM_ANDROID)
    find_library(INTL intl)
  endif()

  # On Android, we don't need to link with gcc_s, pthread and rt
  if(NOT CLR_CMAKE_PLATFORM_ANDROID)
    target_link_libraries(coreclrpal
      gcc_s
      pthread
      rt
    )
  endif()

  if(CLR_CMAKE_PLATFORM_ANDROID)
    find_library(LZMA NAMES lzma)

    if(LZMA STREQUAL LZMA-NOTFOUND)
      message(FATAL_ERROR "Cannot find liblzma.")
    endif(LZMA STREQUAL LZMA-NOTFOUND)

    target_link_libraries(coreclrpal
      gnustl_shared
      android-support
      android-glob
      ${LZMA})
  endif()

  if(CLR_MAKE_PLATFORM_ANDROID)
    find_library(ANDROID_SUPPORT NAMES android-support)
    find_library(ANDROID_GLOB NAMES android-glob)
    find_library(INTL NAMES intl)

    if(ANDROID_SUPPORT STREQUAL ANDROID_SUPPORT-NOTFOUND)
      message(FATAL_ERROR "Cannot find android-support.")
    endif()

    if(ANDROID_GLOB STREQUAL ANDROID_GLOB-NOTFOUND)
      message(FATAL_ERROR "Cannot find android-glob.")
    endif()

    if(INTL STREQUAL INTL-NOTFOUND)
      message(FATAL_ERROR "Cannot find libintl.")
    endif()
  endif()

  target_link_libraries(coreclrpal
    dl
  )

  if(NOT INTL STREQUAL INTL-NOTFOUND)
    target_link_libraries(coreclrpal ${INTL})
  endif(NOT INTL STREQUAL INTL-NOTFOUND)

  if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
    target_link_libraries(coreclrpal ${UNWIND_LIBS})
  endif(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)

endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL NetBSD)
  if (CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
    find_library(UNWIND unwind)
  endif()
  add_definitions(-D_KMEMUSER)
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
  add_subdirectory(eventprovider)
endif(FEATURE_EVENT_TRACE)


# Install the static PAL library for VS
_install (TARGETS coreclrpal DESTINATION lib)
