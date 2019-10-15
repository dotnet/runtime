cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test1.cpp
)

add_executable(paltest_outputdebugstringa_test1
  ${TESTSOURCES}
)

add_dependencies(paltest_outputdebugstringa_test1 coreclrpal)

target_link_libraries(paltest_outputdebugstringa_test1
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  helper.cpp
)

add_executable(paltest_outputdebugstringa_test1_helper
  ${HELPERSOURCES}
)

add_dependencies(paltest_outputdebugstringa_test1_helper coreclrpal)

target_link_libraries(paltest_outputdebugstringa_test1_helper
  ${COMMON_TEST_LIBRARIES}
)


