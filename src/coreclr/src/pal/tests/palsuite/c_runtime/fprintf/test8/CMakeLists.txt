set(SOURCES
  test8.cpp
)

add_executable(paltest_fprintf_test8
  ${SOURCES}
)

add_dependencies(paltest_fprintf_test8 coreclrpal)

target_link_libraries(paltest_fprintf_test8
  ${COMMON_TEST_LIBRARIES}
)
