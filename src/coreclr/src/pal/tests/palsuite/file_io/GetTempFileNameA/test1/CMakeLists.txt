set(SOURCES
  GetTempFileNameA.cpp
)

add_executable(paltest_gettempfilenamea_test1
  ${SOURCES}
)

add_dependencies(paltest_gettempfilenamea_test1 coreclrpal)

target_link_libraries(paltest_gettempfilenamea_test1
  ${COMMON_TEST_LIBRARIES}
)
