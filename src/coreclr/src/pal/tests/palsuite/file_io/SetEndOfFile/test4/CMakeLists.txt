set(SOURCES
  setendoffile.cpp
)

add_executable(paltest_setendoffile_test4
  ${SOURCES}
)

add_dependencies(paltest_setendoffile_test4 coreclrpal)

target_link_libraries(paltest_setendoffile_test4
  ${COMMON_TEST_LIBRARIES}
)
