cmake_minimum_required(VERSION 2.8.12.2)
project(palexmpl)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  example1.c
)

add_executable(palexmpl
  ${SOURCES}
)

add_dependencies(palexmpl coreclrpal)

target_link_libraries(palexmpl
  coreclrpal
)
