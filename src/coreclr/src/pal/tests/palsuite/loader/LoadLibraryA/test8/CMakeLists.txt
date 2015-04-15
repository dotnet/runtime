cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  loadlibrarya.c
)

add_executable(paltest_loadlibrarya_test8
  ${TESTSOURCES}
)

add_dependencies(paltest_loadlibrarya_test8 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test8
  pthread
  m
  coreclrpal
)


set(HELPERSOURCES
  dlltest.c
)

add_executable(paltest_loadlibrarya_test8_dlltest
  ${HELPERSOURCES}
)

add_dependencies(paltest_loadlibrarya_test8_dlltest coreclrpal)

target_link_libraries(paltest_loadlibrarya_test8_dlltest
  pthread
  m
  coreclrpal
)
