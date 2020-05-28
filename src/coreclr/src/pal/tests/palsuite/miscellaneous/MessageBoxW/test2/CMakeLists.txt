set(SOURCES
  test.cpp
)

add_executable(paltest_messageboxw_test2
  ${SOURCES}
)

add_dependencies(paltest_messageboxw_test2 coreclrpal)

target_link_libraries(paltest_messageboxw_test2
  ${COMMON_TEST_LIBRARIES}
)
