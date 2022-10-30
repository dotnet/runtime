set(SOURCES
  entry.cpp
  metadataimport.cpp
)

set(HEADERS
  ../inc/dnmd_interfaces.hpp
  ./impl.hpp
)

add_library(dnmd_interfaces
  STATIC
  ${SOURCES}
  ${HEADERS}
)

set_target_properties(dnmd_interfaces PROPERTIES PUBLIC_HEADER ../inc/dnmd_interfaces.hpp)

install(TARGETS dnmd_interfaces)