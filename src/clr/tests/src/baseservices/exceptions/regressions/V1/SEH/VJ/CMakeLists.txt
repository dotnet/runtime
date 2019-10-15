cmake_minimum_required (VERSION 2.6)
if (WIN32)
project (Unmanaged)
include_directories(${INC_PLATFORM_DIR})

# add the executable
add_library (Unmanaged SHARED Unmanaged.cpp)

# add the install targets
install (TARGETS Unmanaged DESTINATION bin)
endif()
