set(SOURCES
  WFSOThreadTest.cpp
)

add_executable(paltest_waitforsingleobject_wfsothreadtest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsothreadtest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsothreadtest
  ${COMMON_TEST_LIBRARIES}
)
