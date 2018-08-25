set(CMAKE_INCLUDE_CURRENT_DIR ON)
add_compile_options(-fPIC)
include_directories("../env")

include(configure.cmake)

set(GC_PAL_SOURCES
    gcenv.unix.cpp
    events.cpp
    cgroup.cpp
    cpuinfo.cpp)

add_library(gc_unix STATIC ${GC_PAL_SOURCES} ${VERSION_FILE_PATH})
