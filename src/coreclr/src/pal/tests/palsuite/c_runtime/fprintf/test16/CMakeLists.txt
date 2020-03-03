set(SOURCES
  test16.cpp
)

add_executable(paltest_fprintf_test16
  ${SOURCES}
)

add_dependencies(paltest_fprintf_test16 coreclrpal)

target_link_libraries(paltest_fprintf_test16
  ${COMMON_TEST_LIBRARIES}
)
