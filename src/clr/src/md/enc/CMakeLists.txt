set(MDRUNTIMERW_SOURCES
  liteweightstgdbrw.cpp
  metamodelenc.cpp
  metamodelrw.cpp
  peparse.cpp
  rwutil.cpp
  stgio.cpp
  stgtiggerstorage.cpp
  stgtiggerstream.cpp
  mdinternalrw.cpp
)

convert_to_absolute_path(MDRUNTIMERW_SOURCES ${MDRUNTIMERW_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(wks)
add_subdirectory(dbi)
add_subdirectory(crossgen)
