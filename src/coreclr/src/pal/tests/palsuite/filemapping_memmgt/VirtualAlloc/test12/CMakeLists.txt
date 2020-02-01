set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test12
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test12 coreclrpal)

target_link_libraries(paltest_virtualalloc_test12
  ${COMMON_TEST_LIBRARIES}
)
