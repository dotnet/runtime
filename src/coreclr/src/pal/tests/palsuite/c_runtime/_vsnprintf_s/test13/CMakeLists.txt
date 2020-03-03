set(SOURCES
  test13.cpp
)

add_executable(paltest_vsnprintf_test13
  ${SOURCES}
)

add_dependencies(paltest_vsnprintf_test13 coreclrpal)

target_link_libraries(paltest_vsnprintf_test13
  ${COMMON_TEST_LIBRARIES}
)
