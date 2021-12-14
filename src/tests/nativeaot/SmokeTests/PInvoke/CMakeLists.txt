project (PInvokeNative)
include_directories(${INC_PLATFORM_DIR})

add_library (PInvokeNative SHARED PInvokeNative.cpp)

# add the install targets
install (TARGETS PInvokeNative DESTINATION bin)
