set(SOURCES
  threadId.cpp
)

add_executable(paltest_getcurrentthreadid_test1
  ${SOURCES}
)

add_dependencies(paltest_getcurrentthreadid_test1 coreclrpal)

target_link_libraries(paltest_getcurrentthreadid_test1
  ${COMMON_TEST_LIBRARIES}
)
