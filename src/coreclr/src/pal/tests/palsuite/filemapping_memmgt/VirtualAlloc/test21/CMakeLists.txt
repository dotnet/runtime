set(SOURCES
  virtualalloc.cpp
)

add_executable(paltest_virtualalloc_test21
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test21 coreclrpal)

target_link_libraries(paltest_virtualalloc_test21
  ${COMMON_TEST_LIBRARIES}
)
