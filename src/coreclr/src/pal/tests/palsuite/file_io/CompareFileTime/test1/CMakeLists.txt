set(SOURCES
  CompareFileTime.cpp
)

add_executable(paltest_comparefiletime_test1
  ${SOURCES}
)

add_dependencies(paltest_comparefiletime_test1 coreclrpal)

target_link_libraries(paltest_comparefiletime_test1
  ${COMMON_TEST_LIBRARIES}
)
