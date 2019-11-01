cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test11.cpp
)

add_executable(paltest_duplicatehandle_test11
  ${TESTSOURCES}
)

add_dependencies(paltest_duplicatehandle_test11 coreclrpal)

target_link_libraries(paltest_duplicatehandle_test11
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  childprocess.cpp
)

add_executable(paltest_duplicatehandle_test11_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_duplicatehandle_test11_child coreclrpal)

target_link_libraries(paltest_duplicatehandle_test11_child
  ${COMMON_TEST_LIBRARIES}
)
