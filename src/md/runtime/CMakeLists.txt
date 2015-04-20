add_definitions(-DNO_COR)

set(MDRUNTIME_SOURCES
  mdcolumndescriptors.cpp
  liteweightstgdb.cpp
  mdfileformat.cpp
  metamodel.cpp
  metamodelro.cpp
  recordpool.cpp
  mdinternaldisp.cpp
  mdinternalro.cpp
)

convert_to_absolute_path(MDRUNTIME_SOURCES ${MDRUNTIME_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(wks)
add_subdirectory(dbi)
if(WIN32)
  add_subdirectory(crossgen)
endif(WIN32)
