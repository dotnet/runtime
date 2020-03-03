set(TESTSOURCES
  test2.cpp
)

add_executable(paltest_exitthread_test2
  ${TESTSOURCES}
)

add_dependencies(paltest_exitthread_test2 coreclrpal)

target_link_libraries(paltest_exitthread_test2
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  childprocess.cpp
)

add_executable(paltest_exitthread_test2_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_exitthread_test2_child coreclrpal)

target_link_libraries(paltest_exitthread_test2_child
  ${COMMON_TEST_LIBRARIES}
)
