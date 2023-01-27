set(SOURCES
  perf.cpp
)

add_library(regnative
  SHARED
  ${SOURCES}
)

target_link_libraries(regnative dnmd_interfaces)

install(TARGETS regnative)