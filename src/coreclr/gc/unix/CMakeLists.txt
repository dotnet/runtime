set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories("../env")

include(configure.cmake)

set(GC_PAL_SOURCES
    gcenv.unix.cpp
    events.cpp
    cgroup.cpp)

add_library(gc_pal STATIC ${GC_PAL_SOURCES} ${VERSION_FILE_PATH})
