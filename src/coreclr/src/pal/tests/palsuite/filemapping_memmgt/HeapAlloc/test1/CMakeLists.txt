set(SOURCES
  HeapAlloc.cpp
)

add_executable(paltest_heapalloc_test1
  ${SOURCES}
)

add_dependencies(paltest_heapalloc_test1 coreclrpal)

target_link_libraries(paltest_heapalloc_test1
  ${COMMON_TEST_LIBRARIES}
)
