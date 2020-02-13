if (CLR_CMAKE_TARGET_WIN32)
project (Unmanaged)
include_directories(${INC_PLATFORM_DIR})

# add the executable
add_library (Unmanaged SHARED Unmanaged.cpp)

# add the install targets
install (TARGETS Unmanaged DESTINATION bin)
endif()
