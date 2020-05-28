set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test1
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test1 coreclrpal)

target_link_libraries(paltest_virtualalloc_test1
  ${COMMON_TEST_LIBRARIES}
)
