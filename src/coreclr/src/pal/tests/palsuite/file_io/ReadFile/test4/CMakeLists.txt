set(SOURCES
  readfile.cpp
)

add_executable(paltest_readfile_test4
  ${SOURCES}
)

add_dependencies(paltest_readfile_test4 coreclrpal)

target_link_libraries(paltest_readfile_test4
  ${COMMON_TEST_LIBRARIES}
)
