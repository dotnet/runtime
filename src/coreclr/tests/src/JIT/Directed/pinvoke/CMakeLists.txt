cmake_minimum_required(VERSION 2.6)
project(pinvoke_user32menu)

set(CMAKE_SHARED_LIBRARY_PREFIX "")

add_library(user32menu SHARED user32menu.cpp)
SET_TARGET_PROPERTIES(user32menu PROPERTIES COMPILE_FLAGS "-c")

# add the install targets (this "installs" the native file on Windows systems)
install(TARGETS user32menu DESTINATION bin)

# This "installs" the native file on System V systems
set_target_properties(user32menu PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/user32menu)
