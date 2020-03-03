set(SOURCES
  VirtualFree.cpp
)

add_executable(paltest_virtualfree_test2
  ${SOURCES}
)

add_dependencies(paltest_virtualfree_test2 coreclrpal)

target_link_libraries(paltest_virtualfree_test2
  ${COMMON_TEST_LIBRARIES}
)
