set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test5
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test5 coreclrpal)

target_link_libraries(paltest_virtualalloc_test5
  ${COMMON_TEST_LIBRARIES}
)
