set(SOURCES
  test17.cpp
)

add_executable(paltest_sscanf_test17
  ${SOURCES}
)

add_dependencies(paltest_sscanf_test17 coreclrpal)

target_link_libraries(paltest_sscanf_test17
  ${COMMON_TEST_LIBRARIES}
)
