set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test4
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test4 coreclrpal)

target_link_libraries(paltest_virtualalloc_test4
  ${COMMON_TEST_LIBRARIES}
)
