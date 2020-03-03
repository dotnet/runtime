set(SOURCES
  test.cpp
)

add_executable(paltest_raiseexception_test3
  ${SOURCES}
)

add_dependencies(paltest_raiseexception_test3 coreclrpal)

target_link_libraries(paltest_raiseexception_test3
  ${COMMON_TEST_LIBRARIES}
)
