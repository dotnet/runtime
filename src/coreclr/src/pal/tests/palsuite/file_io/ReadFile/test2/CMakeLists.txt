set(SOURCES
  ReadFile.cpp
)

add_executable(paltest_readfile_test2
  ${SOURCES}
)

add_dependencies(paltest_readfile_test2 coreclrpal)

target_link_libraries(paltest_readfile_test2
  ${COMMON_TEST_LIBRARIES}
)
