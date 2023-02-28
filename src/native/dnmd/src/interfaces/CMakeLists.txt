set(SOURCES
  dispenser.cpp
  metadataimport.cpp
  hcorenum.cpp
  pal.cpp
)

set(HEADERS
  ../inc/dnmd_interfaces.hpp
  ./impl.hpp
  ./pal.hpp
)

if(NOT MSVC)
  # Adds IID_* constants.
  list(APPEND SOURCES ./iids.cpp)
endif()

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

target_include_directories(dnmd_interfaces_static PUBLIC ../inc)
target_include_directories(dnmd_interfaces PRIVATE ../inc)

target_compile_definitions(dnmd_interfaces PRIVATE DNMD_BUILD_SHARED)

target_link_libraries(dnmd_interfaces_static
  dnmd
  dncp)
target_link_libraries(dnmd_interfaces
  dnmd
  dncp)

if(NOT MSVC)
  target_link_libraries(dnmd_interfaces_static dncp::winhdrs)
  target_link_libraries(dnmd_interfaces dncp::winhdrs)
endif()

if(APPLE)
  # Use ICU when running on macOS
  target_link_libraries(dnmd_interfaces icucore)
elseif(UNIX)
  # Use ICU when running on Linux
  target_link_libraries(dnmd_interfaces icuuc)
endif()

set_target_properties(dnmd_interfaces_static PROPERTIES
  PUBLIC_HEADER ../inc/dnmd_interfaces.hpp)

set_target_properties(dnmd_interfaces PROPERTIES
  PUBLIC_HEADER ../inc/dnmd_interfaces.hpp
  INTERPROCEDURAL_OPTIMIZATION $<$<NOT:$<CONFIG:DEBUG>>:TRUE>)

install(TARGETS dnmd_interfaces_static)
install(TARGETS dnmd_interfaces
  DESTINATION bin)