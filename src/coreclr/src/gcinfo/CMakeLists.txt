set(CMAKE_INCLUDE_CURRENT_DIR ON)

set( GCINFO_SOURCES
  gcinfoencoder.cpp
  dbggcinfoencoder.cpp
)

if(CLR_CMAKE_PLATFORM_ARCH_I386)
  list(APPEND GCINFO_SOURCES
    ../gcdump/gcdump.cpp
    ../gcdump/${ARCH_SOURCES_DIR}/gcdumpx86.cpp
  )
endif(CLR_CMAKE_PLATFORM_ARCH_I386)

convert_to_absolute_path(GCINFO_SOURCES ${GCINFO_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(lib)
add_subdirectory(crossgen)

install (FILES gcinfoencoder.cpp
               DESTINATION gcinfo)
