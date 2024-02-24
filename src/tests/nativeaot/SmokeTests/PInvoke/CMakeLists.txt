project (PInvokeNative)
include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        ole32.lib
    )
endif(CLR_CMAKE_HOST_WIN32)

add_library (PInvokeNative SHARED PInvokeNative.cpp)
target_link_libraries(PInvokeNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

if(CLR_CMAKE_HOST_WIN32)
    target_link_options(PInvokeNative PRIVATE /DEF:${CMAKE_CURRENT_SOURCE_DIR}/PInvokeNative.def)
endif(CLR_CMAKE_HOST_WIN32)

# add the install targets
install (TARGETS PInvokeNative DESTINATION bin)
