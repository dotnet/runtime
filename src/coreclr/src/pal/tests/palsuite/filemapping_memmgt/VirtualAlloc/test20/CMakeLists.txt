set(SOURCES
  virtualalloc.cpp
)

add_executable(paltest_virtualalloc_test20
  ${SOURCES}
)

add_dependencies(paltest_virtualalloc_test20 coreclrpal)

target_link_libraries(paltest_virtualalloc_test20
  ${COMMON_TEST_LIBRARIES}
)
