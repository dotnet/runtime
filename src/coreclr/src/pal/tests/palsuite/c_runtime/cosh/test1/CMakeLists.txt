set(SOURCES
  test1.cpp
)

add_executable(paltest_cosh_test1
  ${SOURCES}
)

add_dependencies(paltest_cosh_test1 coreclrpal)

target_link_libraries(paltest_cosh_test1
  ${COMMON_TEST_LIBRARIES}
)
