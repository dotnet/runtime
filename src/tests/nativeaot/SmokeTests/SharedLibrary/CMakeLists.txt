project (SharedLibrary)
include_directories(${INC_PLATFORM_DIR})

add_executable (SharedLibraryDriver SharedLibrary.cpp)

if (CLR_CMAKE_TARGET_UNIX)
    target_link_libraries (SharedLibraryDriver ${CMAKE_DL_LIBS})
endif()

# add the install targets
install (TARGETS PInvokeNative DESTINATION bin)
