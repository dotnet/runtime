set(CMAKE_INCLUDE_CURRENT_DIR ON)

set( GC_SOURCES
  gcconfig.cpp
  gccommon.cpp
  gcscan.cpp
  gcsvr.cpp
  gcwks.cpp
  handletable.cpp
  handletablecore.cpp
  handletablescan.cpp
  objecthandle.cpp
  softwarewritewatch.cpp
  gchandletable.cpp
  gceesvr.cpp
  gceewks.cpp
  handletablecache.cpp)

if(CLR_CMAKE_PLATFORM_UNIX)
  include(unix/configure.cmake)
  set ( GC_SOURCES
    ${GC_SOURCES}
    unix/gcenv.unix.cpp
    unix/events.cpp
    unix/cgroup.cpp)
else()
  set ( GC_SOURCES
    ${GC_SOURCES}
    windows/gcenv.windows.cpp)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(WIN32)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib)
else()
  set (GC_LINK_LIBRARIES)
endif(WIN32)

convert_to_absolute_path(GC_SOURCES ${GC_SOURCES})

add_library_clr(gc SHARED ${GC_SOURCES})
target_link_libraries(gc ${GC_LINK_LIBRARIES})
install_clr(gc)

if(CLR_CMAKE_PLATFORM_UNIX)
  add_compile_options(-fPIC)
  # dprintf causes many warnings
  add_compile_options(-Wno-format)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_definitions(-DBUILD_AS_STANDALONE)
if(CLR_CMAKE_PLATFORM_DARWIN)
  # The implementation of GCToOSInterface on MacOS makes use of non-POSIX
  # pthreads APIs, which by default are not included in the pthreads header
  # unless we define this macro.
  add_definitions(-D_DARWIN_C_SOURCE)
endif(CLR_CMAKE_PLATFORM_DARWIN)

include_directories(${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${CMAKE_CURRENT_SOURCE_DIR}/env)

