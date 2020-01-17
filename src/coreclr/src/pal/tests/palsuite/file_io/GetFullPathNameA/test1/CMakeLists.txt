set(SOURCES
  GetFullPathNameA.cpp
)

add_executable(paltest_getfullpathnamea_test1
  ${SOURCES}
)

add_dependencies(paltest_getfullpathnamea_test1 coreclrpal)

target_link_libraries(paltest_getfullpathnamea_test1
  ${COMMON_TEST_LIBRARIES}
)
