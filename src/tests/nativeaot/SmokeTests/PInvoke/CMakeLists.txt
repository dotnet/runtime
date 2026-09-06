# See the comment in ../../CustomMain/CMakeLists.txt: nativeaot tests never build for browser
# wasm, so this directory excludes itself from the recursive add_subdirectory globber.
if (CLR_CMAKE_TARGET_ARCH_WASM)
    return()
endif()

project (PInvokeNative)
include_directories(${INC_PLATFORM_DIR})

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        ole32.lib
    )
endif(CLR_CMAKE_HOST_WIN32)

add_library (PInvokeNative ${TEST_LIB_TYPE} PInvokeNative.cpp)
target_link_libraries(PInvokeNative PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS PInvokeNative DESTINATION bin)
