set(SOURCES
  test19.cpp
)

add_executable(paltest_vsnprintf_test19
  ${SOURCES}
)

add_dependencies(paltest_vsnprintf_test19 coreclrpal)

target_link_libraries(paltest_vsnprintf_test19
  ${COMMON_TEST_LIBRARIES}
)
