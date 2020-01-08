set(SOURCES
  test7.cpp
)

add_executable(paltest_wfopen_test7
  ${SOURCES}
)

add_dependencies(paltest_wfopen_test7 coreclrpal)

target_link_libraries(paltest_wfopen_test7
  ${COMMON_TEST_LIBRARIES}
)
