set(SOURCES
  main.cpp
  mutex.cpp
)

add_executable(paltest_composite_wfmo
  ${SOURCES}
)

add_dependencies(paltest_composite_wfmo coreclrpal)

target_link_libraries(paltest_composite_wfmo
  ${COMMON_TEST_LIBRARIES}
  rt
)
