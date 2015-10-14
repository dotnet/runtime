project(clrgcsample)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(..)
include_directories(../env)

set(SOURCES
    GCSample.cpp
    gcenv.ee.cpp
    ../gccommon.cpp
    ../gceewks.cpp
    ../gcscan.cpp
    ../gcwks.cpp
    ../handletable.cpp
    ../handletablecache.cpp
    ../handletablecore.cpp
    ../handletablescan.cpp
    ../objecthandle.cpp
)

if(WIN32)
    list(APPEND SOURCES
        gcenv.windows.cpp)
else()
    list(APPEND SOURCES
        gcenv.unix.cpp)
endif()

add_executable(gcsample
    ${SOURCES}
)
