set(SOURCES
  PAL_get_stderr.cpp
)

add_executable(paltest_pal_get_stderr_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_get_stderr_test1 coreclrpal)

target_link_libraries(paltest_pal_get_stderr_test1
  ${COMMON_TEST_LIBRARIES}
)
