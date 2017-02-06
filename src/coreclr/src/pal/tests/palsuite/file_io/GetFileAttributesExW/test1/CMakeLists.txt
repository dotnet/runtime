cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test1.cpp
)

add_executable(paltest_getfileattributesexw_test1
  ${SOURCES}
)

add_dependencies(paltest_getfileattributesexw_test1 coreclrpal)

target_link_libraries(paltest_getfileattributesexw_test1
  ${COMMON_TEST_LIBRARIES}
)
add_subdirectory(.hidden_directory)
add_subdirectory(normal_test_directory)
add_subdirectory(ro_test_directory)

