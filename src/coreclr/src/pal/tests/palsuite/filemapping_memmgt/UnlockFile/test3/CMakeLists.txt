cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test3.c
)

add_executable(paltest_unlockfile_test3
  ${TESTSOURCES}
)

add_dependencies(paltest_unlockfile_test3 coreclrpal)

target_link_libraries(paltest_unlockfile_test3
  pthread
  m
  coreclrpal
)


set(HELPERSOURCES
  helper.c
)

add_executable(paltest_unlockfile_test3_helper
  ${HELPERSOURCES}
)

add_dependencies(paltest_unlockfile_test3_helper coreclrpal)

target_link_libraries(paltest_unlockfile_test3_helper
  pthread
  m
  coreclrpal
)
