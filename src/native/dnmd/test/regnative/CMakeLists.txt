set(SOURCES
  perf.cpp
  unit.cpp
)

add_library(regnative
  SHARED
  ${SOURCES}
)

target_link_libraries(regnative
  dnmd_interfaces
  dncp)

if(NOT MSVC)
  target_link_libraries(regnative dncp::winhdrs)
endif()

install(TARGETS regnative
  DESTINATION bin)