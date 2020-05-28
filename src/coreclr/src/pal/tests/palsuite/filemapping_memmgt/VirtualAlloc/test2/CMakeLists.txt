set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test2
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test2 coreclrpal)

target_link_libraries(paltest_virtualalloc_test2
  ${COMMON_TEST_LIBRARIES}
)
