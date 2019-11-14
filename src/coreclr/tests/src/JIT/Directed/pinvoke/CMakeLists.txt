cmake_minimum_required(VERSION 2.6)
project(PInvokeExampleNative)

set(CMAKE_SHARED_LIBRARY_PREFIX "")

add_library(PInvokeExampleNative SHARED pinvokeexamplenative.cpp)
SET_TARGET_PROPERTIES(PInvokeExampleNative PROPERTIES COMPILE_FLAGS "-c")

# add the install targets (this "installs" the native file on Windows systems)
install(TARGETS PInvokeExampleNative DESTINATION bin)

# This "installs" the native file on System V systems
set_target_properties(PInvokeExampleNative PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/PInvokeExampleNative)
