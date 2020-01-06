set(SOURCES
  LocalFree.cpp
)

add_executable(paltest_localfree_test1
  ${SOURCES}
)

add_dependencies(paltest_localfree_test1 coreclrpal)

target_link_libraries(paltest_localfree_test1
  ${COMMON_TEST_LIBRARIES}
)
