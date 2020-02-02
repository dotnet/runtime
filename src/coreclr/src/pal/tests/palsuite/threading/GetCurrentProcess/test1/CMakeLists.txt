set(SOURCES
  process.cpp
)

add_executable(paltest_getcurrentprocess_test1
  ${SOURCES}
)

add_dependencies(paltest_getcurrentprocess_test1 coreclrpal)

target_link_libraries(paltest_getcurrentprocess_test1
  ${COMMON_TEST_LIBRARIES}
)
