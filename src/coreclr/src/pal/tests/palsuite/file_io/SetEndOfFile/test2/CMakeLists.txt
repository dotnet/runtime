set(SOURCES
  SetEndOfFile.cpp
)

add_executable(paltest_setendoffile_test2
  ${SOURCES}
)

add_dependencies(paltest_setendoffile_test2 coreclrpal)

target_link_libraries(paltest_setendoffile_test2
  ${COMMON_TEST_LIBRARIES}
)
