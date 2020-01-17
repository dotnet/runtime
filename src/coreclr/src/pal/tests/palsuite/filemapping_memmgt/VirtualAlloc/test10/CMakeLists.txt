set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test10
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test10 coreclrpal)

target_link_libraries(paltest_virtualalloc_test10
  ${COMMON_TEST_LIBRARIES}
)
