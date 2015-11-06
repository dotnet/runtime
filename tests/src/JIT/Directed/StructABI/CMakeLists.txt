cmake_minimum_required (VERSION 2.6)
project (StructABILib)
include_directories(${INC_PLATFORM_DIR})

set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fvisibility=hidden")

# add the executable
add_library (StructABILib SHARED StructABI.c)

# add the install targets
install (TARGETS StructABILib DESTINATION bin)
