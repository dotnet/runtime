set(SOURCES
  test16.cpp
)

add_executable(paltest_sscanf_test16
  ${SOURCES}
)

add_dependencies(paltest_sscanf_test16 coreclrpal)

target_link_libraries(paltest_sscanf_test16
  ${COMMON_TEST_LIBRARIES}
)
