set(SOURCES
  GetTempFileNameA.cpp
)

add_executable(paltest_gettempfilenamea_test2
  ${SOURCES}
)

add_dependencies(paltest_gettempfilenamea_test2 coreclrpal)

target_link_libraries(paltest_gettempfilenamea_test2
  ${COMMON_TEST_LIBRARIES}
)
