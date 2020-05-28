set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test3
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test3 coreclrpal)

target_link_libraries(paltest_virtualalloc_test3
  ${COMMON_TEST_LIBRARIES}
)
