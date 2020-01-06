set(SOURCES
  GetTempFileNameW.cpp
)

add_executable(paltest_gettempfilenamew_test1
  ${SOURCES}
)

add_dependencies(paltest_gettempfilenamew_test1 coreclrpal)

target_link_libraries(paltest_gettempfilenamew_test1
  ${COMMON_TEST_LIBRARIES}
)
