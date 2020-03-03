project(unixcoreruncommon)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include(configure.cmake)

_add_library(unixcoreruncommon
    STATIC
    coreruncommon.cpp
)

if(CLR_CMAKE_HOST_LINUX)
  target_link_libraries(unixcoreruncommon dl)
endif(CLR_CMAKE_HOST_LINUX)

if(CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_ARM)
    add_definitions(-D_FILE_OFFSET_BITS=64)
endif()
