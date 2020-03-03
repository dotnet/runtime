set(SOURCES
  test.cpp
)

add_executable(paltest_formatmessagew_test3
  ${SOURCES}
)

add_dependencies(paltest_formatmessagew_test3 coreclrpal)

target_link_libraries(paltest_formatmessagew_test3
  ${COMMON_TEST_LIBRARIES}
)
