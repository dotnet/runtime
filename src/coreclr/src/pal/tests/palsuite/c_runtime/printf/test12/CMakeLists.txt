set(SOURCES
  test12.cpp
)

add_executable(paltest_printf_test12
  ${SOURCES}
)

add_dependencies(paltest_printf_test12 coreclrpal)

target_link_libraries(paltest_printf_test12
  ${COMMON_TEST_LIBRARIES}
)
