cmake_minimum_required(VERSION 2.6)
project(object_pin_mirror)

set(CMAKE_SHARED_LIBRARY_PREFIX "")

add_library(mirror SHARED mirror.cpp)
SET_TARGET_PROPERTIES(mirror PROPERTIES COMPILE_FLAGS "-c") 

# add the install targets (this "installs" the native file on Windows systems)
install(TARGETS mirror DESTINATION bin)

# This "installs" the native file on System V systems
set_target_properties(mirror PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/mirror)
