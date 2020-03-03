set(SOURCES
  ReadFile.cpp
)

add_executable(paltest_readfile_test1
  ${SOURCES}
)

add_dependencies(paltest_readfile_test1 coreclrpal)

target_link_libraries(paltest_readfile_test1
  ${COMMON_TEST_LIBRARIES}
)
