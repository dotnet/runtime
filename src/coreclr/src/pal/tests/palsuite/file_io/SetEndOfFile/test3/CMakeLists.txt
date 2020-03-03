set(SOURCES
  SetEndOfFile.cpp
)

add_executable(paltest_setendoffile_test3
  ${SOURCES}
)

add_dependencies(paltest_setendoffile_test3 coreclrpal)

target_link_libraries(paltest_setendoffile_test3
  ${COMMON_TEST_LIBRARIES}
)
