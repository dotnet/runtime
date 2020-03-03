set(SOURCES
  test1.cpp
)

add_executable(paltest_querythreadcycletime_test1
  ${SOURCES}
)

add_dependencies(paltest_querythreadcycletime_test1 coreclrpal)

target_link_libraries(paltest_querythreadcycletime_test1
  ${COMMON_TEST_LIBRARIES}
)
