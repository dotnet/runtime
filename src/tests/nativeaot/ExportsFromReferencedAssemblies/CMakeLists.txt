project (NativeHost)
include_directories(${INC_PLATFORM_DIR})

add_library (NativeHost STATIC NativeHost.cpp)

# add the install targets
install (TARGETS NativeHost DESTINATION bin)
