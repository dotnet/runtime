project (PropagateThroughNative_Native)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES PropagateThroughNative.cpp)

# add the executable
add_library (PropagateThroughNative_Native SHARED ${SOURCES})

# add the install targets
install (TARGETS PropagateThroughNative_Native DESTINATION bin)
