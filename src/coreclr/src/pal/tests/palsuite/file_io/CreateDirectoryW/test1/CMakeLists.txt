set(SOURCES
  CreateDirectoryW.cpp
)

add_executable(paltest_createdirectoryw_test1
  ${SOURCES}
)

add_dependencies(paltest_createdirectoryw_test1 coreclrpal)

target_link_libraries(paltest_createdirectoryw_test1
  ${COMMON_TEST_LIBRARIES}
)
