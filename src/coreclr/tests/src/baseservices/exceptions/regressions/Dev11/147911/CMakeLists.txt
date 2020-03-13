if(CLR_CMAKE_TARGET_WIN32)
project (fpcw)
include_directories(${INC_PLATFORM_DIR})

# add the executable
add_library (fpcw SHARED fpcw.cpp)

# add the install targets
install (TARGETS fpcw DESTINATION bin)
endif()
