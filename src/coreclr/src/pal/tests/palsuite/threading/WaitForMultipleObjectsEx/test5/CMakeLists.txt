set(TESTSOURCES
  test5.cpp
)

add_executable(paltest_waitformultipleobjectsex_test5
  ${TESTSOURCES}
)

add_dependencies(paltest_waitformultipleobjectsex_test5 coreclrpal)

target_link_libraries(paltest_waitformultipleobjectsex_test5
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  helper.cpp
)

add_executable(paltest_waitformultipleobjectsex_test5_helper
  ${HELPERSOURCES}
)

add_dependencies(paltest_waitformultipleobjectsex_test5_helper coreclrpal)

target_link_libraries(paltest_waitformultipleobjectsex_test5_helper
  ${COMMON_TEST_LIBRARIES}
)
