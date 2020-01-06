set(SOURCES
  test2.cpp
  testlib.cpp
)

add_executable(paltest_getprocaddress_test2
  ${SOURCES}
)

add_dependencies(paltest_getprocaddress_test2 coreclrpal)

target_link_libraries(paltest_getprocaddress_test2
  ${COMMON_TEST_LIBRARIES}
)
