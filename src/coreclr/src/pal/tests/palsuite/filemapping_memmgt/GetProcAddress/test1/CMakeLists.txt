set(SOURCES
  test1.cpp
  testlib.cpp
)

add_executable(paltest_getprocaddress_test1
  ${SOURCES}
)

add_dependencies(paltest_getprocaddress_test1 coreclrpal)

target_link_libraries(paltest_getprocaddress_test1
  ${COMMON_TEST_LIBRARIES}
)
