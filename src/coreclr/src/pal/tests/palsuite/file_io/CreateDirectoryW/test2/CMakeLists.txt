set(SOURCES
  createdirectoryw.cpp
)

add_executable(paltest_createdirectoryw_test2
  ${SOURCES}
)

add_dependencies(paltest_createdirectoryw_test2 coreclrpal)

target_link_libraries(paltest_createdirectoryw_test2
  ${COMMON_TEST_LIBRARIES}
)
