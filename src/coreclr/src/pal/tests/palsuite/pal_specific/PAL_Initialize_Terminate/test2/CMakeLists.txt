set(SOURCES
  pal_initialize_twice.cpp
)

add_executable(paltest_pal_initialize_terminate_test2
  ${SOURCES}
)

add_dependencies(paltest_pal_initialize_terminate_test2 coreclrpal)

target_link_libraries(paltest_pal_initialize_terminate_test2
  ${COMMON_TEST_LIBRARIES}
)
