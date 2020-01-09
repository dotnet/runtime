set(SOURCES
  test1.c
)

add_executable(paltest_asinf_test1
  ${SOURCES}
)

add_dependencies(paltest_asinf_test1 coreclrpal)

target_link_libraries(paltest_asinf_test1
  ${COMMON_TEST_LIBRARIES}
)
