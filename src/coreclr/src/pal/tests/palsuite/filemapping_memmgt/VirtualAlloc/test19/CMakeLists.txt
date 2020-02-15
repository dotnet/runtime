set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test19
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test19 coreclrpal)

target_link_libraries(paltest_virtualalloc_test19
  ${COMMON_TEST_LIBRARIES}
)
