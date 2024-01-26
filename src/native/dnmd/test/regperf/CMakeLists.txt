set(SOURCES
  ./perf.cpp
)

add_executable(regperf
  ${SOURCES}
)

target_link_libraries(regperf
  dnmd::interfaces
  dncp::dncp
  regpal)

if(NOT MSVC)
  target_link_libraries(regperf dncp::winhdrs)
endif()


target_link_libraries(regperf benchmark::benchmark)

install(DIRECTORY ${RegressionLocatorDirectory} DESTINATION bin)
install(TARGETS regperf DESTINATION bin)