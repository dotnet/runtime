set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test9
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test9 coreclrpal)

target_link_libraries(paltest_virtualalloc_test9
  ${COMMON_TEST_LIBRARIES}
)
