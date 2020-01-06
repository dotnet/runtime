set(SOURCES
  getlongpathnamew.cpp
)

add_executable(paltest_getlongpathnamew_test2
  ${SOURCES}
)

add_dependencies(paltest_getlongpathnamew_test2 coreclrpal)

target_link_libraries(paltest_getlongpathnamew_test2
  ${COMMON_TEST_LIBRARIES}
)
