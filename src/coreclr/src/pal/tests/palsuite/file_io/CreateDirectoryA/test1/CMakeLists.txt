set(SOURCES
  CreateDirectoryA.cpp
)

add_executable(paltest_createdirectorya_test1
  ${SOURCES}
)

add_dependencies(paltest_createdirectorya_test1 coreclrpal)

target_link_libraries(paltest_createdirectorya_test1
  ${COMMON_TEST_LIBRARIES}
)
