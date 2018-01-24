project(clrgcsample)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(..)
include_directories(../env)

add_definitions(-DFEATURE_REDHAWK)

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
    list(APPEND SOURCES
        ../windows/gcenv.windows.cpp)
    add_definitions(-DUNICODE=1)
else()
    list(APPEND SOURCES
        ../gcenv.unix.cpp)
endif()

_add_executable(gcsample
    ${SOURCES}
)
