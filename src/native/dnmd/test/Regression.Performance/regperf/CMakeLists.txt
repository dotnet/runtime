set(SOURCES
  scenarios.cpp
)

add_library(regperf
  SHARED
  ${SOURCES}
)

target_link_libraries(regperf dnmd_interfaces)

install(TARGETS regperf)