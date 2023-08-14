set(SOURCES
  access.c
  bytes.c
  deltas.c
  editor.c
  entry.c
  query.c
  streams.c
  tables.c
  sig.c
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

add_library(dnmd::dnmd ALIAS dnmd)

target_include_directories(dnmd PUBLIC $<INSTALL_INTERFACE:include>)

set_target_properties(dnmd PROPERTIES
  PUBLIC_HEADER "../inc/dnmd.h;../inc/dnmd.hpp")

install(TARGETS dnmd EXPORT dnmd
  PUBLIC_HEADER DESTINATION include
  ARCHIVE DESTINATION lib
  LIBRARY DESTINATION lib
  RUNTIME DESTINATION bin)

install(EXPORT dnmd NAMESPACE dnmd:: FILE dnmdlib.cmake DESTINATION lib/cmake/dnmd)
