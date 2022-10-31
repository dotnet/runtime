set(SOURCES
  dispenser.cpp
  metadataimport.cpp
)

set(HEADERS
  ../inc/dnmd_interfaces.hpp
  ./impl.hpp
)

add_library(dnmd_interfaces_static
  STATIC
  ${SOURCES}
  ${HEADERS}
)

add_library(dnmd_interfaces
  SHARED
  ${SOURCES}
  ${HEADERS}
)

target_compile_definitions(dnmd_interfaces PRIVATE DNMD_BUILD_SHARED)
target_link_libraries(dnmd_interfaces dnmd)

set_target_properties(dnmd_interfaces_static PROPERTIES PUBLIC_HEADER ../inc/dnmd_interfaces.hpp)
set_target_properties(dnmd_interfaces PROPERTIES PUBLIC_HEADER ../inc/dnmd_interfaces.hpp)

install(TARGETS dnmd_interfaces_static)
install(TARGETS dnmd_interfaces)