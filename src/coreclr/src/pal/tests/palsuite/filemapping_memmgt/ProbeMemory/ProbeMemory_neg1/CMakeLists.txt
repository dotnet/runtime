set(SOURCES
  ProbeMemory_neg.cpp
)

add_executable(paltest_probememory_probememory_neg1
  ${SOURCES}
)

add_dependencies(paltest_probememory_probememory_neg1 coreclrpal)

target_link_libraries(paltest_probememory_probememory_neg1
  ${COMMON_TEST_LIBRARIES}
)
