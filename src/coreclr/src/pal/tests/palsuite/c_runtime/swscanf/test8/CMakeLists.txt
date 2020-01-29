set(SOURCES
  test8.cpp
)

add_executable(paltest_swscanf_test8
  ${SOURCES}
)

add_dependencies(paltest_swscanf_test8 coreclrpal)

target_link_libraries(paltest_swscanf_test8
  ${COMMON_TEST_LIBRARIES}
)
