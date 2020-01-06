set(SOURCES
  SetCurrentDirectoryW.cpp
)

add_executable(paltest_setcurrentdirectoryw_test1
  ${SOURCES}
)

add_dependencies(paltest_setcurrentdirectoryw_test1 coreclrpal)

target_link_libraries(paltest_setcurrentdirectoryw_test1
  ${COMMON_TEST_LIBRARIES}
)
