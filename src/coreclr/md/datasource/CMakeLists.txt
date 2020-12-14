set(MDDATASOURCE_SOURCES
  api.cpp
  datatargetreader.cpp
  remotemdinternalrwsource.cpp
  targettypes.cpp
)

set(MDDATASOURCE_HEADERS
  ../../inc/cor.h
  ../../inc/corpriv.h
  datatargetreader.h
  remotemdinternalrwsource.h
  targettypes.h
)

convert_to_absolute_path(MDDATASOURCE_SOURCES ${MDDATASOURCE_SOURCES})
convert_to_absolute_path(MDDATASOURCE_HEADERS ${MDDATASOURCE_HEADERS})

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND MDDATASOURCE_SOURCES ${MDDATASOURCE_HEADERS})
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(mddatasource_dbi STATIC ${MDDATASOURCE_SOURCES})
set_target_properties(mddatasource_dbi PROPERTIES DBI_COMPONENT TRUE)
target_precompile_headers(mddatasource_dbi PRIVATE stdafx.h)
