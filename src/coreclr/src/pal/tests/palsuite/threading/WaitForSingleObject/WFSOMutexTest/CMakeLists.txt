set(SOURCES
  WFSOMutexTest.cpp
)

add_executable(paltest_waitforsingleobject_wfsomutextest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsomutextest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsomutextest
  ${COMMON_TEST_LIBRARIES}
)
