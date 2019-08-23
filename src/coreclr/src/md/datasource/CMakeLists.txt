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

add_subdirectory(dbi)
