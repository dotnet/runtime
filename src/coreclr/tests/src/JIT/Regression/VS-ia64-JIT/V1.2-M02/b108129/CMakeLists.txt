cmake_minimum_required(VERSION 2.6)
project(b108129_test2)
include_directories(${INC_PLATFORM_DIR})
set(CMAKE_SHARED_LIBRARY_PREFIX "")

add_library(test2 SHARED test2.cpp)
SET_TARGET_PROPERTIES(test2 PROPERTIES COMPILE_FLAGS "-c") 

# add the install targets (this "installs" the native file on Windows systems)
install(TARGETS test2 DESTINATION bin)

# This "installs" the native file on System V systems
set_target_properties(test2 PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/test2)
