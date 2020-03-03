set(SOURCES
  VirtualFree.cpp
)

add_executable(paltest_virtualfree_test3
  ${SOURCES}
)

add_dependencies(paltest_virtualfree_test3 coreclrpal)

target_link_libraries(paltest_virtualfree_test3
  ${COMMON_TEST_LIBRARIES}
)
