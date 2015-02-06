cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  loadlibrarya.c
)

add_executable(paltest_loadlibrarya_test6
  ${TESTSOURCES}
)

add_dependencies(paltest_loadlibrarya_test6 CoreClrPal)

target_link_libraries(paltest_loadlibrarya_test6
  pthread
  m
  CoreClrPal
)


set(HELPERSOURCES
  dlltest.c
)

add_executable(paltest_loadlibrarya_test6_dlltest
  ${HELPERSOURCES}
)

add_dependencies(paltest_loadlibrarya_test6_dlltest CoreClrPal)

target_link_libraries(paltest_loadlibrarya_test6_dlltest
  pthread
  m
  CoreClrPal
)
