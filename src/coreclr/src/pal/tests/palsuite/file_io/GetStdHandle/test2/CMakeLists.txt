set(SOURCES
  GetStdHandle.cpp
)

add_executable(paltest_getstdhandle_test2
  ${SOURCES}
)

add_dependencies(paltest_getstdhandle_test2 coreclrpal)

target_link_libraries(paltest_getstdhandle_test2
  ${COMMON_TEST_LIBRARIES}
)
