set(SOURCES
  abs.cpp
)

add_executable(paltest_abs_test1
  ${SOURCES}
)

add_dependencies(paltest_abs_test1 coreclrpal)

target_link_libraries(paltest_abs_test1
  ${COMMON_TEST_LIBRARIES}
)
