cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  setcurrentdirectorya.cpp
)

add_executable(paltest_setcurrentdirectorya_test3
  ${SOURCES}
)

add_dependencies(paltest_setcurrentdirectorya_test3 coreclrpal)

target_link_libraries(paltest_setcurrentdirectorya_test3
  ${COMMON_TEST_LIBRARIES}
)
