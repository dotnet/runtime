cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

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
