set(SOURCES
  WFSOExThreadTest.cpp
)

add_executable(paltest_waitforsingleobject_wfsoexthreadtest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsoexthreadtest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsoexthreadtest
  ${COMMON_TEST_LIBRARIES}
)
