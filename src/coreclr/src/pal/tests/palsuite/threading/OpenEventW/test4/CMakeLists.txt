set(SOURCES
  test4.cpp
)

add_executable(paltest_openeventw_test4
  ${SOURCES}
)

add_dependencies(paltest_openeventw_test4 coreclrpal)

target_link_libraries(paltest_openeventw_test4
  ${COMMON_TEST_LIBRARIES}
)
