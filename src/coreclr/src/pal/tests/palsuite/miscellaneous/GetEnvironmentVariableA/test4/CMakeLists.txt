set(SOURCES
  test.cpp
)

add_executable(paltest_getenvironmentvariablea_test4
  ${SOURCES}
)

add_dependencies(paltest_getenvironmentvariablea_test4 coreclrpal)

target_link_libraries(paltest_getenvironmentvariablea_test4
  ${COMMON_TEST_LIBRARIES}
)
