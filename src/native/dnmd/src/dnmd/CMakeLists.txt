set(SOURCES
  bytes.c
  entry.c
  query.c
  streams.c
  tables.c
  sig.c
)

set(HEADERS
  ../inc/dnmd.h
  ../inc/platform.h
  ./internal.h
)

add_library(dnmd
  STATIC
  ${SOURCES}
  ${HEADERS}
)

target_include_directories(dnmd PUBLIC ../inc)

set_target_properties(dnmd PROPERTIES
  PUBLIC_HEADER ../inc/dnmd.h
  PUBLIC_HEADER ../inc/dnmd.hpp)

install(TARGETS dnmd)