cmake_minimum_required(VERSION 3.0.0)

project(library VERSION 0.1.0)

SET(CMAKE_SKIP_BUILD_RPATH  FALSE)
SET(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
SET(CMAKE_INSTALL_RPATH "\${ORIGIN}")

add_executable(rpath.out library.c)
