set(SOURCES
  test10.cpp
)

add_executable(paltest_vfprintf_test10
  ${SOURCES}
)

add_dependencies(paltest_vfprintf_test10 coreclrpal)

target_link_libraries(paltest_vfprintf_test10
  ${COMMON_TEST_LIBRARIES}
)
