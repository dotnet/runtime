set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories("../env")
include_directories("..")

include(configure.cmake)

set(GC_PAL_SOURCES
    gcenv.unix.cpp
    numasupport.cpp
    events.cpp
    cgroup.cpp)

add_library(gc_pal OBJECT ${GC_PAL_SOURCES} ${VERSION_FILE_PATH})
