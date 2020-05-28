set(SOURCES
  VirtualQuery.cpp
)

add_executable(paltest_virtualquery_test1
  ${SOURCES}
)

add_dependencies(paltest_virtualquery_test1 coreclrpal)

target_link_libraries(paltest_virtualquery_test1
  ${COMMON_TEST_LIBRARIES}
)
