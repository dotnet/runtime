set(SOURCES
  GetCurrentDirectoryW.cpp
)

add_executable(paltest_getcurrentdirectoryw_test1
  ${SOURCES}
)

add_dependencies(paltest_getcurrentdirectoryw_test1 coreclrpal)

target_link_libraries(paltest_getcurrentdirectoryw_test1
  ${COMMON_TEST_LIBRARIES}
)
