set(SOURCES
  GetConsoleOutputCP.cpp
)

add_executable(paltest_getconsoleoutputcp_test1
  ${SOURCES}
)

add_dependencies(paltest_getconsoleoutputcp_test1 coreclrpal)

target_link_libraries(paltest_getconsoleoutputcp_test1
  ${COMMON_TEST_LIBRARIES}
)
