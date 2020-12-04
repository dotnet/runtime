project (NativeFunctions)
include_directories(${INC_PLATFORM_DIR})

add_library (NativeFunctions SHARED NativeFunctions.cpp)

# add the install targets
install (TARGETS NativeFunctions DESTINATION bin)
