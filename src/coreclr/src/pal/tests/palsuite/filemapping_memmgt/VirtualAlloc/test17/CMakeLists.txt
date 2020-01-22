set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test17
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test17 coreclrpal)

target_link_libraries(paltest_virtualalloc_test17
  ${COMMON_TEST_LIBRARIES}
)
