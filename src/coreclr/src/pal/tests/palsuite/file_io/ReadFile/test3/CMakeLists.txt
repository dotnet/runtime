set(SOURCES
  ReadFile.cpp
)

add_executable(paltest_readfile_test3
  ${SOURCES}
)

add_dependencies(paltest_readfile_test3 coreclrpal)

target_link_libraries(paltest_readfile_test3
  ${COMMON_TEST_LIBRARIES}
)
