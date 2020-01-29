set(SOURCES
  HeapAlloc.cpp
)

add_executable(paltest_heapalloc_test2
  ${SOURCES}
)

add_dependencies(paltest_heapalloc_test2 coreclrpal)

target_link_libraries(paltest_heapalloc_test2
  ${COMMON_TEST_LIBRARIES}
)
