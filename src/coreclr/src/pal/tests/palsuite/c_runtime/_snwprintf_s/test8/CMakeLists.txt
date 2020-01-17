set(SOURCES
  test8.cpp
)

add_executable(paltest_snwprintf_test8
  ${SOURCES}
)

add_dependencies(paltest_snwprintf_test8 coreclrpal)

target_link_libraries(paltest_snwprintf_test8
  ${COMMON_TEST_LIBRARIES}
)
