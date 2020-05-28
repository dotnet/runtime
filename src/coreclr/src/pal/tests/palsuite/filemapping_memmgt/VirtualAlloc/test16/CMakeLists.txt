set(SOURCES
  VirtualAlloc.cpp
)

add_executable(paltest_virtualalloc_test16
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test16 coreclrpal)

target_link_libraries(paltest_virtualalloc_test16
  ${COMMON_TEST_LIBRARIES}
)
