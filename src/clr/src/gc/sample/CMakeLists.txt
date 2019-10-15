project(clrgcsample)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(..)
include_directories(../env)

set(SOURCES
    GCSample.cpp
    gcenv.ee.cpp
    ../gceventstatus.cpp
    ../gcconfig.cpp
    ../gccommon.cpp
    ../gceewks.cpp
    ../gchandletable.cpp
    ../gcscan.cpp
    ../gcwks.cpp
    ../gcload.cpp
    ../handletable.cpp
    ../handletablecache.cpp
    ../handletablecore.cpp
    ../handletablescan.cpp
    ../objecthandle.cpp
    ../softwarewritewatch.cpp
)

if(WIN32)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    advapi32.lib)
endif(WIN32)

if(WIN32)
    list(APPEND SOURCES
        ../windows/gcenv.windows.cpp)
    add_definitions(-DUNICODE)
else()
    list(APPEND SOURCES
        ../gcenv.unix.cpp)
endif()

_add_executable(gcsample
    ${SOURCES}
)

if(WIN32)
    target_link_libraries(gcsample ${GC_LINK_LIBRARIES})
endif()
