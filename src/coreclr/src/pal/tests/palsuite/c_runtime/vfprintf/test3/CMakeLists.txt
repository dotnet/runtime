set(SOURCES
  test3.cpp
)

add_executable(paltest_vfprintf_test3
  ${SOURCES}
)

add_dependencies(paltest_vfprintf_test3 coreclrpal)

target_link_libraries(paltest_vfprintf_test3
  ${COMMON_TEST_LIBRARIES}
)
