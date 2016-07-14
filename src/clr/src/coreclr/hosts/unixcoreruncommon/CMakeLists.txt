project(unixcoreruncommon)

add_compile_options(-fPIC)

if(FEATURE_GDBJIT)    
   add_definitions(-DFEATURE_GDBJIT)
endif(FEATURE_GDBJIT)

_add_library(unixcoreruncommon
    STATIC
    coreruncommon.cpp
)

if(CLR_CMAKE_PLATFORM_LINUX)
  target_link_libraries(unixcoreruncommon dl)
endif(CLR_CMAKE_PLATFORM_LINUX)
