set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories("../env")
include_directories("..")
include_directories("../unix")

set(GC_PAL_SOURCES
    gcenv.cpp
    ../unix/events.cpp)

add_library(gc_pal OBJECT ${GC_PAL_SOURCES} ${VERSION_FILE_PATH})
