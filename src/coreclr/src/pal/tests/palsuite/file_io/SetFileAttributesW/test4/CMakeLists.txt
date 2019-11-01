cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  SetFileAttributesW.cpp
)

add_executable(paltest_setfileattributesw_test4
  ${SOURCES}
)

add_dependencies(paltest_setfileattributesw_test4 coreclrpal)

target_link_libraries(paltest_setfileattributesw_test4
  ${COMMON_TEST_LIBRARIES}
)
