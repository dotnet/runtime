
set(CMAKE_INCLUDE_CURRENT_DIR ON)

set( GCINFO_SOURCES
  gcinfoencoder.cpp
  dbggcinfoencoder.cpp
)

convert_to_absolute_path(GCINFO_SOURCES ${GCINFO_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(lib)
if(WIN32)
  add_subdirectory(crossgen)
endif()
