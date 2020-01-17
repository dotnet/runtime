set(SOURCES
  LocalAlloc.cpp
)

add_executable(paltest_localalloc_test1
  ${SOURCES}
)

add_dependencies(paltest_localalloc_test1 coreclrpal)

target_link_libraries(paltest_localalloc_test1
  ${COMMON_TEST_LIBRARIES}
)
