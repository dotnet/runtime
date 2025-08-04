project (PInvokeUnhandledNative)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES PInvokeUnhandledNative.cpp)

if(NOT CLR_CMAKE_HOST_WIN32)
    add_compile_options(-pthread)
endif()

# add the executable
add_library (PInvokeUnhandledNative SHARED ${SOURCES})

# add the install targets
install (TARGETS PInvokeUnhandledNative DESTINATION bin)
