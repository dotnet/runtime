set(SOURCES
  gettempfilenamew.cpp
)

add_executable(paltest_gettempfilenamew_test3
  ${SOURCES}
)

add_dependencies(paltest_gettempfilenamew_test3 coreclrpal)

target_link_libraries(paltest_gettempfilenamew_test3
  ${COMMON_TEST_LIBRARIES}
)
