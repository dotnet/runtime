set(SOURCES
  writefile.cpp
)

add_executable(paltest_writefile_test4
  ${SOURCES}
)

add_dependencies(paltest_writefile_test4 coreclrpal)

target_link_libraries(paltest_writefile_test4
  ${COMMON_TEST_LIBRARIES}
)
