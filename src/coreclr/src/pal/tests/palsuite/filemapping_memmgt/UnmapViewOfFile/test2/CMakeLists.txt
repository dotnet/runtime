set(SOURCES
  unmapviewoffile.cpp
)

add_executable(paltest_unmapviewoffile_test2
  ${SOURCES}
)

add_dependencies(paltest_unmapviewoffile_test2 coreclrpal)

target_link_libraries(paltest_unmapviewoffile_test2
  ${COMMON_TEST_LIBRARIES}
)
