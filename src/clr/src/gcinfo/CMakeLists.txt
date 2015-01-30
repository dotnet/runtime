
set(CMAKE_INCLUDE_CURRENT_DIR ON)

set( GCINFO_SOURCES
  gcinfoencoder.cpp
  dbggcinfoencoder.cpp
)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_library(gcinfo
    STATIC
    ${GCINFO_SOURCES}
)
