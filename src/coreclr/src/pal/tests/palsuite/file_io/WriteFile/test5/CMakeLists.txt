set(SOURCES
  writefile.cpp
)

add_executable(paltest_writefile_test5
  ${SOURCES}
)

add_dependencies(paltest_writefile_test5 coreclrpal)

target_link_libraries(paltest_writefile_test5
  ${COMMON_TEST_LIBRARIES}
)
