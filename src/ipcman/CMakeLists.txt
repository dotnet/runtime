set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DFEATURE_NO_HOST)

set(IPCMAN_SOURCES
  ipcsharedsrc.cpp
  ipcreaderimpl.cpp
  ipcwriterimpl.cpp
  ipcfunccallimpl.cpp
)

convert_to_absolute_path(IPCMAN_SOURCES ${IPCMAN_SOURCES})

add_subdirectory(ipcman-staticcrt)
