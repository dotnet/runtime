set(SOURCES
  access.c
  bytes.c
  deltas.c
  editor.c
  entry.c
  query.c
  streams.c
  tables.c
  write.c
)

set(HEADERS
  ../inc/dnmd.h
  ./internal.h
)

add_library(dnmd
  STATIC
  ${SOURCES}
  ${HEADERS}
)
add_library(dnmd_pdb
  STATIC
  ${SOURCES}
  ${HEADERS}
)

target_compile_definitions(dnmd_pdb PUBLIC DNMD_PORTABLE_PDB)
target_sources(dnmd_pdb PRIVATE ../inc/dnmd_pdb.h pdb_blobs.c)
set_target_properties(dnmd_pdb PROPERTIES EXPORT_NAME pdb)

add_library(dnmd::dnmd ALIAS dnmd)
add_library(dnmd::pdb ALIAS dnmd_pdb)

target_include_directories(dnmd PUBLIC $<INSTALL_INTERFACE:include>)
target_include_directories(dnmd_pdb PUBLIC $<INSTALL_INTERFACE:include>)

set_target_properties(dnmd PROPERTIES
  PUBLIC_HEADER "${CMAKE_CURRENT_SOURCE_DIR}/../inc/dnmd.h;${CMAKE_CURRENT_SOURCE_DIR}/../inc/dnmd.hpp"
  POSITION_INDEPENDENT_CODE ON)

set_target_properties(dnmd_pdb PROPERTIES
  PUBLIC_HEADER "${CMAKE_CURRENT_SOURCE_DIR}/../inc/dnmd.h;${CMAKE_CURRENT_SOURCE_DIR}/../inc/dnmd.hpp;${CMAKE_CURRENT_SOURCE_DIR}/../inc/dnmd_pdb.h"
  POSITION_INDEPENDENT_CODE ON)

if (DNMD_INSTALL)
  install(TARGETS dnmd dnmd_pdb EXPORT dnmd
    PUBLIC_HEADER DESTINATION include
    ARCHIVE DESTINATION lib
    LIBRARY DESTINATION lib
    RUNTIME DESTINATION bin)

  install(EXPORT dnmd NAMESPACE dnmd:: FILE dnmdlib.cmake DESTINATION lib/cmake/dnmd)
endif()
