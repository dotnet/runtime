set(SOURCES
  processId.cpp
)

add_executable(paltest_getcurrentprocessid_test1
  ${SOURCES}
)

add_dependencies(paltest_getcurrentprocessid_test1 coreclrpal)

target_link_libraries(paltest_getcurrentprocessid_test1
  ${COMMON_TEST_LIBRARIES}
)
