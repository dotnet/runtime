set(SOURCES
  perf.cpp
  unit.cpp
)

add_library(regnative
  SHARED
  ${SOURCES}
)

target_link_libraries(regnative dnmd_interfaces)
target_link_libraries(regnative dncp)

install(TARGETS regnative)