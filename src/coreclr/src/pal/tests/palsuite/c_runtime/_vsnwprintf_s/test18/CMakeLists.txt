set(SOURCES
  test18.cpp
)

add_executable(paltest_vsnwprintf_test18
  ${SOURCES}
)

add_dependencies(paltest_vsnwprintf_test18 coreclrpal)

target_link_libraries(paltest_vsnwprintf_test18
  ${COMMON_TEST_LIBRARIES}
)
