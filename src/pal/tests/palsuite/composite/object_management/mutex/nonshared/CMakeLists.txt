cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  main.cpp
  mutex.cpp
)

add_executable(paltest_mutex_nonshared
  ${SOURCES}
)

add_dependencies(paltest_mutex_nonshared coreclrpal)

target_link_libraries(paltest_mutex_nonshared
  ${COMMON_TEST_LIBRARIES}
  rt
)
