set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test11
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test11 coreclrpal)

target_link_libraries(paltest_virtualalloc_test11
  ${COMMON_TEST_LIBRARIES}
)
