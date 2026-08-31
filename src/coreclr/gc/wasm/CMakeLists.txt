set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories("../env")
include_directories("..")
include_directories("../unix")

# Generates config.gc.h in this subdirectory's binary dir (picked up via
# CMAKE_INCLUDE_CURRENT_DIR). Mirrors how gc/unix/CMakeLists.txt does it.
include(../unix/configure.cmake)

set(GC_PAL_SOURCES
    gcenv.cpp
    ../unix/events.cpp)

add_library(gc_pal OBJECT ${GC_PAL_SOURCES} ${VERSION_FILE_PATH})
