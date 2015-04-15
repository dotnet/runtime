cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  PAL_TRY_EXCEPT_EX.c
)

add_executable(paltest_pal_try_except_ex_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_try_except_ex_test1 coreclrpal)

target_link_libraries(paltest_pal_try_except_ex_test1
  pthread
  m
  coreclrpal
)
