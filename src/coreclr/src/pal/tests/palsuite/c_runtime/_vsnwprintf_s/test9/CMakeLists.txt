set(SOURCES
  test9.cpp
)

add_executable(paltest_vsnwprintf_test9
  ${SOURCES}
)

add_dependencies(paltest_vsnwprintf_test9 coreclrpal)

target_link_libraries(paltest_vsnwprintf_test9
  ${COMMON_TEST_LIBRARIES}
)
