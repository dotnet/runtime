set(SOURCES
  test11.cpp
)

add_executable(paltest_sscanf_test11
  ${SOURCES}
)

add_dependencies(paltest_sscanf_test11 coreclrpal)

target_link_libraries(paltest_sscanf_test11
  ${COMMON_TEST_LIBRARIES}
)
