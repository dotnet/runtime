cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  loadlibrarya.c
)

add_executable(paltest_loadlibrarya_test6
  ${TESTSOURCES}
)

add_dependencies(paltest_loadlibrarya_test6 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test6
  pthread
  m
  coreclrpal
)


set(HELPERSOURCES
  dlltest.c
)

add_executable(paltest_loadlibrarya_test6_dlltest
  ${HELPERSOURCES}
)

add_dependencies(paltest_loadlibrarya_test6_dlltest coreclrpal)

target_link_libraries(paltest_loadlibrarya_test6_dlltest
  pthread
  m
  coreclrpal
)
