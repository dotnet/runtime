set(SOURCES
  test5.cpp
)

add_executable(paltest_openeventw_test5
  ${SOURCES}
)

add_dependencies(paltest_openeventw_test5 coreclrpal)

target_link_libraries(paltest_openeventw_test5
  ${COMMON_TEST_LIBRARIES}
)
