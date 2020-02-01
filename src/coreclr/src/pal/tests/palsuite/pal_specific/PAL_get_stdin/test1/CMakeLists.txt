set(SOURCES
  PAL_get_stdin.cpp
)

add_executable(paltest_pal_get_stdin_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_get_stdin_test1 coreclrpal)

target_link_libraries(paltest_pal_get_stdin_test1
  ${COMMON_TEST_LIBRARIES}
)
