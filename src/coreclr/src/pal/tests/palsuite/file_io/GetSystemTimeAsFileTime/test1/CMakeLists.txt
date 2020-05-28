set(SOURCES
  GetSystemTimeAsFileTime.cpp
)

add_executable(paltest_getsystemtimeasfiletime_test1
  ${SOURCES}
)

add_dependencies(paltest_getsystemtimeasfiletime_test1 coreclrpal)

target_link_libraries(paltest_getsystemtimeasfiletime_test1
  ${COMMON_TEST_LIBRARIES}
)
