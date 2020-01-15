set(SOURCES
  HeapFree.cpp
)

add_executable(paltest_heapfree_test1
  ${SOURCES}
)

add_dependencies(paltest_heapfree_test1 coreclrpal)

target_link_libraries(paltest_heapfree_test1
  ${COMMON_TEST_LIBRARIES}
)
