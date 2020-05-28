set(SOURCES
  UnmapViewOfFile.cpp
)

add_executable(paltest_unmapviewoffile_test1
  ${SOURCES}
)

add_dependencies(paltest_unmapviewoffile_test1 coreclrpal)

target_link_libraries(paltest_unmapviewoffile_test1
  ${COMMON_TEST_LIBRARIES}
)
