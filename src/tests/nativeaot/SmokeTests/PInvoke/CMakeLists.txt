project (PInvokeNative)
include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        ole32.lib
    )
endif(CLR_CMAKE_HOST_WIN32)

add_library (PInvokeNative SHARED PInvokeNative.cpp)
target_link_libraries(PInvokeNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS PInvokeNative DESTINATION bin)
