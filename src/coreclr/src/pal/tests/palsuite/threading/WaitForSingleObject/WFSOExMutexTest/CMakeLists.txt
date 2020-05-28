set(SOURCES
  WFSOExMutexTest.cpp
)

add_executable(paltest_waitforsingleobject_wfsoexmutextest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsoexmutextest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsoexmutextest
  ${COMMON_TEST_LIBRARIES}
)
