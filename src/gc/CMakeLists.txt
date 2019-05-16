set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Local GC meta-issue: https://github.com/dotnet/coreclr/issues/11518

# https://github.com/dotnet/coreclr/issues/11516
remove_definitions(-DSTRESS_HEAP)

# https://github.com/dotnet/coreclr/issues/11519
remove_definitions(-DWRITE_BARRIER_CHECK)

set( GC_SOURCES
  gceventstatus.cpp
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
  gcload.cpp
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

if (WIN32)
  set(GC_HEADERS
    env/common.h
    env/etmdummy.h
    env/gcenv.base.h
    env/gcenv.ee.h
    env/gcenv.h
    env/gcenv.interlocked.h
    env/gcenv.interlocked.inl
    env/gcenv.object.h
    env/gcenv.os.h
    env/gcenv.structs.h
    env/gcenv.sync.h
    env/gcenv.windows.inl
    env/volatile.h
    gc.h
    gcconfig.h
    gcdesc.h
    gcenv.ee.standalone.inl
    gcenv.inl
    gcevent_serializers.h
    gcevents.h
    gceventstatus.h
    gchandletableimpl.h
    gcimpl.h
    gcinterface.dac.h
    gcinterface.ee.h
    gcinterface.h
    gcpriv.h
    gcrecord.h
    gcscan.h
    handletable.h
    handletable.inl
    handletablepriv.h
    objecthandle.h
    softwarewritewatch.h)
endif(WIN32)

if(WIN32)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    advapi32.lib)
else()
  set (GC_LINK_LIBRARIES)
endif(WIN32)

list(APPEND GC_SOURCES ${GC_HEADERS})

convert_to_absolute_path(GC_SOURCES ${GC_SOURCES})

add_library_clr(clrgc SHARED ${GC_SOURCES})
add_dependencies(clrgc eventing_headers)
target_link_libraries(clrgc ${GC_LINK_LIBRARIES})
install_clr(clrgc)

if(CLR_CMAKE_PLATFORM_UNIX)
  add_compile_options(-fPIC)
  # dprintf causes many warnings (https://github.com/dotnet/coreclr/issues/13367)
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

