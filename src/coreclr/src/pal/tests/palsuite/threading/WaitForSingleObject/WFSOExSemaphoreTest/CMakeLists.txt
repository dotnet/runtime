set(SOURCES
  WFSOExSemaphoreTest.cpp
)

add_executable(paltest_waitforsingleobject_wfsoexsemaphoretest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsoexsemaphoretest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsoexsemaphoretest
  ${COMMON_TEST_LIBRARIES}
)
