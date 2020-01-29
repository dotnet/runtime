set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test8
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test8 coreclrpal)

target_link_libraries(paltest_virtualalloc_test8
  ${COMMON_TEST_LIBRARIES}
)
