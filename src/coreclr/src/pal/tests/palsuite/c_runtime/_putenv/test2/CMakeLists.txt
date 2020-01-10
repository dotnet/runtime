set(SOURCES
  test2.cpp
)

add_executable(paltest_putenv_test2
  ${SOURCES}
)

add_dependencies(paltest_putenv_test2 coreclrpal)

target_link_libraries(paltest_putenv_test2
  ${COMMON_TEST_LIBRARIES}
)
