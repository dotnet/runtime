add_definitions(-DNO_CRT)

set(MDDATASOURCE_SOURCES
  api.cpp
  DataTargetReader.cpp
  RemoteMDInternalRWSource.cpp
  TargetTypes.cpp
)

convert_to_absolute_path(MDDATASOURCE_SOURCES ${MDDATASOURCE_SOURCES})

add_subdirectory(dbi)

