cmake_minimum_required (VERSION 2.6)
project (StructABILib)
include_directories(${INC_PLATFORM_DIR})

if(WIN32)
    add_compile_options(/TC) # compile all files as C
else()
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fvisibility=hidden")
endif()

# add the executable
add_library (StructABILib SHARED StructABI.c)

# add the install targets
install (TARGETS StructABILib DESTINATION bin)
