set(SOURCES
  ./perf.cpp
)

add_executable(regperf
  ${SOURCES}
)

target_link_libraries(regperf
  dnmd::interfaces
  minipal_com
  regpal)

if(NOT MSVC)
  target_link_libraries(regperf minipal_comhdrs)
endif()


target_link_libraries(regperf benchmark::benchmark)

install(DIRECTORY ${RegressionLocatorDirectory} DESTINATION bin)
install(TARGETS regperf DESTINATION bin)
