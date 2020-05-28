set(SOURCES
  ProbeMemory.cpp
)

add_executable(paltest_probememory_test1
  ${SOURCES}
)

add_dependencies(paltest_probememory_test1 coreclrpal)

target_link_libraries(paltest_probememory_test1
  ${COMMON_TEST_LIBRARIES}
)
