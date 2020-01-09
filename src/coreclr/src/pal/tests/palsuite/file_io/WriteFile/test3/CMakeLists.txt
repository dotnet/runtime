set(SOURCES
  WriteFile.cpp
)

add_executable(paltest_writefile_test3
  ${SOURCES}
)

add_dependencies(paltest_writefile_test3 coreclrpal)

target_link_libraries(paltest_writefile_test3
  ${COMMON_TEST_LIBRARIES}
)
