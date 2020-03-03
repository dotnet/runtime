set(SOURCES
  test.cpp
)

add_executable(paltest_cgroup_test1
  ${SOURCES}
)

add_dependencies(paltest_cgroup_test1 coreclrpal)

target_link_libraries(paltest_cgroup_test1
  ${COMMON_TEST_LIBRARIES}
)
