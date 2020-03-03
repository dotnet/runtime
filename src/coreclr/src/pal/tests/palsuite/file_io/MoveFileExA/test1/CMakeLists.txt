set(SOURCES
  MoveFileExA.cpp
)

add_executable(paltest_movefileexa_test1
  ${SOURCES}
)

add_dependencies(paltest_movefileexa_test1 coreclrpal)

target_link_libraries(paltest_movefileexa_test1
  ${COMMON_TEST_LIBRARIES}
)
