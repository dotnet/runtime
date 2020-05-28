set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test6
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test6 coreclrpal)

target_link_libraries(paltest_virtualalloc_test6
  ${COMMON_TEST_LIBRARIES}
)
