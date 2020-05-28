set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test7
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test7 coreclrpal)

target_link_libraries(paltest_virtualalloc_test7
  ${COMMON_TEST_LIBRARIES}
)
