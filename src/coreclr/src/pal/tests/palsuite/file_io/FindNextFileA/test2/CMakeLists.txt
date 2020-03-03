set(SOURCES
  findnextfilea.cpp
)

add_executable(paltest_findnextfilea_test2
  ${SOURCES}
)

add_dependencies(paltest_findnextfilea_test2 coreclrpal)

target_link_libraries(paltest_findnextfilea_test2
  ${COMMON_TEST_LIBRARIES}
)
