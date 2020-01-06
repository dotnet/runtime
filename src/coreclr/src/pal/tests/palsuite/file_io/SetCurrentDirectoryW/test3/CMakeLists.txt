set(SOURCES
  setcurrentdirectoryw.cpp
)

add_executable(paltest_setcurrentdirectoryw_test3
  ${SOURCES}
)

add_dependencies(paltest_setcurrentdirectoryw_test3 coreclrpal)

target_link_libraries(paltest_setcurrentdirectoryw_test3
  ${COMMON_TEST_LIBRARIES}
)
