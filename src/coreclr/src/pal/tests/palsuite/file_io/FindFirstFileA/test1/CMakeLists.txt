set(SOURCES
  FindFirstFileA.cpp
)

add_executable(paltest_findfirstfilea_test1
  ${SOURCES}
)

add_dependencies(paltest_findfirstfilea_test1 coreclrpal)

target_link_libraries(paltest_findfirstfilea_test1
  ${COMMON_TEST_LIBRARIES}
)
