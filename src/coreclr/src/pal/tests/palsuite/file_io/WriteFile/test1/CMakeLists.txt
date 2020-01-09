set(SOURCES
  WriteFile.cpp
)

add_executable(paltest_writefile_test1
  ${SOURCES}
)

add_dependencies(paltest_writefile_test1 coreclrpal)

target_link_libraries(paltest_writefile_test1
  ${COMMON_TEST_LIBRARIES}
)
