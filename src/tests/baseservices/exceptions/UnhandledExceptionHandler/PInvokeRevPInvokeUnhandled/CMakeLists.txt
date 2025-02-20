project (PInvokeRevPInvokeUnhandledNative)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES PInvokeRevPInvokeUnhandledNative.cpp)

if(NOT CLR_CMAKE_HOST_WIN32)
    add_compile_options(-pthread)
endif()

# add the executable
add_library (PInvokeRevPInvokeUnhandledNative SHARED ${SOURCES})

# add the install targets
install (TARGETS PInvokeRevPInvokeUnhandledNative DESTINATION bin)
