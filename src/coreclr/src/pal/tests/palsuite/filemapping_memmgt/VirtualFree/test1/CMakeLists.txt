set(SOURCES
  VirtualFree.cpp
)

add_executable(paltest_virtualfree_test1
  ${SOURCES}
)

add_dependencies(paltest_virtualfree_test1 coreclrpal)

target_link_libraries(paltest_virtualfree_test1
  ${COMMON_TEST_LIBRARIES}
)
