set(SOURCES
  RemoveDirectoryA.cpp
)

add_executable(paltest_removedirectorya_test1
  ${SOURCES}
)

add_dependencies(paltest_removedirectorya_test1 coreclrpal)

target_link_libraries(paltest_removedirectorya_test1
  ${COMMON_TEST_LIBRARIES}
)
