set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test15
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test15 coreclrpal)

target_link_libraries(paltest_virtualalloc_test15
  ${COMMON_TEST_LIBRARIES}
)
