project(unixcoreruncommon)

add_compile_options(-fPIC)

_add_library(unixcoreruncommon
    STATIC
    coreruncommon.cpp
)

if(CLR_CMAKE_PLATFORM_LINUX)
  target_link_libraries(unixcoreruncommon dl)
endif(CLR_CMAKE_PLATFORM_LINUX)
