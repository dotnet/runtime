if(CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_FREEBSD)
  # On OSX and *BSD, we use the libunwind that's part of the OS
  set(CLR_CMAKE_USE_SYSTEM_LIBUNWIND 1)
endif(CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_FREEBSD)

if(NOT DEFINED ENV{ROOTFS_DIR})
  include_directories(SYSTEM /usr/local/include)
elseif (CLR_CMAKE_TARGET_FREEBSD)
  include_directories(SYSTEM $ENV{ROOTFS_DIR}/usr/local/include)
endif()

if(NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
  include_directories(libunwind/include)
  include_directories(libunwind/include/tdep)
  include_directories(${CMAKE_CURRENT_BINARY_DIR}/libunwind/include)
  include_directories(${CMAKE_CURRENT_BINARY_DIR}/libunwind/include/tdep)

  add_subdirectory(libunwind)
elseif(NOT CLR_CMAKE_TARGET_OSX)
  find_unwind_libs(UNWIND_LIBS)
else()
  add_subdirectory(libunwind)
endif(NOT CLR_CMAKE_USE_SYSTEM_LIBUNWIND)

include(configure.cmake)

project(coreclrpal)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CORECLR_SET_RPATH)
    # Enable @rpath support for shared libraries.
    set(MACOSX_RPATH ON)
endif(CORECLR_SET_RPATH)

cmake_policy(SET CMP0042 NEW)

# Include directories

include_directories(include)

# Compile options

if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
  add_definitions(-DFEATURE_USE_SYSTEM_LIBUNWIND)
endif(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)

if(CLR_CMAKE_TARGET_OSX)
  add_definitions(-DTARGET_OSX)
  add_definitions(-DXSTATE_SUPPORTED)
  set(PLATFORM_SOURCES
    arch/amd64/activationhandlerwrapper.S
    arch/amd64/context.S
    arch/amd64/dispatchexceptionwrapper.S
    exception/machexception.cpp
    exception/machmessage.cpp
  )
endif(CLR_CMAKE_TARGET_OSX)

if (FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION)
  add_definitions(-DFEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION)
endif(FEATURE_ENABLE_NO_ADDRESS_SPACE_RANDOMIZATION)
add_definitions(-DLP64COMPATIBLE)
add_definitions(-DCORECLR)
add_definitions(-DPIC)
add_definitions(-D_FILE_OFFSET_BITS=64)
if(CLR_CMAKE_HOST_ARCH_AMD64)
  set(PAL_ARCH_SOURCES_DIR amd64)
elseif(CLR_CMAKE_HOST_ARCH_ARM)
  set(PAL_ARCH_SOURCES_DIR arm)
elseif(CLR_CMAKE_HOST_ARCH_ARM64)
  set(PAL_ARCH_SOURCES_DIR arm64)
elseif(CLR_CMAKE_HOST_ARCH_I386)
  set(PAL_ARCH_SOURCES_DIR i386)
endif()

if(CLR_CMAKE_HOST_ARCH_AMD64 AND CLR_CMAKE_TARGET_LINUX AND NOT CLR_CMAKE_HOST_ALPINE_LINUX)
  # Currently the _xstate is not available on Alpine Linux
  add_definitions(-DXSTATE_SUPPORTED)
endif(CLR_CMAKE_HOST_ARCH_AMD64 AND CLR_CMAKE_TARGET_LINUX AND NOT CLR_CMAKE_HOST_ALPINE_LINUX)

if(CLR_CMAKE_HOST_ALPINE_LINUX)
  # Setting RLIMIT_NOFILE breaks debugging of coreclr on Alpine Linux for some reason
  add_definitions(-DDONT_SET_RLIMIT_NOFILE)
  # On Alpine Linux, we need to ensure that the reported stack range for the primary thread is
  # larger than the initial committed stack size.
  add_definitions(-DENSURE_PRIMARY_STACK_SIZE)
endif(CLR_CMAKE_HOST_ALPINE_LINUX)

# turn off capability to remove unused functions (which was enabled in debug build with sanitizers)
set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG} -Wl,--no-gc-sections")

if (CMAKE_CXX_COMPILER_ID MATCHES "GNU")
  add_compile_options(-Wno-unused-result)
endif(CMAKE_CXX_COMPILER_ID MATCHES "GNU")

set(ARCH_SOURCES
  arch/${PAL_ARCH_SOURCES_DIR}/context2.S
  arch/${PAL_ARCH_SOURCES_DIR}/debugbreak.S
  arch/${PAL_ARCH_SOURCES_DIR}/exceptionhelper.S
  arch/${PAL_ARCH_SOURCES_DIR}/processor.cpp
)

if(NOT CLR_CMAKE_TARGET_OSX)
  list(APPEND PLATFORM_SOURCES
    arch/${PAL_ARCH_SOURCES_DIR}/callsignalhandlerwrapper.S
    arch/${PAL_ARCH_SOURCES_DIR}/signalhandlerhelper.cpp
  )
endif(NOT CLR_CMAKE_TARGET_OSX)

if(CLR_CMAKE_HOST_ARCH_ARM)
  if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    set_source_files_properties(exception/seh.cpp PROPERTIES COMPILE_FLAGS -Wno-error=inline-asm)
  endif()
endif(CLR_CMAKE_HOST_ARCH_ARM)

if (CMAKE_CXX_COMPILER_ID MATCHES "GNU")
  add_compile_options(-Wa,--divide)
endif()

set(SOURCES
  cruntime/file.cpp
  cruntime/filecrt.cpp
  cruntime/malloc.cpp
  cruntime/math.cpp
  cruntime/mbstring.cpp
  cruntime/misc.cpp
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
  misc/perfjitdump.cpp
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
)

if(NOT CLR_CMAKE_TARGET_OSX)
  list(APPEND SOURCES
     exception/remote-unwind.cpp
  )
endif(NOT CLR_CMAKE_TARGET_OSX)

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

# Build separate pal library for DAC (addition to regular pal library)
if(CLR_CMAKE_TARGET_OSX)
  set(LIBUNWIND_DAC_OBJECTS $<TARGET_OBJECTS:libunwind_dac>)

  add_library(coreclrpal_dac STATIC
    exception/remote-unwind.cpp
    ${LIBUNWIND_DAC_OBJECTS}
  )

  target_compile_definitions(coreclrpal_dac PUBLIC 
    "-D_XOPEN_SOURCE"
    "-DUNW_REMOTE_ONLY"
  )

  target_include_directories(coreclrpal_dac PUBLIC 
    ${CMAKE_CURRENT_SOURCE_DIR}/libunwind/include
    ${CMAKE_CURRENT_SOURCE_DIR}/libunwind/include/tdep
    ${CMAKE_CURRENT_BINARY_DIR}
    ${CMAKE_CURRENT_BINARY_DIR}/libunwind/include
    ${CMAKE_CURRENT_BINARY_DIR}/libunwind/include/tdep
  )
endif(CLR_CMAKE_TARGET_OSX)

# There is only one function exported in 'tracepointprovider.cpp' namely 'PAL_InitializeTracing',
# which is guarded with '#if defined(__linux__)'. On macOS, Xcode issues the following warning:
#
# > warning: /Applications/Xcode-9.4.1.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/bin/ranlib:
# > warning for library: libtracepointprovider.a the table of contents is empty (no object file members in the library define global symbols)
#
if(CLR_CMAKE_TARGET_LINUX)
  add_library(tracepointprovider
    OBJECT
    misc/tracepointprovider.cpp
  )
endif(CLR_CMAKE_TARGET_LINUX)

if(CLR_CMAKE_TARGET_OSX)
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
endif(CLR_CMAKE_TARGET_OSX)

if(CLR_CMAKE_TARGET_FREEBSD)
  target_link_libraries(coreclrpal
    pthread
    rt
    ${UNWIND_LIBS}
  )
endif(CLR_CMAKE_TARGET_FREEBSD)

if(CLR_CMAKE_TARGET_LINUX)
  # On Android, we don't need to link with gcc_s, pthread and rt
  if(NOT CLR_CMAKE_TARGET_ANDROID)
    target_link_libraries(coreclrpal
      gcc_s
      pthread
      rt
    )
  else(NOT CLR_CMAKE_TARGET_ANDROID)
    target_link_libraries(coreclrpal
      ${ANDROID_GLOB}
      ${LZMA})
  endif(NOT CLR_CMAKE_TARGET_ANDROID)

  target_link_libraries(coreclrpal
    dl
  )

  if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
    target_link_libraries(coreclrpal ${UNWIND_LIBS})
  endif(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)

endif(CLR_CMAKE_TARGET_LINUX)

if(CLR_CMAKE_TARGET_NETBSD)
  if (CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
    find_library(UNWIND unwind)
  endif()
  add_definitions(-D_KMEMUSER)
  find_library(KVM kvm)
  target_link_libraries(coreclrpal
    pthread
    rt
    ${UNWIND}
    ${KVM}
  )
endif(CLR_CMAKE_TARGET_NETBSD)

if(CLR_CMAKE_TARGET_SUNOS)
  target_link_libraries(coreclrpal
    pthread
    rt
  )
endif(CLR_CMAKE_TARGET_SUNOS)

if(FEATURE_EVENT_TRACE)
  add_subdirectory(eventprovider)
endif(FEATURE_EVENT_TRACE)

# Install the static PAL library for VS
_install (TARGETS coreclrpal DESTINATION lib)
