set(SOURCES
  GetStdHandle.cpp
)

add_executable(paltest_getstdhandle_test1
  ${SOURCES}
)

add_dependencies(paltest_getstdhandle_test1 coreclrpal)

target_link_libraries(paltest_getstdhandle_test1
  ${COMMON_TEST_LIBRARIES}
)
