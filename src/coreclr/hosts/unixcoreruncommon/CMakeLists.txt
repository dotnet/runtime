project(unixcoreruncommon)

add_compile_options(-fPIC)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include(configure.cmake)

_add_library(unixcoreruncommon
    STATIC
    coreruncommon.cpp
)

if(CLR_CMAKE_PLATFORM_LINUX)
  target_link_libraries(unixcoreruncommon dl)
endif(CLR_CMAKE_PLATFORM_LINUX)
