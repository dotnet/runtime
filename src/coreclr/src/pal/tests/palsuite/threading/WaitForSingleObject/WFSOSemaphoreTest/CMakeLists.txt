set(SOURCES
  WFSOSemaphoreTest.cpp
)

add_executable(paltest_waitforsingleobject_wfsosemaphoretest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsosemaphoretest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsosemaphoretest
  ${COMMON_TEST_LIBRARIES}
)
