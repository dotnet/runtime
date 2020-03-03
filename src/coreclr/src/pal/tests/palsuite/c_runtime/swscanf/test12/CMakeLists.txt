set(SOURCES
  test12.cpp
)

add_executable(paltest_swscanf_test12
  ${SOURCES}
)

add_dependencies(paltest_swscanf_test12 coreclrpal)

target_link_libraries(paltest_swscanf_test12
  ${COMMON_TEST_LIBRARIES}
)
