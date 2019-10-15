cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

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
