set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test18
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test18 coreclrpal)

target_link_libraries(paltest_virtualalloc_test18
  ${COMMON_TEST_LIBRARIES}
)
