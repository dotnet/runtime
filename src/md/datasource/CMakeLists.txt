if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(WIN32)
    add_definitions(-DNO_CRT)
endif(WIN32)


set(MDDATASOURCE_SOURCES
  api.cpp
  datatargetreader.cpp
  remotemdinternalrwsource.cpp
  targettypes.cpp
)

convert_to_absolute_path(MDDATASOURCE_SOURCES ${MDDATASOURCE_SOURCES})

add_subdirectory(dbi)

