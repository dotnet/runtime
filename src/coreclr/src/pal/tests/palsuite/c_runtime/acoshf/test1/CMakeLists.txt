set(SOURCES
  test1.c
)

add_executable(paltest_acoshf_test1
  ${SOURCES}
)

add_dependencies(paltest_acoshf_test1 coreclrpal)

target_link_libraries(paltest_acoshf_test1
  ${COMMON_TEST_LIBRARIES}
)
