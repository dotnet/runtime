set(SOURCES
  isprint.cpp
)

add_executable(paltest_isprint_test1
  ${SOURCES}
)

add_dependencies(paltest_isprint_test1 coreclrpal)

target_link_libraries(paltest_isprint_test1
  ${COMMON_TEST_LIBRARIES}
)
