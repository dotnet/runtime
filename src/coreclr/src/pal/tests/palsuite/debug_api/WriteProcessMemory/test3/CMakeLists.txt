set(TESTSOURCES
  test3.cpp
)

add_executable(paltest_writeprocessmemory_test3
  ${TESTSOURCES}
)

add_dependencies(paltest_writeprocessmemory_test3 coreclrpal)

target_link_libraries(paltest_writeprocessmemory_test3
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  helper.cpp
)

add_executable(paltest_writeprocessmemory_test3_helper
  ${HELPERSOURCES}
)

add_dependencies(paltest_writeprocessmemory_test3_helper coreclrpal)

target_link_libraries(paltest_writeprocessmemory_test3_helper
  ${COMMON_TEST_LIBRARIES}
)
