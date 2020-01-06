set(SOURCES
  RemoveDirectoryW.cpp
)

add_executable(paltest_removedirectoryw_test1
  ${SOURCES}
)

add_dependencies(paltest_removedirectoryw_test1 coreclrpal)

target_link_libraries(paltest_removedirectoryw_test1
  ${COMMON_TEST_LIBRARIES}
)
