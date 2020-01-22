set(SOURCES
  HeapAlloc.cpp
)

add_executable(paltest_heapalloc_test3
  ${SOURCES}
)

add_dependencies(paltest_heapalloc_test3 coreclrpal)

target_link_libraries(paltest_heapalloc_test3
  ${COMMON_TEST_LIBRARIES}
)
