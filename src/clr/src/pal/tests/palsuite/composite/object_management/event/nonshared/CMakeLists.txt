cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  event.cpp
  main.cpp
)

add_executable(paltest_event_nonshared
  ${SOURCES}
)

add_dependencies(paltest_event_nonshared coreclrpal)

target_link_libraries(paltest_event_nonshared
  ${COMMON_TEST_LIBRARIES}
  rt
)
