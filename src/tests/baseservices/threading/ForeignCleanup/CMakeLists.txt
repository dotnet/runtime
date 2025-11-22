include_directories(${INC_PLATFORM_DIR})

set(SOURCES ForeignThreadCleanupNative.cpp)

if(NOT CLR_CMAKE_HOST_WIN32)
    add_compile_options(-pthread)
endif()

# add the executable
add_library (ForeignThreadCleanupNative SHARED ${SOURCES})

# add the install targets
install (TARGETS ForeignThreadCleanupNative DESTINATION bin)
