set(SOURCES
  bytes.c
  entry.c
  query.c
  streams.c
  tables.c
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

set_target_properties(dnmd PROPERTIES PUBLIC_HEADER ../inc/dnmd.h)
set_target_properties(dnmd PROPERTIES PUBLIC_HEADER ../inc/platform.h)
target_include_directories(dnmd PUBLIC ../inc)

install(TARGETS dnmd)