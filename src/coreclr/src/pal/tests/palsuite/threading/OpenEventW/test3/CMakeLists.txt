set(TESTSOURCES
  test3.cpp
)

add_executable(paltest_openeventw_test3
  ${TESTSOURCES}
)

add_dependencies(paltest_openeventw_test3 coreclrpal)

target_link_libraries(paltest_openeventw_test3
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  childprocess.cpp
)

add_executable(paltest_openeventw_test3_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_openeventw_test3_child coreclrpal)

target_link_libraries(paltest_openeventw_test3_child
  ${COMMON_TEST_LIBRARIES}
)
