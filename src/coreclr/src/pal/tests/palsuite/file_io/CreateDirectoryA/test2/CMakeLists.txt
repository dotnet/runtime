set(SOURCES
  createdirectorya.cpp
)

add_executable(paltest_createdirectorya_test2
  ${SOURCES}
)

add_dependencies(paltest_createdirectorya_test2 coreclrpal)

target_link_libraries(paltest_createdirectorya_test2
  ${COMMON_TEST_LIBRARIES}
)
