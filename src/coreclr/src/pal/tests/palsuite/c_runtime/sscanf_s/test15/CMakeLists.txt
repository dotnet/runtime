set(SOURCES
  test15.cpp
)

add_executable(paltest_sscanf_test15
  ${SOURCES}
)

add_dependencies(paltest_sscanf_test15 coreclrpal)

target_link_libraries(paltest_sscanf_test15
  ${COMMON_TEST_LIBRARIES}
)
