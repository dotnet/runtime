set(SOURCES
  test2.cpp
)

add_executable(paltest_strtod_test2
  ${SOURCES}
)

add_dependencies(paltest_strtod_test2 coreclrpal)

target_link_libraries(paltest_strtod_test2
  ${COMMON_TEST_LIBRARIES}
)
