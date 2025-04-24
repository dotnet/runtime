set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories("../env")
include_directories("..")

include(configure.cmake)

set(GC_PAL_SOURCES
    gcenv.unix.cpp
    numasupport.cpp
    events.cpp
    cgroup.cpp)

if (CLR_CMAKE_HOST_ANDROID)
    add_library(gc_pal_objects OBJECT ${GC_PAL_SOURCES} ${VERSION_FILE_PATH})
endif()

add_library(gc_pal STATIC ${GC_PAL_SOURCES} ${VERSION_FILE_PATH})
