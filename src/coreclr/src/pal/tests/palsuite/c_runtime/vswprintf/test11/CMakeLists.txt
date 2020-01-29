set(SOURCES
  test11.cpp
)

add_executable(paltest_vswprintf_test11
  ${SOURCES}
)

add_dependencies(paltest_vswprintf_test11 coreclrpal)

target_link_libraries(paltest_vswprintf_test11
  ${COMMON_TEST_LIBRARIES}
)
