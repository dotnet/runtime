cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  GetFileAttributesA.c
)

add_executable(paltest_getfileattributesa_test1
  ${SOURCES}
)

add_dependencies(paltest_getfileattributesa_test1 coreclrpal)

target_link_libraries(paltest_getfileattributesa_test1
  pthread
  m
  coreclrpal
)
add_subdirectory(.hidden_directory)
add_subdirectory(.hidden_ro_directory)
add_subdirectory(normal_test_directory)
add_subdirectory(no_directory)
add_subdirectory(ro_test_directory)
add_subdirectory(rw_directory)

