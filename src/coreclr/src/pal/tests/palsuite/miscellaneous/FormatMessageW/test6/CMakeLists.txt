set(SOURCES
  test.cpp
)

add_executable(paltest_formatmessagew_test6
  ${SOURCES}
)

add_dependencies(paltest_formatmessagew_test6 coreclrpal)

target_link_libraries(paltest_formatmessagew_test6
  ${COMMON_TEST_LIBRARIES}
)
