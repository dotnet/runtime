set(SOURCES
  SetEndOfFile.cpp
)

add_executable(paltest_setendoffile_test1
  ${SOURCES}
)

add_dependencies(paltest_setendoffile_test1 coreclrpal)

target_link_libraries(paltest_setendoffile_test1
  ${COMMON_TEST_LIBRARIES}
)
