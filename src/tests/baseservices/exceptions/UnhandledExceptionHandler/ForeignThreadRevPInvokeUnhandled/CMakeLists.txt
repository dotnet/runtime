project (ForeignThreadRevPInvokeUnhandledNative)

include_directories(${INC_PLATFORM_DIR})

set(SOURCES ForeignThreadRevPInvokeUnhandledNative.cpp)

if(NOT CLR_CMAKE_HOST_WIN32)
    add_compile_options(-pthread)
endif()

# add the executable
add_library (ForeignThreadRevPInvokeUnhandledNative SHARED ${SOURCES})

# add the install targets
install (TARGETS ForeignThreadRevPInvokeUnhandledNative DESTINATION bin)
