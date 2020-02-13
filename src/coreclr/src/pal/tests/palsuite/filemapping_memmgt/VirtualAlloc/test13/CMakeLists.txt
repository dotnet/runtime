set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test13
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test13 coreclrpal)

target_link_libraries(paltest_virtualalloc_test13
  ${COMMON_TEST_LIBRARIES}
)
