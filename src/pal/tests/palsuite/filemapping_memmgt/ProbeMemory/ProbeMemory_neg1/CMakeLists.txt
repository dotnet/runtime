cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  ProbeMemory_neg.cpp
)

add_executable(paltest_probememory_probememory_neg1
  ${SOURCES}
)

add_dependencies(paltest_probememory_probememory_neg1 coreclrpal)

target_link_libraries(paltest_probememory_probememory_neg1
  ${COMMON_TEST_LIBRARIES}
)
